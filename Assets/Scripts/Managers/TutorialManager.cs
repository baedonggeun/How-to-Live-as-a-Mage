using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>튜토리얼 진행 담당: 게임 코드가 상황(튜토리얼 id)을 알리면 안 배운 조작 안내만 화면 12시에 하나씩 띄우고, 입력을 보고 실제로 하면 배움으로 이 PC에 저장. 다른 코드가 튜토리얼 중인지 묻지 않게 알림만 받음</summary>
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

        /// <summary>이동을 배웠다고 칠 이동 입력 시간 (초). 걷기 5.6m/s면 약 2m</summary>
        [SerializeField, Min(0f)] float _moveSeconds = 0.36f;

        /// <summary>둘러보기를 배웠다고 칠 마우스 이동량 합. PlayerController 기본 감도 0.1이면 약 90°</summary>
        [SerializeField, Min(0f)] float _lookAmount = 900f;

        /// <summary>달리기를 배웠다고 칠, 움직이며 달린 시간 (초)</summary>
        [SerializeField, Min(0f)] float _sprintSeconds = 1f;

        /// <summary>안내 하나가 끝나고 다음 안내를 띄우기까지 쉬는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _gapSeconds = 0.5f;

        /// <summary>띄울 차례를 기다리는 튜토리얼 (알린 순서)</summary>
        readonly Queue<TutorialSO> _queue = new();

        /// <summary>지금 다루는 튜토리얼. 없으면 null</summary>
        TutorialSO _current;

        /// <summary>지금 안내가 화면에 떠 있는지 (걷기를 기다리는 동안은 false)</summary>
        bool _shown;

        /// <summary>지금 튜토리얼의 진행량 (안내가 뜨기 전 것도 셈. 행동마다 단위가 다름: 초 또는 마우스 이동량)</summary>
        float _progress;

        /// <summary>지금 안내를 띄우기 전에 달리지 않고 걸은 시간 (초)</summary>
        float _walked;

        /// <summary>다음 안내까지 남은 쉬는 시간 (초)</summary>
        float _gap;

        /// <summary>전투 상황이라 12시 안내를 숨기는지 (다루던 안내는 그대로 두고 화면에서만 숨김)</summary>
        bool _suppressed;

        /// <summary>이동 입력. 없으면 null</summary>
        InputAction _move;

        /// <summary>시점 입력. 없으면 null</summary>
        InputAction _look;

        /// <summary>달리기 입력. 없으면 null</summary>
        InputAction _sprint;

        #region InitSingleton - 안내 연결 확인·숨김, 입력 액션 찾기
        protected override void InitSingleton()
        {
            if (_prompt == null) Debug.LogError("[Tutorial] TutorialPromptView 연결 안 됨");
            else _prompt.Root.SetActive(false);
            _move = FindAction(MoveActionPath);
            _look = FindAction(LookActionPath);
            _sprint = FindAction(SprintActionPath);
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

        #region Notify - 상황 알림: 모르는 id·12시 안내가 없는 튜토리얼은 경고, 배웠거나 이미 다루는 중이면 무시. 아니면 대기열에 넣고 쉬는 중이 아니면 바로 시작
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
            if (IsLearned(id) || _current == tutorial || _queue.Contains(tutorial)) return;
            _queue.Enqueue(tutorial);
            if (_current == null && _gap <= 0f) StartNext();
        }
        #endregion

        #region MarkLearned - 배움으로 기록 (상호작용·지도·현장 시전처럼 게임 코드가 처음 쓴 걸 알림). 새로 배웠으면 파일 저장. 지금 다루는 안내면 끝냄
        /// <param name="id">튜토리얼 id</param>
        public void MarkLearned(string id)
        {
            SaveLearned(id);
            if (_current != null && string.Equals(_current.Id, id, System.StringComparison.Ordinal)) Complete();
        }
        #endregion

        #region SaveLearned - 이 PC 배운 기록에 넣고 새로 배웠으면 파일 저장
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
        public void SetSuppressed(bool suppressed)
        {
            if (suppressed == _suppressed) return;
            _suppressed = suppressed;
            if (_prompt != null && _current != null && _shown) _prompt.Root.SetActive(!suppressed);
        }
        #endregion

        #region IsLearned - 이 PC에서 배운 튜토리얼인지
        /// <param name="id">튜토리얼 id</param>
        public bool IsLearned(string id) => SettingsManager.Instance.Device.HasLearnedTutorial(id);
        #endregion

        #region Clear - 떠 있는 안내와 대기열을 비움 (상태를 나갈 때. 다음에 다시 알리면 다시 나옴)
        public void Clear()
        {
            _queue.Clear();
            _current = null;
            _shown = false;
            _gap = 0f;
            _suppressed = false;
            if (_prompt != null) _prompt.Root.SetActive(false);
        }
        #endregion

        #region Update - 게임 시간이 멈췄으면(로딩창·일시정지·메뉴) 안 셈. 쉬는 중이면 기다림. 행동 진행량은 안내가 뜨기 전에도 셈(다 차면 안내 없이 배움). 안 떠 있으면 걷기를 기다렸다 띄움
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (_current == null)
            {
                if (_gap <= 0f) return;
                _gap -= dt;
                if (_gap <= 0f) StartNext();
                return;
            }
            // 안내가 뜨기 전에 한 것도 셈 (처음부터 달리던 플레이어에겐 달리기 안내를 안 띄움)
            _progress += Measure(_current.Action, dt);
            if (_progress >= Goal(_current.Action))
            {
                Complete();
                return;
            }
            if (_shown) return;
            if (IsMoving() && !IsSprinting()) _walked += dt;
            if (_walked >= _current.WalkSecondsBeforeShow) Show();
        }
        #endregion

        #region StartNext - 대기열에서 안 배운 다음 튜토리얼을 꺼내 다룸 (걸어야 할 시간이 0이면 바로 띄움)
        void StartNext()
        {
            _current = null;
            while (_queue.Count > 0)
            {
                // 다음 차례
                TutorialSO next = _queue.Dequeue();
                if (IsLearned(next.Id)) continue;
                _current = next;
                _shown = false;
                _progress = 0f;
                _walked = 0f;
                if (next.WalkSecondsBeforeShow <= 0f) Show();
                return;
            }
        }
        #endregion

        #region Show - 지금 튜토리얼의 안내를 12시에 띄움
        void Show()
        {
            _shown = true;
            if (_prompt == null) return;
            _prompt.Text.StringReference.SetReference(_current.Prompt.TableReference, _current.Prompt.TableEntryReference);
            _prompt.Root.SetActive(!_suppressed);
        }
        #endregion

        #region Complete - 배움 저장, 안내 숨김, 쉬는 시간 뒤 다음 안내
        void Complete()
        {
            SaveLearned(_current.Id);
            _current = null;
            _shown = false;
            if (_prompt != null) _prompt.Root.SetActive(false);
            _gap = _gapSeconds;
            if (_gap <= 0f) StartNext();
        }
        #endregion

        #region Measure - 이번 프레임 행동 진행량 (이동·달리기: 초, 둘러보기: 마우스 이동량)
        /// <param name="action">배울 행동</param>
        /// <param name="dt">이번 프레임 시간 (초)</param>
        float Measure(TutorialAction action, float dt) => action switch
        {
            TutorialAction.Move => IsMoving() ? dt : 0f,
            TutorialAction.Look => _look != null ? _look.ReadValue<Vector2>().magnitude : 0f,
            TutorialAction.Sprint => IsMoving() && IsSprinting() ? dt : 0f,
            _ => 0f,
        };
        #endregion

        #region Goal - 배웠다고 칠 진행량
        /// <param name="action">배울 행동</param>
        float Goal(TutorialAction action) => action switch
        {
            TutorialAction.Move => _moveSeconds,
            TutorialAction.Look => _lookAmount,
            TutorialAction.Sprint => _sprintSeconds,
            _ => float.MaxValue,
        };
        #endregion

        #region IsMoving - 이동 입력이 들어오는지
        bool IsMoving() => _move != null && _move.ReadValue<Vector2>().magnitude >= MoveInputThreshold;
        #endregion

        #region IsSprinting - 달리기 입력이 눌려 있는지
        bool IsSprinting() => _sprint != null && _sprint.IsPressed();
        #endregion
    }
}
