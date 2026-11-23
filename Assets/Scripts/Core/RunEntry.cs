using System;
using System.Collections.Generic;

namespace Mage
{
    /// <summary>이번 판 데이터. 씬을 넘어 GameManager가 들고 다님</summary>
    public sealed class RunEntry
    {
        /// <summary>새 판 시작 금화 (임시)</summary>
        public const int StartGold = 100;

        /// <summary>플레이어가 정한 마법사 이름. 플레이어 입력이라 번역 대상 아님</summary>
        public string PlayerName { get; set; } = "";

        /// <summary>파티에 든 용병들</summary>
        public List<MercenaryEntry> Party { get; } = new();

        /// <summary>길드 영입 후보 (처음 영입 창을 열 때 뽑음. 주마다 새로 뽑는 건 나중)</summary>
        public List<MercenaryEntry> RecruitCandidates { get; } = new();

        /// <summary>영입 후보를 뽑았는지</summary>
        public bool CandidatesDrawn { get; set; }

        /// <summary>지금 받은 의뢰. 받기 전·다녀온 뒤엔 null</summary>
        public QuestSO Quest { get; set; }

        /// <summary>금화</summary>
        public int Gold { get; set; } = StartGold;

        /// <summary>원정 식량 (인분. 한 사람 하루치가 1인분)</summary>
        public int Rations { get; set; }

        /// <summary>여관 방을 잡았는지 (방값은 처음 한 번)</summary>
        public bool HasRoom { get; set; }

        /// <summary>정문으로 한 번이라도 나갔는지. 그 전(첫 의뢰)엔 경비병이 파티·의뢰·식량을 봄</summary>
        public bool HasDeparted { get; set; }

        /// <summary>숲에서 막 돌아오는 중인지 (전초기지가 정문 밖에서 시작하고 한 번 쓰면 끔)</summary>
        public bool ReturningFromField { get; set; }

        /// <summary>익힌 룬 (캐릭터 생성에서 고름). 주문 판정·코덱스 룬 주문 장이 씀</summary>
        public List<RuneSO> LearnedRunes { get; } = new();

        /// <summary>전초기지 첫 도착 연출(역마차)을 봤는지. 새 판은 false, 디버그 판은 true (연출은 새 게임으로 확인)</summary>
        public bool HasArrived { get; set; }

        /// <summary>지금 목표 (임무 일지). 없으면 null. 판마다 새로</summary>
        public ObjectiveSO CurrentObjective { get; set; }

        /// <summary>코덱스에 등록한 다중 룬 마법 (생략 없이 처음 완성. 등록 순서). 생략은 등록한 마법에만</summary>
        public List<SpellSO> CollectedSpells { get; } = new();

        /// <summary>생략 가능 수 (레벨이 생기면 레벨이 정함. 지금은 프로토타입 기본 0)</summary>
        public int OmitLimit { get; set; }

        #region CreateDebug - 에디터에서 중간 씬부터 Play할 때 쓰는 판 데이터 (도착한 판). 룬을 다 익히고 다중 룬 마법을 다 등록함 (코덱스 탭·묶음을 바로 확인)
        /// <param name="runes">룬 목록 (DataManager 순서, 빈 칸은 건너뜀)</param>
        /// <param name="spells">다중 룬 마법 목록 (빈 칸은 건너뜀)</param>
        public static RunEntry CreateDebug(IReadOnlyList<RuneSO> runes, IReadOnlyList<SpellSO> spells)
        {
            if (runes == null) throw new ArgumentNullException(nameof(runes));
            if (spells == null) throw new ArgumentNullException(nameof(spells));
            // 디버그 판 데이터. 빌드에선 늘 Title부터 시작해서 이 이름이 화면에 안 나옴
            var run = new RunEntry { PlayerName = "디버그 마법사", HasArrived = true };
            foreach (RuneSO rune in runes)
                if (rune != null) run.LearnedRunes.Add(rune);
            foreach (SpellSO spell in spells)
                if (spell != null) run.CollectedSpells.Add(spell);
            return run;
        }
        #endregion
    }
}
