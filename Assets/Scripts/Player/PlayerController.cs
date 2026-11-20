using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>1인칭 조작: 이동·달리기·마우스 시점·중력. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        /// <summary>이동 입력 액션 경로 (전역 액션, WASD)</summary>
        const string MoveActionPath = "Player/Move";

        /// <summary>시점 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>달리기 입력 액션 경로 (전역 액션, 왼쪽 Shift)</summary>
        const string SprintActionPath = "Player/Sprint";

        /// <summary>땅에 있을 때 아래로 누르는 속도 (m/s). 경사·턱에서 뜨지 않게</summary>
        const float GroundedFallSpeed = -2f;

        /// <summary>몸 (충돌·이동. 인스펙터에서만 연결)</summary>
        [SerializeField] CharacterController _body;

        /// <summary>머리 (카메라 자리, 위아래로 돎. 인스펙터에서만 연결)</summary>
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

        /// <summary>이동 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _move;

        /// <summary>시점 입력. 없으면 null</summary>
        InputAction _look;

        /// <summary>달리기 입력. 없으면 null</summary>
        InputAction _sprint;

        /// <summary>지금 위아래 각도 (도, 위를 보면 음수)</summary>
        float _pitch;

        /// <summary>지금 떨어지는 속도 (m/s, 아래가 음수)</summary>
        float _fallSpeed;

        /// <summary>머리 (카메라 자리)</summary>
        public Transform Head => _head;

        #region Awake - 연결 확인, 입력 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_body == null || _head == null) Debug.LogError("[Player] 몸·머리 연결 안 됨");
            _move = FindAction(MoveActionPath);
            _look = FindAction(LookActionPath);
            _sprint = FindAction(SprintActionPath);
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

        #region PlaceAt - 지점 위치·좌우 방향으로 옮기고 위아래 각도·낙하 속도 초기화 (CharacterController는 켜진 채 옮기면 무시돼서 잠깐 끔)
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
        }
        #endregion

        #region Update - 게임 시간이 멈췄으면(로딩창·일시정지) 아무것도 안 함. 아니면 시점 → 이동
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            Look();
            Move();
        }
        #endregion

        #region Look - 마우스 이동량으로 좌우는 몸, 위아래는 머리
        void Look()
        {
            if (_look == null) return;
            // 이번 프레임 회전량 (도)
            Vector2 delta = _look.ReadValue<Vector2>() * _lookSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            _pitch = Mathf.Clamp(_pitch - delta.y, -_pitchLimit, _pitchLimit);
            _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
        #endregion

        #region Move - 몸 기준 앞·옆으로 걷기·달리기, 땅이면 살짝 눌러 붙이고 아니면 중력으로 떨어짐
        void Move()
        {
            // 이동 입력 (x 옆, y 앞. 대각선도 1을 안 넘게)
            Vector2 input = _move != null ? Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            // 지금 속도 (달리기를 누르고 있으면 달리기 속도)
            float speed = _sprint != null && _sprint.IsPressed() ? _sprintSpeed : _walkSpeed;
            // 몸 기준 평면 이동 방향
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            _fallSpeed = _body.isGrounded ? GroundedFallSpeed : _fallSpeed + _gravity * Time.deltaTime;
            _body.Move((direction * speed + Vector3.up * _fallSpeed) * Time.deltaTime);
        }
        #endregion
    }
}
