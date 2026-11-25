using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mage
{
    /// <summary>캐릭터 생성 확정을 막는 이유</summary>
    public enum CharacterCreationBlock
    {
        /// <summary>막는 것 없음 (확정할 수 있음)</summary>
        None,

        /// <summary>이름이 비었음</summary>
        NameEmpty,

        /// <summary>고른 룬이 모자람</summary>
        RunesMissing,
    }

    /// <summary>캐릭터 생성 캡슐 (MonoBehaviour 아님. CharacterCreationState가 만들고 이음): 생성 화면 참조·룬 목록을 받아 이름 칸 규칙(12자, 서식 태그 글자 막기)을 걸고, 룬마다 카드를 만들어(이름·그림·툴팁) 고른 수를 보이고 다 고르면 안 고른 카드를 잠금. 확정 검사·이름·고른 룬을 알려 줌. 토스트·확인창·판 데이터는 상태가 함</summary>
    public sealed class CharacterCreationController
    {
        /// <summary>이름 최대 글자 수</summary>
        public const int NameMaxLength = 12;

        /// <summary>캐릭터 생성에서 고를 룬 수 (임시. 기획 02·12 미정)</summary>
        public const int StartRuneCount = 4;

        /// <summary>생성 화면 참조</summary>
        readonly CharacterCreationView _view;

        /// <summary>만든 카드와 그 룬 (룬 목록 순서)</summary>
        readonly List<(RuneCardView card, RuneSO rune)> _cards = new();

        /// <summary>앞뒤 공백을 지운 이름</summary>
        public string PlayerName => _view.NameInput.text.Trim();

        #region CharacterCreationController - 생성 화면 참조·룬 목록을 받아 이름 칸 규칙을 걸고 룬 카드를 만든 뒤 고른 수를 보임
        /// <param name="view">생성 화면 참조</param>
        /// <param name="runes">룬 전부 (DataManager 순서, 빈 칸은 건너뜀)</param>
        public CharacterCreationController(CharacterCreationView view, IReadOnlyList<RuneSO> runes)
        {
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            if (runes == null) throw new ArgumentNullException(nameof(runes));
            _view.NameInput.characterLimit = NameMaxLength;
            _view.NameInput.onValidateInput = FilterNameChar;
            CreateCards(runes);
            RefreshSelection();
        }
        #endregion

        #region Check - 확정을 막는 이유: 이름이 비었으면 NameEmpty, 고른 룬 수가 정해진 수가 아니면 RunesMissing, 아니면 None
        public CharacterCreationBlock Check()
        {
            if (PlayerName.Length == 0) return CharacterCreationBlock.NameEmpty;
            return CountSelected() != StartRuneCount ? CharacterCreationBlock.RunesMissing : CharacterCreationBlock.None;
        }
        #endregion

        #region ChosenRunes - 고른 룬 (카드 순서, 복사본)
        public RuneSO[] ChosenRunes()
        {
            // 고른 룬 (카드 순서)
            var chosen = new List<RuneSO>();
            foreach (var (card, rune) in _cards)
                if (card.Toggle.isOn) chosen.Add(rune);
            return chosen.ToArray();
        }
        #endregion

        #region CreateCards - 룬마다 카드를 만들어 이름·그림을 넣고 이름·설명은 툴팁으로, 켜고 끌 때 고른 수를 다시 셈
        /// <param name="runes">룬 전부 (빈 칸은 건너뜀)</param>
        void CreateCards(IReadOnlyList<RuneSO> runes)
        {
            if (_view.RuneCardPrefab == null || _view.RuneGrid == null)
            {
                Debug.LogError("[Creation] 카드 프리팹·카드 자리 연결 안 됨");
                return;
            }
            foreach (var rune in runes)
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

        #region FilterNameChar - 이름에 TMP 서식 태그 글자(<, >)가 들어가지 않게 막음
        /// <param name="text">지금까지 입력된 이름</param>
        /// <param name="charIndex">넣을 위치</param>
        /// <param name="addedChar">넣으려는 글자</param>
        static char FilterNameChar(string text, int charIndex, char addedChar) =>
            addedChar == '<' || addedChar == '>' ? '\0' : addedChar;
        #endregion
    }
}
