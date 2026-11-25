using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>캐릭터 생성. 이름과 습득할 룬을 받아 판 데이터에 넣고 오프닝으로</summary>
    public sealed class CharacterCreationState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "CharacterCreation";

        /// <summary>이름 최대 글자 수</summary>
        public const int NameMaxLength = 12;

        /// <summary>캐릭터 생성에서 고를 룬 수 (임시. 기획 02·12 미정)</summary>
        public const int StartRuneCount = 4;

        /// <summary>이름이 비었을 때 토스트 키</summary>
        const string NameEmptyKey = "creation.name_empty";

        /// <summary>룬이 모자랄 때 토스트 키 ({0} 골라야 할 수)</summary>
        const string RuneRequiredKey = "creation.rune_required";

        /// <summary>시작 확인창 문구 키</summary>
        const string StartConfirmKey = "creation.start_confirm";

        /// <summary>생성 화면 문구 테이블</summary>
        static readonly TableReference Table = "Creation";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>이 씬의 화면 참조</summary>
        CharacterCreationView _view;

        /// <summary>만든 카드와 그 룬 (DataManager 순서)</summary>
        readonly List<(RuneCardView card, RuneSO rune)> _cards = new();

        #region OnSceneReady - 이름 입력 규칙, 확정 버튼, 룬 카드, 고른 수 표시
        protected override void OnSceneReady()
        {
            _view = FindView<CharacterCreationView>();
            if (_view == null) return;
            _view.NameInput.characterLimit = NameMaxLength;
            _view.NameInput.onValidateInput = FilterNameChar;
            _view.ConfirmButton.onClick.AddListener(OnConfirm);
            CreateCards();
            RefreshSelection();
        }
        #endregion

        #region Reveal - 배경음 재생
        public override void Reveal()
        {
            // 비어 있으면 AudioManager가 무시함 (타이틀 곡이 이어짐)
            if (_view != null) AudioManager.Instance.PlayBgm(_view.Bgm);
        }
        #endregion

        #region CreateCards - 룬마다 카드를 만들어 이름·그림을 넣고 이름·설명은 툴팁으로, 켜고 끌 때 고른 수를 다시 셈
        void CreateCards()
        {
            if (_view.RuneCardPrefab == null || _view.RuneGrid == null)
            {
                Debug.LogError("[Creation] 카드 프리팹·카드 자리 연결 안 됨");
                return;
            }
            foreach (var rune in DataManager.Instance.Runes)
            {
                // 빈 칸은 DataManager가 이미 에러를 남김
                if (rune == null) continue;
                // 이 룬의 카드 (생성 씬 카드 자리 아래라 씬과 같이 지워짐)
                RuneCardView card = Object.Instantiate(_view.RuneCardPrefab, _view.RuneGrid);
                card.name = rune.Id;
                // SO의 LocalizedString을 같이 쓰지 않게 테이블·키만 넣음
                card.NameText.StringReference.SetReference(rune.Name.TableReference, rune.Name.TableEntryReference);
                card.Tooltip.Set(rune.Name, rune.Description);
                card.Icon.sprite = rune.Icon;
                card.Icon.enabled = rune.Icon != null;
                card.Toggle.SetIsOnWithoutNotify(false);
                card.Toggle.onValueChanged.AddListener(OnCardToggled);
                _cards.Add((card, rune));
            }
        }
        #endregion

        #region OnCardToggled - 카드를 켜거나 끄면 고른 수·잠금을 다시 맞춤
        /// <param name="isOn">켜졌는지 (안 씀, 카드 전부를 다시 셈)</param>
        void OnCardToggled(bool isOn) => RefreshSelection();
        #endregion

        #region RefreshSelection - 고른 수 표시, 정해진 수를 다 고르면 안 고른 카드 잠금
        void RefreshSelection()
        {
            // 지금 고른 수
            int count = CountSelected();
            // 다 골랐는지
            bool full = count >= StartRuneCount;
            foreach (var (card, _) in _cards)
                card.Toggle.interactable = card.Toggle.isOn || !full;
            _view.RuneCountText.StringReference.Arguments = new object[] { count, StartRuneCount };
            _view.RuneCountText.RefreshString();
        }
        #endregion

        #region CountSelected - 켜진 카드 수
        int CountSelected()
        {
            // 켜진 카드 수
            int count = 0;
            foreach (var (card, _) in _cards)
                if (card.Toggle.isOn) count++;
            return count;
        }
        #endregion

        #region OnConfirm - 이름 빈칸·룬 모자람이면 토스트, 다 갖췄으면 시작 확인창
        void OnConfirm()
        {
            // 앞뒤 공백을 지운 이름
            string playerName = _view.NameInput.text.Trim();
            if (playerName.Length == 0)
            {
                UIManager.Instance.ShowToast(Table, NameEmptyKey);
                return;
            }
            if (CountSelected() != StartRuneCount)
            {
                UIManager.Instance.ShowToast(Table, RuneRequiredKey, StartRuneCount);
                return;
            }
            UIManager.Instance.ShowConfirm(Table, StartConfirmKey, () => StartRun(playerName));
        }
        #endregion

        #region StartRun - 이름을 판 데이터에 넣고 고른 룬(카드 순서)을 코덱스 캡슐로 익힌 뒤 전초기지로 (첫 도착이라 역마차 연출부터)
        /// <param name="playerName">앞뒤 공백을 지운 이름</param>
        void StartRun(string playerName)
        {
            Game.Run.PlayerName = playerName;
            // 고른 룬 (카드 순서)
            var chosen = new List<RuneSO>();
            foreach (var (card, rune) in _cards)
                if (card.Toggle.isOn) chosen.Add(rune);
            new CodexService(Game.Run.Codex, DataManager.Instance.Spells, DataManager.Instance.Runes).SetLearned(chosen);
            Game.ChangeState(new HubState());
        }
        #endregion

        #region FilterNameChar - 이름에 TMP 서식 태그 글자(<, >)가 들어가지 않게 막음
        /// <param name="text">지금까지 입력된 이름</param>
        /// <param name="charIndex">넣을 위치</param>
        /// <param name="addedChar">넣으려는 글자</param>
        static char FilterNameChar(string text, int charIndex, char addedChar) =>
            addedChar == '<' || addedChar == '>' ? '\0' : addedChar;
        #endregion
    }
}
