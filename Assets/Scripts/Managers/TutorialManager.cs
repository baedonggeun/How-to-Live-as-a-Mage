using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>튜토리얼 진행 담당: 게임 코드가 상황(튜토리얼 id)을 알리면 튜토리얼을 찾아 진행 캡슐(TutorialService)에 넘기고, 매 프레임 입력(이동·달리기·둘러보기)을 읽어 넘김. 캡슐 알림을 받아 12시 안내를 맞추고 배운 기록을 이 PC에 저장. 다른 코드가 튜토리얼 중인지 묻지 않게 알림만 받음</summary>
    public sealed class TutorialManager : MonoSingleton<TutorialManager>
    {
        /// <summary>이동 입력 액션 경로 (전역 액션)</summary>
        const string MoveActionPath = "Player/Move";

        /// <summary>시점 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>달리기 입력 액션 경로 (전역 액션)</summary>
        const string SprintActionPath = "Player/Sprint";

        /// <summary>움직이는 중으로 볼 이동 입력 크기 (0~1)</summary>
        const float MoveInputThreshold = 0.5f;

        /// <summary>화면 12시 안내 (Boot 씬 UICanvas/TutorialPrompt. 인스펙터에서만 연결)</summary>
        [SerializeField] TutorialPromptView _prompt;

        /// <summary>이동을 배웠다고 칠 이동 입력 시간 (초). 걷기 5.6m/s면 약 2m (시작할 때 진행 캡슐에 넘김)</summary>
        [SerializeField, Min(0f)] float _moveSeconds = 0.36f;

        /// <summary>둘러보기를 배웠다고 칠 마우스 이동량 합. PlayerController 기본 감도 0.1이면 약 90° (시작할 때 진행 캡슐에 넘김)</summary>
        [SerializeField, Min(0f)] float _lookAmount = 900f;

        /// <summary>달리기를 배웠다고 칠, 움직이며 달린 시간 (초. 시작할 때 진행 캡슐에 넘김)</summary>
        [SerializeField, Min(0f)] float _sprintSeconds = 1f;

        /// <summary>안내 하나가 끝나고 다음 안내를 띄우기까지 쉬는 시간 (초. 시작할 때 진행 캡슐에 넘김)</summary>
        [SerializeField, Min(0f)] float _gapSeconds = 0.5f;

        /// <summary>튜토리얼 진행 캡슐 (대기열·진행량·쉬는 시간·전투 중 숨김)</summary>
        TutorialService _service;

        /// <summary>이동 입력. 없으면 null</summary>
        InputAction _move;

        /// <summary>시점 입력. 없으면 null</summary>
        InputAction _look;

        /// <summary>달리기 입력. 없으면 null</summary>
        InputAction _sprint;

        #region InitSingleton - 안내 연결 확인·숨김, 입력 액션 찾기, 진행 캡슐을 만들어 알림(배움 → 저장, 안내 바뀜 → 12시 안내) 구독
        protected override void InitSingleton()
        {
            if (_prompt == null) Debug.LogError("[Tutorial] TutorialPromptView 연결 안 됨");
            else _prompt.Root.SetActive(false);
            _move = FindAction(MoveActionPath);
            _look = FindAction(LookActionPath);
            _sprint = FindAction(SprintActionPath);
            _service = new TutorialService(IsLearned, _moveSeconds, _lookAmount, _sprintSeconds, _gapSeconds);
            _service.Learned += SaveLearned;
            _service.PromptChanged += OnPromptChanged;
        }
        #endregion

        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[Tutorial] {path} 액션 없음");
            return action;
        }
        #endregion

        #region Notify - 상황 알림: 모르는 id·12시 안내가 없는 튜토리얼은 경고, 아니면 진행 캡슐에 넘김 (배웠거나 이미 다루는 중이면 캡슐이 무시)
        /// <param name="id">튜토리얼 id (TutorialSO.Id)</param>
        public void Notify(string id)
        {
            // 알린 튜토리얼 (없으면 null)
            TutorialSO tutorial = DataManager.Instance.FindTutorial(id);
            if (tutorial == null)
            {
                Debug.LogWarning($"[Tutorial] 모르는 튜토리얼 id: {id}");
                return;
            }
            if (tutorial.Prompt.IsEmpty)
            {
                Debug.LogWarning($"[Tutorial] {id}는 12시 안내가 없음. MarkLearned로 알려야 함");
                return;
            }
            _service.Notify(tutorial);
        }
        #endregion

        #region MarkLearned - 배움으로 기록 (상호작용·지도·현장 시전처럼 게임 코드가 처음 쓴 걸 알림). 진행 캡슐이 저장 알림을 내고, 지금 다루는 안내면 끝냄
        /// <param name="id">튜토리얼 id</param>
        public void MarkLearned(string id) => _service.MarkLearned(id);
        #endregion

        #region SaveLearned - 이 PC 배운 기록에 넣고 새로 배웠으면 파일 저장 (진행 캡슐 배움 알림)
        /// <param name="id">튜토리얼 id</param>
        void SaveLearned(string id)
        {
            // 기기 설정 담당 (배운 기록이 여기 있음)
            var settings = SettingsManager.Instance;
            if (settings != null && settings.Device.AddLearnedTutorial(id)) settings.Save();
        }
        #endregion

        #region SetSuppressed - 전투 상황이면 12시 안내를 숨기고, 끝나면 떠 있던 안내를 다시 보임 (숲)
        /// <param name="suppressed">숨길지</param>
        public void SetSuppressed(bool suppressed) => _service.SetSuppressed(suppressed);
        #endregion

        #region IsLearned - 이 PC에서 배운 튜토리얼인지
        /// <param name="id">튜토리얼 id</param>
        public bool IsLearned(string id) => SettingsManager.Instance.Device.HasLearnedTutorial(id);
        #endregion

        #region Clear - 떠 있는 안내와 대기열을 비움 (상태를 나갈 때. 다음에 다시 알리면 다시 나옴)
        public void Clear() => _service.Clear();
        #endregion

        #region Update - 게임 시간이 멈췄으면(로딩창·일시정지·메뉴) 안 셈. 아니면 이번 프레임 입력을 진행 캡슐에 넘김
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            _service.Tick(Time.deltaTime, IsMoving(), IsSprinting(), LookAmount());
        }
        #endregion

        #region OnPromptChanged - 진행 캡슐 안내 알림: 튜토리얼이 있으면 그 안내 글을 넣고, 보일지에 따라 12시 안내를 켜고 끔
        /// <param name="tutorial">보일 튜토리얼 (숨길 땐 null)</param>
        /// <param name="visible">화면에 보일지</param>
        void OnPromptChanged(TutorialSO tutorial, bool visible)
        {
            if (_prompt == null) return;
            if (tutorial != null) _prompt.Text.StringReference.SetReference(tutorial.Prompt.TableReference, tutorial.Prompt.TableEntryReference);
            _prompt.Root.SetActive(visible);
        }
        #endregion

        #region IsMoving - 이동 입력이 들어오는지
        bool IsMoving() => _move != null && _move.ReadValue<Vector2>().magnitude >= MoveInputThreshold;
        #endregion

        #region IsSprinting - 달리기 입력이 눌려 있는지
        bool IsSprinting() => _sprint != null && _sprint.IsPressed();
        #endregion

        #region LookAmount - 이번 프레임 마우스 이동량 (시점 입력이 없으면 0)
        float LookAmount() => _look != null ? _look.ReadValue<Vector2>().magnitude : 0f;
        #endregion
    }
}
