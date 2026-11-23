using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>1인칭 조작: 이동·달리기(뒤로는 못 달림, 스태미나)·점프·앉기(누르는 동안)·마우스 시점·중력. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함. 현장 시전이 이동·시점을 잠금</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        /// <summary>이동 입력 액션 경로 (전역 액션, WASD)</summary>
        const string MoveActionPath = "Player/Move";

        /// <summary>시점 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>달리기 입력 액션 경로 (전역 액션, 왼쪽 Shift)</summary>
        const string SprintActionPath = "Player/Sprint";

        /// <summary>점프 입력 액션 경로 (전역 액션, Space. 시전 상태에선 같은 키가 바로 판정이라 이동 잠금으로 막힘)</summary>
        const string JumpActionPath = "Player/Jump";

        /// <summary>앉기 입력 액션 경로 (전역 액션, 왼쪽 Ctrl. 누르는 동안)</summary>
        const string CrouchActionPath = "Player/Crouch";

        /// <summary>땅에 있을 때 아래로 누르는 속도 (m/s). 경사·턱에서 뜨지 않게</summary>
        const float GroundedFallSpeed = -2f;

        /// <summary>이보다 뒤로 가는 입력이면 못 달림 (앞뒤 입력 값)</summary>
        const float BackwardInput = -0.01f;

        /// <summary>움직이는 중으로 볼 이동 입력 크기</summary>
        const float MovingInput = 0.1f;

        /// <summary>몸 (충돌·이동. 인스펙터에서만 연결)</summary>
        [SerializeField] CharacterController _body;

        /// <summary>머리 (카메라 자리, 위아래로 돎·앉으면 낮아짐. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _head;

        /// <summary>걷기 속도 (m/s)</summary>
        [SerializeField, Min(0f)] float _walkSpeed = 5.6f;

        /// <summary>달리기 속도 (m/s)</summary>
        [SerializeField, Min(0f)] float _sprintSpeed = 8.2f;

        /// <summary>마우스 감도 (이동량 1당 도)</summary>
        [SerializeField, Min(0f)] float _lookSensitivity = 0.1f;

        /// <summary>위아래로 볼 수 있는 한도 (도)</summary>
        [SerializeField, Range(0f, 89f)] float _pitchLimit = 85f;

        /// <summary>중력 가속도 (m/s², 아래가 음수)</summary>
        [SerializeField] float _gravity = -20f;

        /// <summary>점프 처음 속도 (m/s, 임시. 중력 20이면 약 0.6m)</summary>
        [SerializeField, Min(0f)] float _jumpSpeed = 5f;

        /// <summary>앉았을 때 머리 높이 (m, 임시)</summary>
        [SerializeField, Min(0.2f)] float _crouchEyeHeight = 1f;

        /// <summary>앉았을 때 걷기 속도 배율 (임시)</summary>
        [SerializeField, Range(0.1f, 1f)] float _crouchSpeedScale = 0.5f;

        /// <summary>머리가 앉고 서는 빠르기 (m/s)</summary>
        [SerializeField, Min(0.1f)] float _eyeMoveSpeed = 6f;

        /// <summary>최대 스태미나 (임시)</summary>
        [SerializeField, Min(1f)] float _staminaMax = 100f;

        /// <summary>달릴 때 초당 스태미나 소모 (임시)</summary>
        [SerializeField, Min(0f)] float _sprintCost = 25f;

        /// <summary>쉴 때 초당 스태미나 회복 (임시)</summary>
        [SerializeField, Min(0f)] float _staminaRegen = 20f;

        /// <summary>달리기를 멈추고 회복이 시작되기까지 (초, 임시)</summary>
        [SerializeField, Min(0f)] float _regenDelay = 1f;

        /// <summary>다 쓴 뒤 다시 달릴 수 있는 스태미나 비율 (0~1, 임시)</summary>
        [SerializeField, Range(0f, 1f)] float _recoverRatio = 0.3f;

        /// <summary>이동 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _move;

        /// <summary>시점 입력. 없으면 null</summary>
        InputAction _look;

        /// <summary>달리기 입력. 없으면 null</summary>
        InputAction _sprint;

        /// <summary>점프 입력. 없으면 null</summary>
        InputAction _jump;

        /// <summary>앉기 입력. 없으면 null</summary>
        InputAction _crouch;

        /// <summary>지금 위아래 각도 (도, 위를 보면 음수)</summary>
        float _pitch;

        /// <summary>지금 세로 속도 (m/s, 아래가 음수)</summary>
        float _fallSpeed;

        /// <summary>선 머리 높이 (프리팹 Head 높이)</summary>
        float _standEyeHeight;

        /// <summary>지금 머리 높이 (앉고 설 때 이 값을 옮김)</summary>
        float _eyeHeight;

        /// <summary>회복이 시작되기까지 남은 시간 (초)</summary>
        float _regenWait;

        /// <summary>스태미나를 다 써서 회복될 때까지 못 달리는지</summary>
        bool _exhausted;

        /// <summary>머리 (카메라 자리)</summary>
        public Transform Head => _head;

        /// <summary>지금 스태미나</summary>
        public float Stamina { get; private set; }

        /// <summary>최대 스태미나</summary>
        public float StaminaMax => _staminaMax;

        /// <summary>앉아 있는지</summary>
        public bool IsCrouching { get; private set; }

        /// <summary>이번 프레임 달렸는지</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>이동·달리기·점프·앉기 잠금 (현장 시전이 그리는 중·조준 중에 켬)</summary>
        public bool MoveLocked { get; set; }

        /// <summary>시점 잠금 (현장 시전이 그리는 중에 켬)</summary>
        public bool LookLocked { get; set; }

        #region Awake - 연결 확인, 입력 액션 찾기, 선 머리 높이·스태미나 채움, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_body == null || _head == null) Debug.LogError("[Player] 몸·머리 연결 안 됨");
            _move = FindAction(MoveActionPath);
            _look = FindAction(LookActionPath);
            _sprint = FindAction(SprintActionPath);
            _jump = FindAction(JumpActionPath);
            _crouch = FindAction(CrouchActionPath);
            _standEyeHeight = _head != null ? _head.localPosition.y : 1.6f;
            _eyeHeight = _standEyeHeight;
            Stamina = _staminaMax;
            enabled = false;
        }
        #endregion

        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[Player] {path} 액션 없음");
            return action;
        }
        #endregion

        #region PlaceAt - 지점 위치·좌우 방향으로 옮기고 위아래 각도·세로 속도·머리 높이 초기화 (CharacterController는 켜진 채 옮기면 무시돼서 잠깐 끔)
        /// <param name="point">옮길 지점</param>
        public void PlaceAt(Transform point)
        {
            if (point == null) throw new System.ArgumentNullException(nameof(point));
            _body.enabled = false;
            transform.SetPositionAndRotation(point.position, Quaternion.Euler(0f, point.eulerAngles.y, 0f));
            _body.enabled = true;
            _pitch = 0f;
            _fallSpeed = 0f;
            _head.localRotation = Quaternion.identity;
            _eyeHeight = _standEyeHeight;
            SetEyeHeight();
        }
        #endregion

        #region OnDisable - 꺼지면 달리는 중 표시를 끔
        void OnDisable() => IsSprinting = false;
        #endregion

        #region Update - 게임 시간이 멈췄으면(로딩창·일시정지) 아무것도 안 함. 아니면 시점 → 이동
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            Look();
            Move();
        }
        #endregion

        #region Look - 시점이 잠기지 않았으면 마우스 이동량으로 좌우는 몸, 위아래는 머리
        void Look()
        {
            if (_look == null || LookLocked) return;
            // 이번 프레임 회전량 (도)
            Vector2 delta = _look.ReadValue<Vector2>() * _lookSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            _pitch = Mathf.Clamp(_pitch - delta.y, -_pitchLimit, _pitchLimit);
            _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
        #endregion

        #region Move - 잠기지 않았으면 몸 기준 앞·옆으로 걷기·앉아 걷기·달리기(뒤로는 안 됨, 스태미나), 땅이면 눌러 붙이고 점프, 아니면 중력. 머리 높이를 앉음에 맞춤
        void Move()
        {
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            // 이동 입력 (x 옆, y 앞. 대각선도 1을 안 넘게. 잠기면 0)
            Vector2 input = MoveLocked || _move == null ? Vector2.zero : Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
            IsCrouching = !MoveLocked && _crouch != null && _crouch.IsPressed();
            // 움직이는 중인지
            bool moving = input.sqrMagnitude >= MovingInput * MovingInput;
            // 달리려는지 (뒤로 가는 입력·앉음이면 아님)
            bool wantSprint = !MoveLocked && _sprint != null && _sprint.IsPressed() && moving && input.y > BackwardInput && !IsCrouching;
            IsSprinting = wantSprint && !_exhausted && Stamina > 0f;
            UpdateStamina(dt);
            // 지금 속도
            float speed = IsSprinting ? _sprintSpeed : _walkSpeed * (IsCrouching ? _crouchSpeedScale : 1f);
            // 몸 기준 평면 이동 방향
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            // 땅에 있는지
            bool grounded = _body.isGrounded;
            if (grounded && _fallSpeed < 0f) _fallSpeed = GroundedFallSpeed;
            if (grounded && !MoveLocked && !IsCrouching && _jump != null && _jump.WasPressedThisFrame()) _fallSpeed = _jumpSpeed;
            else if (!grounded) _fallSpeed += _gravity * dt;
            _body.Move((direction * speed + Vector3.up * _fallSpeed) * dt);
            UpdateEye(dt);
        }
        #endregion

        #region UpdateStamina - 달리면 줄고 다 쓰면 지침. 안 달리면 잠깐 뒤 회복, 회복 비율을 넘으면 지침이 풀림
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateStamina(float dt)
        {
            if (IsSprinting)
            {
                Stamina = Mathf.Max(0f, Stamina - _sprintCost * dt);
                _regenWait = _regenDelay;
                if (Stamina <= 0f) _exhausted = true;
                return;
            }
            if (_regenWait > 0f)
            {
                _regenWait -= dt;
                return;
            }
            Stamina = Mathf.Min(_staminaMax, Stamina + _staminaRegen * dt);
            if (_exhausted && Stamina >= _staminaMax * _recoverRatio) _exhausted = false;
        }
        #endregion

        #region UpdateEye - 머리 높이를 앉음·섬 쪽으로 옮김
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateEye(float dt)
        {
            _eyeHeight = Mathf.MoveTowards(_eyeHeight, IsCrouching ? _crouchEyeHeight : _standEyeHeight, _eyeMoveSpeed * dt);
            SetEyeHeight();
        }
        #endregion

        #region SetEyeHeight - 머리 로컬 높이를 지금 머리 높이로
        void SetEyeHeight()
        {
            if (_head == null) return;
            // 머리 로컬 위치
            Vector3 position = _head.localPosition;
            position.y = _eyeHeight;
            _head.localPosition = position;
        }
        #endregion
    }
}
