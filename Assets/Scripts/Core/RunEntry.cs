using System;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>이번 판 데이터. 씬을 넘어 GameManager가 들고 다님</summary>
    public sealed class RunEntry
    {
        /// <summary>임시 의뢰 이름이 든 String Table</summary>
        const string StubQuestTable = "UI";

        /// <summary>임시 의뢰 이름 키 (4단계에 QuestSO로 바뀜)</summary>
        const string StubQuestKey = "quest.forest_01.title";

        /// <summary>플레이어가 정한 마법사 이름. 플레이어 입력이라 번역 대상 아님</summary>
        public string PlayerName { get; set; } = "";

        /// <summary>파티에 든 용병들</summary>
        public List<MercenaryEntry> Party { get; } = new();

        /// <summary>지금 받은 의뢰 이름. 받기 전엔 null</summary>
        public LocalizedString QuestTitle { get; set; }

        /// <summary>캐릭터 생성에서 고른 요소 (습득 요소). 숲의 주문 판정이 씀</summary>
        public List<ElementSO> LearnedElements { get; } = new();

        /// <summary>전초기지 첫 도착 연출(역마차)을 봤는지. 새 판은 false, 디버그 판은 true (연출은 새 게임으로 확인)</summary>
        public bool HasArrived { get; set; }

        /// <summary>지금 목표 (임무 일지). 없으면 null. 판마다 새로</summary>
        public ObjectiveSO CurrentObjective { get; set; }

        #region CreateDebug - 에디터에서 중간 씬부터 Play할 때 쓰는 판 데이터 (받은 요소는 전부 습득)
        /// <param name="learned">습득할 요소 (빈 칸은 건너뜀)</param>
        public static RunEntry CreateDebug(IEnumerable<ElementSO> learned)
        {
            if (learned == null) throw new ArgumentNullException(nameof(learned));
            // 디버그 판 데이터. 빌드에선 늘 Title부터 시작해서 이 이름이 화면에 안 나옴
            var run = new RunEntry { PlayerName = "디버그 마법사", QuestTitle = StubQuest(), HasArrived = true };
            run.Party.AddRange(StubMercenaries());
            foreach (var element in learned)
                if (element != null) run.LearnedElements.Add(element);
            return run;
        }
        #endregion

        #region StubMercenaries - 임시 영입용 고정 용병 2명 (4단계에서 후보 추첨으로 바뀜)
        public static MercenaryEntry[] StubMercenaries() => new[] { new MercenaryEntry(3), new MercenaryEntry(2) };
        #endregion

        #region StubQuest - 임시 의뢰 이름 (4단계에서 게시판 의뢰로 바뀜)
        public static LocalizedString StubQuest() => new LocalizedString(StubQuestTable, StubQuestKey);
        #endregion
    }
}
