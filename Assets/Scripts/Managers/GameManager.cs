using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Mage
{
    /// <summary>흐름 FSM과 판 데이터 담당. 상태 전환 요청을 받아 순서대로 처리</summary>
    public sealed class GameManager : MonoSingleton<GameManager>
    {
        /// <summary>에디터 Play 때 시작할 씬 이름을 PlayModeRouter가 적어 두는 SessionState 키</summary>
        public const string EditorStartSceneKey = "Mage.EditorStartScene";

        /// <summary>이번 판 데이터 (마법사 이름·파티·의뢰)</summary>
        public RunEntry Run { get; private set; }

        /// <summary>지금 흐름 상태. 시작 전엔 null</summary>
        FlowStateBase _state;

        /// <summary>상태 전환 진행 중이면 true. 중복 전환 막기용</summary>
        bool _isSwitching;

        #region Start - 매니저 확인 → 다국어 준비 대기 → 시작 상태로 진입 (매니저끼리 Awake 순서가 없어서 Start에서)
        IEnumerator Start()
        {
            // 하나라도 빠지면 Boot 씬 구성이 잘못된 것 (빠진 쪽은 Instance가 에러를 남김)
            bool allReady = TransitionManager.Instance != null && UIManager.Instance != null
                && DataManager.Instance != null && SettingsManager.Instance != null
                && AudioManager.Instance != null && TutorialManager.Instance != null;
            if (!allReady) yield break;

            // 첫 화면 글자가 늦게 바뀌지 않게, 덮개를 덮은 채 다국어 준비를 기다림
            TransitionManager.Instance.ShowCover();
            // 다국어 초기화 작업
            var localization = LocalizationSettings.InitializationOperation;
            yield return localization;
            if (localization.Status != AsyncOperationStatus.Succeeded)
                Debug.LogError("[Flow] 다국어 초기화 실패. 화면 글자가 안 나올 수 있음");

            // 시작할 흐름 상태 (빌드: Title, 에디터: 편집 중이던 씬)
            FlowStateBase start = CreateStartState();
            if (start == null)
            {
                TransitionManager.Instance.HideCover();
                yield break;
            }

            // 타이틀·생성 화면이 아니면 중간부터 시작한 것 → 디버그 데이터
            Run = start is TitleState or CharacterCreationState ? new RunEntry() : RunEntry.CreateDebug();
            ChangeState(start);
        }
        #endregion

        #region CreateStartState - 시작 씬 이름에 맞는 상태 만들기
        FlowStateBase CreateStartState()
        {
            // 시작할 씬 이름. 빌드에선 늘 Title
            string sceneName = TitleState.Scene;
#if UNITY_EDITOR
            sceneName = UnityEditor.SessionState.GetString(EditorStartSceneKey, TitleState.Scene);
#endif
            // 씬 이름에 맞는 상태. 흐름 씬이 아니면 null
            FlowStateBase state = sceneName switch
            {
                TitleState.Scene => new TitleState(),
                CharacterCreationState.Scene => new CharacterCreationState(),
                OpeningState.Scene => new OpeningState(),
                HubState.Scene => new HubState(),
                FieldState.Scene => new FieldState(),
                _ => null,
            };
            if (state == null) Debug.LogError($"[Flow] {sceneName}에 맞는 상태 없음");
            return state;
        }
        #endregion

        #region ChangeState - 다음 상태로 전환 요청. 전환 중이거나 씬을 못 읽으면 거절
        /// <param name="next">들어갈 상태</param>
        public void ChangeState(FlowStateBase next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (_isSwitching)
            {
                Debug.LogWarning($"[Flow] 전환 중이라 {next.GetType().Name} 무시");
                return;
            }
            if (!TransitionManager.Instance.CanLoad(next.SceneName))
            {
                Debug.LogError($"[Flow] {next.SceneName} 씬을 읽을 수 없음. 지금 상태 유지");
                return;
            }
            StartCoroutine(SwitchState(next));
        }
        #endregion

        #region SwitchState - 덮개 → 이전 상태 Exit → 씬 교체 → 새 상태 Begin → 덮개 해제
        /// <param name="next">들어갈 상태</param>
        IEnumerator SwitchState(FlowStateBase next)
        {
            // 덮개·씬 교체 담당
            TransitionManager transition = TransitionManager.Instance;
            _isSwitching = true;
            try
            {
                transition.ShowCover();
                Debug.Log($"[Flow] {_state?.GetType().Name ?? "시작"} → {next.GetType().Name}");
                _state?.Exit();
                _state = next;
                yield return transition.LoadScene(next.SceneName);
                // (나중) 상태별 준비 작업 자리: 던전 생성·스폰. 덮개가 덮인 채로
                next.Begin(this);
            }
            finally
            {
                // 중간에 예외가 나도 덮개와 잠금이 남지 않게
                transition.HideCover();
                _isSwitching = false;
            }
        }
        #endregion
    }
}
