using UnityEngine;

namespace Mage
{
    /// <summary>흐름 상태 공통. 상태 하나 = 내용 씬 하나</summary>
    public abstract class FlowStateBase
    {
        /// <summary>이 상태의 내용 씬 이름. 빌드 목록 이름과 같아야 함</summary>
        public abstract string SceneName { get; }

        /// <summary>이 상태로 들어갈 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다릴지. 기본은 안 기다림 (검은 덮개만, 준비되면 바로 걷음). 플레이 씬(전초기지·숲)만 켬</summary>
        public virtual bool WaitsForContinue => false;

        /// <summary>이 상태에서 Esc로 일시정지 메뉴를 열 수 있는지. 기본은 못 엶 (메뉴 화면). 플레이 씬(전초기지·숲)만 켬</summary>
        public virtual bool UsesPauseMenu => false;

        /// <summary>이 상태를 연 GameManager. Begin에서 받음</summary>
        protected GameManager Game { get; private set; }

        #region Begin - 씬 준비가 끝나면 GameManager가 부름. 참조를 받고 OnSceneReady 실행
        /// <param name="game">이 상태를 연 GameManager</param>
        public void Begin(GameManager game)
        {
            if (game == null) throw new System.ArgumentNullException(nameof(game));
            Game = game;
            OnSceneReady();
        }
        #endregion

        #region OnSceneReady - 상태별로 View를 찾아 버튼을 연결하고 글자를 채움 (덮개 밑. 배경음·조작 입력은 Reveal)
        protected abstract void OnSceneReady();
        #endregion

        #region Reveal - 덮개가 걷힌 뒤 GameManager가 부름. 화면이 보일 때 시작할 것 (배경음, 커서 잠금, 조작 입력)
        public virtual void Reveal() { }
        #endregion

        #region Exit - 다음 상태로 가기 전. 씬 밖에 사는 구독(입력 액션 등) 해제
        public virtual void Exit() { }
        #endregion

        #region HandleCancel - 열린 공용 창이 없을 때 Esc를 상태가 먼저 받음. 처리했으면 true (일시정지 메뉴를 안 엶). 기본은 처리 안 함
        public virtual bool HandleCancel() => false;
        #endregion

        #region FindView - 지금 씬에서 View 하나 찾기. 없으면 에러 로그 후 null
        protected T FindView<T>() where T : Object
        {
            // 찾은 View. 씬에 없으면 null
            T view = Object.FindAnyObjectByType<T>();
            if (view == null) Debug.LogError($"[Flow] {SceneName} 씬에 {typeof(T).Name} 없음");
            return view;
        }
        #endregion
    }
}
