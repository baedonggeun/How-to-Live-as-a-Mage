using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>눈 앞 일정 거리 안에서 바라보는 상호작용 지점을 찾고, E(Player/Interact)를 누르면 알림. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
    public sealed class InteractDetector : MonoBehaviour
    {
        /// <summary>상호작용 입력 액션 경로 (전역 액션, E)</summary>
        const string InteractActionPath = "Player/Interact";

        /// <summary>키 이름을 읽을 바인딩 그룹 (키보드·마우스)</summary>
        const string KeyboardMouseGroup = "Keyboard&Mouse";

        /// <summary>레이를 쏠 눈 (플레이어 머리. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _eye;

        /// <summary>닿는 거리 (m)</summary>
        [SerializeField, Min(0.1f)] float _range = 2.5f;

        /// <summary>레이가 맞을 레이어</summary>
        [SerializeField] LayerMask _mask = ~0;

        /// <summary>상호작용 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _interact;

        /// <summary>지금 바라보는 지점. 없으면 null</summary>
        public InteractPointView Target { get; private set; }

        /// <summary>바라보는 지점이 바뀜 (없어지면 null)</summary>
        public event Action<InteractPointView> TargetChanged;

        /// <summary>지점을 바라보며 E를 누름</summary>
        public event Action<InteractPointView> Interacted;

        /// <summary>E 키 이름 (안내 글용, 키보드 기준. 액션이 없으면 빈 문자열)</summary>
        public string KeyName => _interact != null
            ? _interact.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardMouseGroup))
            : string.Empty;

        #region Awake - 연결 확인, E 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_eye == null) Debug.LogError("[Interact] 눈 연결 안 됨");
            _interact = InputSystem.actions.FindAction(InteractActionPath);
            if (_interact == null) Debug.LogError($"[Interact] {InteractActionPath} 액션 없음");
            enabled = false;
        }
        #endregion

        #region OnEnable - E 구독
        void OnEnable()
        {
            if (_interact != null) _interact.performed += OnInteract;
        }
        #endregion

        #region OnDisable - E 구독 해제, 바라보던 지점 비움 (알림)
        void OnDisable()
        {
            if (_interact != null) _interact.performed -= OnInteract;
            SetTarget(null);
        }
        #endregion

        #region Update - 눈 앞으로 레이를 쏴서 맞은 콜라이더(트리거 포함)의 지점 표지를 대상으로
        void Update()
        {
            if (_eye == null) return;
            // 맞은 곳의 지점 표지 (없으면 null)
            InteractPointView found = null;
            if (Physics.Raycast(_eye.position, _eye.forward, out RaycastHit hit, _range, _mask, QueryTriggerInteraction.Collide))
                found = hit.collider.GetComponentInParent<InteractPointView>();
            SetTarget(found);
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

        #region OnInteract - 바라보는 지점이 있으면 E 알림
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnInteract(InputAction.CallbackContext context)
        {
            if (Target != null) Interacted?.Invoke(Target);
        }
        #endregion
    }
}
