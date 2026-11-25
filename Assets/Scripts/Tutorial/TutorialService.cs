using System;
using System.Collections.Generic;

namespace Mage
{
    /// <summary>튜토리얼 진행 캡슐 (MonoBehaviour 아님. TutorialManager가 만들어 씀): 알린 튜토리얼을 대기열에 넣고 하나씩 다룸. 달리지 않고 걸은 시간이 차면 12시 안내를 띄우고, 행동 진행량(이동·달리기: 초, 둘러보기: 마우스 이동량)이 목표에 닿으면 배움으로 알리고 쉬는 시간 뒤 다음 안내. 진행량은 안내가 뜨기 전에도 셈. 전투 중엔 안내를 화면에서만 숨김. 입력은 매니저가 매 프레임 넘기고, 저장·안내 화면은 매니저가 함 (Learned·PromptChanged)</summary>
    public sealed class TutorialService
    {
        /// <summary>이 PC에서 배운 튜토리얼인지 (튜토리얼 id → 배웠는지)</summary>
        readonly Func<string, bool> _isLearned;

        /// <summary>이동을 배웠다고 칠 이동 입력 시간 (초)</summary>
        readonly float _moveSeconds;

        /// <summary>둘러보기를 배웠다고 칠 마우스 이동량 합</summary>
        readonly float _lookAmount;

        /// <summary>달리기를 배웠다고 칠, 움직이며 달린 시간 (초)</summary>
        readonly float _sprintSeconds;

        /// <summary>안내 하나가 끝나고 다음 안내를 띄우기까지 쉬는 시간 (초)</summary>
        readonly float _gapSeconds;

        /// <summary>띄울 차례를 기다리는 튜토리얼 (알린 순서)</summary>
        readonly Queue<TutorialSO> _queue = new();

        /// <summary>지금 다루는 튜토리얼. 없으면 null</summary>
        TutorialSO _current;

        /// <summary>지금 안내가 떠 있는지 (걷기를 기다리는 동안은 false)</summary>
        bool _shown;

        /// <summary>지금 튜토리얼의 진행량 (안내가 뜨기 전 것도 셈. 행동마다 단위가 다름: 초 또는 마우스 이동량)</summary>
        float _progress;

        /// <summary>지금 안내를 띄우기 전에 달리지 않고 걸은 시간 (초)</summary>
        float _walked;

        /// <summary>다음 안내까지 남은 쉬는 시간 (초)</summary>
        float _gap;

        /// <summary>전투 상황이라 12시 안내를 숨기는지 (다루던 안내는 그대로 두고 화면에서만 숨김)</summary>
        bool _suppressed;

        /// <summary>배움으로 정함 (튜토리얼 id). 매니저가 이 PC 기록에 넣고 새로 배웠으면 저장</summary>
        public event Action<string> Learned;

        /// <summary>12시 안내가 바뀜 (보일 튜토리얼 — 다 끝났거나 비웠으면 null, 화면에 보일지). 매니저가 안내 글·켜기를 맞춤</summary>
        public event Action<TutorialSO, bool> PromptChanged;

        #region TutorialService - 배웠는지 묻는 함수와 배움 목표·쉬는 시간을 받음
        /// <param name="isLearned">이 PC에서 배운 튜토리얼인지</param>
        /// <param name="moveSeconds">이동을 배웠다고 칠 이동 입력 시간 (초)</param>
        /// <param name="lookAmount">둘러보기를 배웠다고 칠 마우스 이동량 합</param>
        /// <param name="sprintSeconds">달리기를 배웠다고 칠, 움직이며 달린 시간 (초)</param>
        /// <param name="gapSeconds">안내 사이 쉬는 시간 (초)</param>
        public TutorialService(Func<string, bool> isLearned, float moveSeconds, float lookAmount, float sprintSeconds, float gapSeconds)
        {
            _isLearned = isLearned ?? throw new ArgumentNullException(nameof(isLearned));
            _moveSeconds = moveSeconds;
            _lookAmount = lookAmount;
            _sprintSeconds = sprintSeconds;
            _gapSeconds = gapSeconds;
        }
        #endregion

        #region Notify - 배웠거나 이미 다루는 중·기다리는 중이면 무시. 아니면 대기열에 넣고 쉬는 중이 아니면 바로 시작 (모르는 id·12시 안내가 없는 튜토리얼은 매니저가 거름)
        /// <param name="tutorial">알린 튜토리얼</param>
        public void Notify(TutorialSO tutorial)
        {
            if (tutorial == null) throw new ArgumentNullException(nameof(tutorial));
            if (_isLearned(tutorial.Id) || _current == tutorial || _queue.Contains(tutorial)) return;
            _queue.Enqueue(tutorial);
            if (_current == null && _gap <= 0f) StartNext();
        }
        #endregion

        #region MarkLearned - 배움으로 알림 (상호작용·지도·현장 시전처럼 게임 코드가 처음 쓴 걸 알림). 지금 다루는 안내면 끝냄
        /// <param name="id">튜토리얼 id</param>
        public void MarkLearned(string id)
        {
            Learned?.Invoke(id);
            if (_current != null && string.Equals(_current.Id, id, StringComparison.Ordinal)) Complete();
        }
        #endregion

        #region SetSuppressed - 전투 상황이면 12시 안내를 화면에서 숨기고, 끝나면 떠 있던 안내를 다시 보임 (숲)
        /// <param name="suppressed">숨길지</param>
        public void SetSuppressed(bool suppressed)
        {
            if (suppressed == _suppressed) return;
            _suppressed = suppressed;
            if (_current != null && _shown) PromptChanged?.Invoke(_current, !suppressed);
        }
        #endregion

        #region Clear - 다루던 안내와 대기열을 비우고 안내를 숨김 (상태를 나갈 때. 다음에 다시 알리면 다시 나옴)
        public void Clear()
        {
            _queue.Clear();
            _current = null;
            _shown = false;
            _gap = 0f;
            _suppressed = false;
            PromptChanged?.Invoke(null, false);
        }
        #endregion

        #region Tick - 한 프레임: 쉬는 중이면 기다렸다 다음 안내. 다루는 중이면 행동 진행량을 세고(안내가 뜨기 전에도) 목표에 닿으면 배움, 안 떠 있으면 달리지 않고 걸은 시간을 세다 차면 띄움 (게임 시간이 멈췄을 땐 매니저가 안 부름)
        /// <param name="dt">이번 프레임 시간 (초)</param>
        /// <param name="moving">이동 입력이 들어오는지</param>
        /// <param name="sprinting">달리기 입력이 눌려 있는지</param>
        /// <param name="look">이번 프레임 마우스 이동량</param>
        public void Tick(float dt, bool moving, bool sprinting, float look)
        {
            if (_current == null)
            {
                if (_gap <= 0f) return;
                _gap -= dt;
                if (_gap <= 0f) StartNext();
                return;
            }
            // 안내가 뜨기 전에 한 것도 셈 (처음부터 달리던 플레이어에겐 달리기 안내를 안 띄움)
            _progress += Measure(_current.Action, dt, moving, sprinting, look);
            if (_progress >= Goal(_current.Action))
            {
                Complete();
                return;
            }
            if (_shown) return;
            if (moving && !sprinting) _walked += dt;
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
                if (_isLearned(next.Id)) continue;
                _current = next;
                _shown = false;
                _progress = 0f;
                _walked = 0f;
                if (next.WalkSecondsBeforeShow <= 0f) Show();
                return;
            }
        }
        #endregion

        #region Show - 지금 튜토리얼의 안내를 띄우라고 알림 (전투 중이면 숨긴 채)
        void Show()
        {
            _shown = true;
            PromptChanged?.Invoke(_current, !_suppressed);
        }
        #endregion

        #region Complete - 배움 알림, 안내 숨김 알림, 쉬는 시간 뒤 다음 안내
        void Complete()
        {
            Learned?.Invoke(_current.Id);
            _current = null;
            _shown = false;
            PromptChanged?.Invoke(null, false);
            _gap = _gapSeconds;
            if (_gap <= 0f) StartNext();
        }
        #endregion

        #region Measure - 이번 프레임 행동 진행량 (이동·달리기: 초, 둘러보기: 마우스 이동량)
        /// <param name="action">배울 행동</param>
        /// <param name="dt">이번 프레임 시간 (초)</param>
        /// <param name="moving">이동 입력이 들어오는지</param>
        /// <param name="sprinting">달리기 입력이 눌려 있는지</param>
        /// <param name="look">이번 프레임 마우스 이동량</param>
        static float Measure(TutorialAction action, float dt, bool moving, bool sprinting, float look) => action switch
        {
            TutorialAction.Move => moving ? dt : 0f,
            TutorialAction.Look => look,
            TutorialAction.Sprint => moving && sprinting ? dt : 0f,
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
    }
}
