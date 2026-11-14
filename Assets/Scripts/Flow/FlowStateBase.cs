using UnityEngine;

namespace Mage
{
    /// <summary>흐름 상태 공통. 상태 하나 = 내용 씬 하나</summary>
    public abstract class FlowStateBase
    {
        /// <summary>이 상태의 내용 씬 이름. 빌드 목록 이름과 같아야 함</summary>
        public abstract string SceneName { get; }

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

        #region OnSceneReady - 상태별로 View를 찾아 버튼·입력을 연결하고 표시
        protected abstract void OnSceneReady();
        #endregion

        #region Exit - 다음 상태로 가기 전. 씬 밖에 사는 구독(입력 액션 등) 해제
        public virtual void Exit() { }
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
