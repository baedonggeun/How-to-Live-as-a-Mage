using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>이번 판 데이터. 씬을 넘어 GameManager가 들고 다님</summary>
    public sealed class RunEntry
    {
        /// <summary>새 판 시작 금화 (임시)</summary>
        public const int StartGold = 100;

        /// <summary>플레이어가 정한 마법사 이름. 플레이어 입력이라 번역 대상 아님</summary>
        public string PlayerName { get; set; } = "";

        /// <summary>용병 파티·영입 후보 (파티 캡슐만 만짐)</summary>
        public PartyEntry Party { get; } = new();

        /// <summary>받은 의뢰·기한·끝낸 의뢰 (의뢰 캡슐만 만짐)</summary>
        public QuestEntry Quest { get; } = new();

        /// <summary>금화 (여러 기능이 쓰는 바깥 일이라 상태가 더하고 뺌)</summary>
        public int Gold { get; set; } = StartGold;

        /// <summary>여관 방 (InnService만 만짐)</summary>
        public InnEntry Inn { get; } = new();

        /// <summary>원정 식량·첫 출발 (ExpeditionService만 만짐)</summary>
        public ExpeditionEntry Expedition { get; } = new();

        /// <summary>익힌 룬·등록한 다중 룬 마법·생략 가능 수 (CodexService만 만짐)</summary>
        public CodexEntry Codex { get; } = new();

        /// <summary>전초기지 첫 도착 연출(역마차)을 봤는지. 새 판은 false, 디버그 판은 true (연출은 새 게임으로 확인)</summary>
        public bool HasArrived { get; set; }

        /// <summary>임무 일지 지금 목표 (JournalService만 만짐)</summary>
        public JournalEntry Journal { get; } = new();

        /// <summary>숲에서 쓰러뜨린 적 (ForestEnemyService만 만짐)</summary>
        public ForestEntry Forest { get; } = new();

        /// <summary>숲 안개 지도 (FogMapService만 만짐)</summary>
        public ForestFogEntry Fog { get; } = new();

        /// <summary>떨어진 금화 (FieldLootHandler만 만짐)</summary>
        public LootEntry Loot { get; } = new();

        /// <summary>판 시계 (FieldClockController만 흘림)</summary>
        public ClockEntry Clock { get; } = new();

        /// <summary>숲 리셋 (ForestResetService만 만짐)</summary>
        public ForestResetEntry ForestReset { get; } = new();

        /// <summary>고블린 던전 (DungeonService만 만짐)</summary>
        public DungeonEntry Dungeon { get; } = new();

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
                if (rune != null) run.Codex.LearnedRunes.Add(rune);
            foreach (SpellSO spell in spells)
                if (spell != null) run.Codex.CollectedSpells.Add(spell);
            return run;
        }
        #endregion
    }
}
