using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>숲. 1단계는 판 데이터 표시만 하고 상호작용 키(F)로 전초기지에 돌아감 (정문 밖)</summary>
    public sealed class FieldState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름 (지역이 늘면 의뢰 데이터에서 받음)</summary>
        public const string Scene = "Forest";

        /// <summary>귀환에 쓰는 입력 액션 경로 (전역 액션)</summary>
        const string InteractActionPath = "Player/Interact";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬. 문구를 보는 동안 게임은 멈춰 있음)</summary>
        public override bool WaitsForContinue => true;

        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (플레이 씬. 메뉴가 떠 있는 동안 귀환도 막힘)</summary>
        public override bool UsesPauseMenu => true;

        /// <summary>귀환 입력 액션. Exit에서 구독 해제</summary>
        InputAction _interact;

        #region OnSceneReady - 판 데이터 표시
        protected override void OnSceneReady()
        {
            // 이 씬의 화면 참조
            var view = FindView<FieldDebugView>();
            if (view != null) ShowRun(view);
        }
        #endregion

        #region Reveal - 커서 잠금, 상호작용 입력 구독 (로딩창을 넘길 때 누른 키는 구독 전이라 귀환으로 안 감)
        public override void Reveal()
        {
            Cursor.lockState = CursorLockMode.Locked;
            _interact = InputSystem.actions.FindAction(InteractActionPath, throwIfNotFound: true);
            _interact.performed += OnInteract;
        }
        #endregion

        #region Exit - 상호작용 구독 해제, 커서 풀기
        public override void Exit()
        {
            if (_interact != null) _interact.performed -= OnInteract;
            Cursor.lockState = CursorLockMode.None;
        }
        #endregion

        #region ShowRun - 이름·파티 수·의뢰 이름을 디버그 글자에 넣음
        /// <param name="view">숲 디버그 화면</param>
        void ShowRun(FieldDebugView view)
        {
            // 의뢰 이름. 디버그 표시라 들어올 때 한 번만 읽음 (숲에서 언어를 바꾸면 다음 진입 때 바뀜)
            string quest = Game.Run.Quest != null ? Game.Run.Quest.Title.GetLocalizedString() : string.Empty;
            view.InfoText.StringReference.Arguments = new object[] { Game.Run.PlayerName, Game.Run.Party.Count, quest };
            view.InfoText.RefreshString();
        }
        #endregion

        #region OnInteract - 상호작용 키를 누르면 전초기지로 (정문 밖에서 시작하게 표시)
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnInteract(InputAction.CallbackContext context)
        {
            Game.Run.ReturningFromField = true;
            Game.ChangeState(new HubState());
        }
        #endregion
    }
}
