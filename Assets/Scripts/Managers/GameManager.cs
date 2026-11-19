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

        /// <summary>창이 비활성일 때 프레임 제한. 멈춘 화면·로딩창을 수백 fps로 그리지 않게 (PC 품질은 vSync 꺼짐)</summary>
        const int BackgroundFrameRate = 30;

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

        /// <summary>로딩창이 떠 있는지. 떠 있는 동안 게임 시간 0</summary>
        bool _covered;

        /// <summary>로딩 중인지 (켤 때·전환 시작 ~ 바 1). 이 동안은 Run In Background를 켜서 창이 비활성이어도 로딩이 끝까지 감</summary>
        bool _loading;

        /// <summary>창이 비활성인지 (빌드만). 비활성이면 게임 시간 0, 소리 멈춤, 프레임 제한</summary>
        bool _unfocused;

        /// <summary>프로젝트 설정 Run In Background 값. 로딩이 끝나면 이 값으로 되돌림</summary>
        bool _runInBackgroundSetting;

        /// <summary>창이 비활성이 되기 전 프레임 제한. 창으로 돌아오면 이 값으로 되돌림</summary>
        int _focusedFrameRate;

        #region Start - 매니저 확인 → 시작 상태 고르기 → (타이틀부터면) 로고 → 로딩 표시 → 다국어 준비 대기 → 시작 상태로 진입 (매니저끼리 Awake 순서가 없어서 Start에서)
        IEnumerator Start()
        {
            // 하나라도 빠지면 Boot 씬 구성이 잘못된 것 (빠진 쪽은 Instance가 에러를 남김)
            bool allReady = TransitionManager.Instance != null && UIManager.Instance != null
                && DataManager.Instance != null && SettingsManager.Instance != null
                && AudioManager.Instance != null && TutorialManager.Instance != null;
            if (!allReady) yield break;

            // 로딩이 끝날 때마다 되돌릴 값이라 로딩 전에 기억
            _runInBackgroundSetting = Application.runInBackground;
            // 시작할 흐름 상태 (빌드: Title, 에디터: 편집 중이던 씬)
            FlowStateBase start = CreateStartState();
            if (start == null) yield break;

            // 게임을 켤 때(타이틀부터)만 엔진·회사 로고. 로고가 도는 동안 다국어 준비·첫 씬 로드를 진행 (먼저 끝나거나 넘기면 밑의 로딩창이 보임)
            if (start is TitleState) TransitionManager.Instance.ShowLogo();
            // 로고 밑 검은 덮개. 에디터에서 중간 씬부터 켜면 이게 화면을 덮음
            TransitionManager.Instance.ShowCover();
            _covered = true;
            ApplyTimeScale();
            // 켜자마자 창을 비활성으로 돌려도 다국어 준비·첫 씬 로드는 끝까지 가게
            SetLoading(true);
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
                // 창이 비활성이어도 로딩은 끝까지 가게 (바가 1이 되면 되돌림)
                SetLoading(true);
                if (showTip) yield return transition.ShowLoading();
                else transition.ShowCover();
                // 로딩창 밑에서 물리·애니메이션·적이 안 돌게. 로딩창 연출·로딩 대기는 실제 시간이라 그대로 돎
                _covered = true;
                ApplyTimeScale();
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
                // 로딩 끝. Run In Background가 꺼진 설정이면 여기서부터 창이 비활성일 때 Unity가 루프를 멈춤 (로고·안내 대기도 같이 멈춤)
                SetLoading(false);
                // 첫 진입은 로고가 끝나거나 넘길 때까지, 키 입력 대기 상태는 넘기기 버튼을 누를 때까지 덮어 둠. 나머지는 바로 걷음
                if (firstEntry) yield return transition.WaitForLogo();
                if (waitForContinue) yield return transition.WaitForContinue();
            }
            finally
            {
                // 중간에 예외가 나도 덮개·잠금·게임 시간·Run In Background가 남지 않게
                transition.HideCover();
                _covered = false;
                ApplyTimeScale();
                SetLoading(false);
                _isSwitching = false;
            }
            next.Reveal();
        }
        #endregion

        #region SetLoading - 로딩 중 표시. 로딩 중엔 Run In Background를 켜서 창이 비활성이어도 루프가 돌게, 끝나면 프로젝트 설정값으로 (빌드만)
        /// <param name="loading">로딩 중인지</param>
        void SetLoading(bool loading)
        {
            _loading = loading;
            // 에디터는 MCP·사용자의 Run In Background 덮어쓰기와 안 싸우게 빌드만
            if (!Application.isEditor) Application.runInBackground = loading || _runInBackgroundSetting;
        }
        #endregion

        #region ApplyTimeScale - 멈출 이유(로딩창·창 비활성)가 하나라도 있으면 게임 시간 0, 없으면 1
        void ApplyTimeScale() => Time.timeScale = _covered || _unfocused ? 0f : 1f;
        #endregion

        #region SetUnfocused - 창 비활성 처리: 게임 시간 0, 소리 멈춤(돌아오면 이어 재생), 프레임 제한. 돌아오면 되돌림. 로딩은 SetLoading이 따로 계속 돌림
        /// <param name="unfocused">창이 비활성인지</param>
        void SetUnfocused(bool unfocused)
        {
            if (unfocused == _unfocused) return;
            _unfocused = unfocused;
            ApplyTimeScale();
            AudioListener.pause = unfocused;
            if (unfocused)
            {
                _focusedFrameRate = Application.targetFrameRate;
                Application.targetFrameRate = BackgroundFrameRate;
            }
            else
            {
                Application.targetFrameRate = _focusedFrameRate;
            }
        }
        #endregion

        #region OnApplicationFocus - 창이 활성·비활성이 될 때 Unity가 부름. 빌드만 (에디터 Play는 다른 창을 보면서 확인·MCP 검증할 때도 돌아야 해서)
        /// <param name="hasFocus">창이 활성인지</param>
        void OnApplicationFocus(bool hasFocus)
        {
            if (Application.isEditor) return;
            SetUnfocused(!hasFocus);
        }
        #endregion
    }
}
