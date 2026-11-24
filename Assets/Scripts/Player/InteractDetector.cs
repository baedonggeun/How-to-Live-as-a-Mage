using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>눈 앞 일정 거리 안에서 바라보는 상호작용 지점을 찾고, 상호작용 키(Player/Interact, 기본 F)를 누르면 알림. 뗄 때 상호작용을 켜면(숲) 0.35초보다 짧게 눌렀다 뗄 때만 알리고, 그 이상 꾹 누르면 주문 시전이라 알리지 않음. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
    public sealed class InteractDetector : MonoBehaviour
    {
        /// <summary>상호작용 입력 액션 경로 (전역 액션, 기본 F)</summary>
        const string InteractActionPath = "Player/Interact";

        /// <summary>키 이름을 읽을 바인딩 그룹 (키보드·마우스)</summary>
        const string KeyboardMouseGroup = "Keyboard&Mouse";

        /// <summary>꾹 누르기로 보는 시간 (초). 이보다 짧게 눌렀다 떼면 단발 (뗄 때 상호작용), 이 이상 누르면 꾹 누르기 (주문 시전 시작). 주문 그리기 패드도 이 값을 읽음</summary>
        public const float HoldSeconds = 0.35f;

        /// <summary>레이를 쏠 눈 (플레이어 머리. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _eye;

        /// <summary>닿는 거리 (m)</summary>
        [SerializeField, Min(0.1f)] float _range = 2.5f;

        /// <summary>레이가 맞을 레이어</summary>
        [SerializeField] LayerMask _mask = ~0;

        /// <summary>단발을 키를 뗄 때 상호작용으로 칠지 (숲: 켬. 꾹 누르기와 구분하려고. 전초기지: 끔. 누르는 즉시 상호작용)</summary>
        [SerializeField] bool _tapOnRelease;

        /// <summary>상호작용 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _interact;

        /// <summary>레이에 맞은 것 (순서 없음. 매 프레임 다시 씀)</summary>
        readonly RaycastHit[] _hits = new RaycastHit[8];

        /// <summary>상호작용 키를 누른 시각 (실제 시간, 초)</summary>
        float _pressedAt;

        /// <summary>상호작용 키를 누르고 있는지 (뗄 때 상호작용이 켜졌을 때만 씀)</summary>
        bool _pressed;

        /// <summary>지금 바라보는 지점. 없으면 null</summary>
        public InteractPointView Target { get; private set; }

        /// <summary>바라보는 지점이 바뀜 (없어지면 null)</summary>
        public event Action<InteractPointView> TargetChanged;

        /// <summary>지점을 바라보며 상호작용 키를 누름</summary>
        public event Action<InteractPointView> Interacted;

        /// <summary>상호작용 키 이름 (안내 글용, 키보드 기준. 액션이 없으면 빈 문자열)</summary>
        public string KeyName => _interact != null
            ? _interact.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardMouseGroup))
            : string.Empty;

        #region Awake - 연결 확인, 상호작용 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_eye == null) Debug.LogError("[Interact] 눈 연결 안 됨");
            _interact = InputSystem.actions.FindAction(InteractActionPath);
            if (_interact == null) Debug.LogError($"[Interact] {InteractActionPath} 액션 없음");
            enabled = false;
        }
        #endregion

        #region OnEnable - 상호작용 키 누름·뗌 구독
        void OnEnable()
        {
            if (_interact == null) return;
            _interact.performed += OnInteract;
            _interact.canceled += OnReleased;
        }
        #endregion

        #region OnDisable - 구독 해제, 누르던 것·바라보던 지점 비움 (알림)
        void OnDisable()
        {
            if (_interact != null)
            {
                _interact.performed -= OnInteract;
                _interact.canceled -= OnReleased;
            }
            _pressed = false;
            SetTarget(null);
        }
        #endregion

        #region Update - 눈 앞으로 레이를 쏴서 맞은 것 중 자기 몸(플레이어 아래 콜라이더)을 뺀 가장 가까운 콜라이더(트리거 포함)의 지점 표지를 대상으로. 아래를 볼 때 자기 CharacterController에 먼저 맞아서
        void Update()
        {
            if (_eye == null) return;
            // 레이에 맞은 수
            int count = Physics.RaycastNonAlloc(_eye.position, _eye.forward, _hits, _range, _mask, QueryTriggerInteraction.Collide);
            // 자기 몸을 뺀 가장 가까운 콜라이더와 거리
            Collider nearest = null;
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                // 이번에 맞은 콜라이더
                Collider hit = _hits[i].collider;
                if (hit == null || hit.transform.IsChildOf(transform) || _hits[i].distance >= best) continue;
                best = _hits[i].distance;
                nearest = hit;
            }
            SetTarget(nearest != null ? nearest.GetComponentInParent<InteractPointView>() : null);
        }
        #endregion

        #region SetTarget - 대상이 바뀌었을 때만 바꾸고 알림
        /// <param name="target">새 대상 (없으면 null)</param>
        void SetTarget(InteractPointView target)
        {
            if (target == Target) return;
            Target = target;
            TargetChanged?.Invoke(target);
        }
        #endregion

        #region OnInteract - 상호작용 키를 누름: 뗄 때 상호작용이 꺼져 있으면 바라보는 지점에 바로 알림, 켜져 있으면 누른 시각만 적음
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnInteract(InputAction.CallbackContext context)
        {
            if (_tapOnRelease)
            {
                _pressed = true;
                _pressedAt = Time.unscaledTime;
                return;
            }
            if (Target != null) Interacted?.Invoke(Target);
        }
        #endregion

        #region OnReleased - 상호작용 키를 뗌 (뗄 때 상호작용이 켜졌을 때만): HoldSeconds보다 짧게 눌렀다 뗐으면 단발이라 바라보는 지점에 알림. 꾹 눌렀으면 주문 시전이라 상호작용 아님. 창·메뉴가 떠서 입력이 꺼지며 온 떼기도 아님
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnReleased(InputAction.CallbackContext context)
        {
            if (!_tapOnRelease || !_pressed) return;
            _pressed = false;
            if (Time.timeScale <= 0f || Time.unscaledTime - _pressedAt >= HoldSeconds) return;
            if (Target != null) Interacted?.Invoke(Target);
        }
        #endregion
    }
}
