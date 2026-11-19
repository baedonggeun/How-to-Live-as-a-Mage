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

        /// <summary>로딩 바에서 씬 로드 단계가 차지하는 몫 (0~1). 콘솔 [Flow] 로딩 시간 로그로 잰 비율에 맞춤</summary>
        [SerializeField, Range(0f, 1f)] float _sceneLoadShare = 0.8f;

        /// <summary>로딩 바에서 씬에 정보 적용 단계가 차지하는 몫 (0~1). 다국어 적용은 나머지</summary>
        [SerializeField, Range(0f, 1f)] float _sceneReadyShare = 0.1f;

        /// <summary>이번 판 데이터 (마법사 이름·파티·의뢰)</summary>
        public RunEntry Run { get; private set; }

        /// <summary>지금 흐름 상태. 시작 전엔 null</summary>
        FlowStateBase _state;

        /// <summary>상태 전환 진행 중이면 true. 중복 전환 막기용</summary>
        bool _isSwitching;

        #region Start - 매니저 확인 → 시작 상태 고르기 → (타이틀부터면) 로고 → 다국어 준비 대기 → 시작 상태로 진입 (매니저끼리 Awake 순서가 없어서 Start에서)
        IEnumerator Start()
        {
            // 하나라도 빠지면 Boot 씬 구성이 잘못된 것 (빠진 쪽은 Instance가 에러를 남김)
            bool allReady = TransitionManager.Instance != null && UIManager.Instance != null
                && DataManager.Instance != null && SettingsManager.Instance != null
                && AudioManager.Instance != null && TutorialManager.Instance != null;
            if (!allReady) yield break;

            // 시작할 흐름 상태 (빌드: Title, 에디터: 편집 중이던 씬)
            FlowStateBase start = CreateStartState();
            if (start == null) yield break;

            // 게임을 켤 때(타이틀부터)만 엔진·회사 로고. 로고가 도는 동안 다국어 준비·첫 씬 로드를 진행 (먼저 끝나거나 넘기면 밑의 로딩창이 보임)
            if (start is TitleState) TransitionManager.Instance.ShowLogo();
            // 로고 밑 검은 덮개. 에디터에서 중간 씬부터 켜면 이게 화면을 덮음
            TransitionManager.Instance.ShowCover();
            // 다국어 초기화 작업
            var localization = LocalizationSettings.InitializationOperation;
            yield return localization;
            if (localization.Status != AsyncOperationStatus.Succeeded)
                Debug.LogError("[Flow] 다국어 초기화 실패. 화면 글자가 안 나올 수 있음");

            // 타이틀·생성 화면이 아니면 중간부터 시작한 것 → 디버그 데이터
            Run = start is TitleState or CharacterCreationState ? new RunEntry() : RunEntry.CreateDebug(DataManager.Instance.Elements);
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

        #region SwitchState - 덮개(팁 로딩창 또는 검은 덮개) → 이전 상태 Exit → 씬 교체 → 새 상태 Begin → 글자 대기 → 로고·키 입력 대기 → 덮개 해제 → 새 상태 Reveal
        /// <param name="next">들어갈 상태</param>
        IEnumerator SwitchState(FlowStateBase next)
        {
            // 덮개·씬 교체 담당
            TransitionManager transition = TransitionManager.Instance;
            // 게임을 켠 뒤 첫 진입인지. 키 입력을 안 기다림 (Title부터면 로고가 덮음)
            bool firstEntry = _state == null;
            // 넘기기 버튼을 기다릴지. 들어갈 상태가 정함 (첫 진입은 안 기다림)
            bool waitForContinue = !firstEntry && next.WaitsForContinue;
            // 팁 로딩창을 띄울지: 넘기기 버튼을 기다릴 때, 또는 로고 밑 첫 진입 (로고가 로딩보다 먼저 끝나거나 넘기면 보임)
            bool showTip = waitForContinue || (firstEntry && transition.LogoShown);
            _isSwitching = true;
            try
            {
                if (showTip) yield return transition.ShowLoading();
                else transition.ShowCover();
                Debug.Log($"[Flow] {_state?.GetType().Name ?? "시작"} → {next.GetType().Name}");
                _state?.Exit();
                _state = next;
                // 단계 시작 시각 (실제 시간). 단계마다 걸린 시간을 로그로 남겨 바 몫을 맞출 때 씀
                float stepStart = Time.realtimeSinceStartup;
                yield return transition.LoadScene(next.SceneName, _sceneLoadShare);
                // 씬 로드에 걸린 시간 (초)
                float loadSeconds = Time.realtimeSinceStartup - stepStart;
                stepStart = Time.realtimeSinceStartup;
                // (나중) 상태별 준비 작업 자리: 던전 생성·스폰. 덮개가 덮인 채로
                next.Begin(this);
                transition.SetProgress(Mathf.Min(1f, _sceneLoadShare + _sceneReadyShare));
                // 씬에 정보 적용에 걸린 시간 (초)
                float readySeconds = Time.realtimeSinceStartup - stepStart;
                stepStart = Time.realtimeSinceStartup;
                // 글자 테이블을 다 읽어야 걷음. 폰트는 글자가 켜질 때 키트가 바로 바꿔서 따로 안 기다림
                yield return transition.WaitForLocalizedTexts();
                transition.SetProgress(1f);
                Debug.Log($"[Flow] {next.SceneName} 로딩 시간(초): 씬 로드 {loadSeconds:F2}, 정보 적용 {readySeconds:F2}, 다국어 {Time.realtimeSinceStartup - stepStart:F2}");
                // 첫 진입은 로고가 끝나거나 넘길 때까지, 키 입력 대기 상태는 넘기기 버튼을 누를 때까지 덮어 둠. 나머지는 바로 걷음
                if (firstEntry) yield return transition.WaitForLogo();
                if (waitForContinue) yield return transition.WaitForContinue();
            }
            finally
            {
                // 중간에 예외가 나도 덮개·잠금·게임 시간이 남지 않게 (게임 시간은 HideCover가 되돌림)
                transition.HideCover();
                _isSwitching = false;
            }
            next.Reveal();
        }
        #endregion
    }
}
