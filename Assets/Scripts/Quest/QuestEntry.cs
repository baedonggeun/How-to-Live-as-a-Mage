using System;
using System.Collections.Generic;

namespace Mage
{
    /// <summary>의뢰 판 데이터 (RunEntry가 씬을 넘어 들고 다님). 의뢰 캡슐(숲: FieldQuestService, 전초기지: 게시판·정문)만 만짐</summary>
    public sealed class QuestEntry
    {
        /// <summary>지금 받은 의뢰. 받기 전·다녀온 뒤엔 null</summary>
        public QuestSO Current { get; set; }

        /// <summary>받은 의뢰의 목표를 숲에서 끝냈는지 (소탕 대상을 다 쓰러뜨림). 마을로 오면 의뢰를 끝냄</summary>
        public bool Cleared { get; set; }

        /// <summary>받은 의뢰가 끝나는 판 시각 (ClockEntry.PlaySeconds 기준). 0이면 아직 안 셈 (처음 출발할 때 정함)</summary>
        public float Deadline { get; set; }

        /// <summary>끝낸 의뢰 id (게시판이 '끝냄'으로 보임)</summary>
        public HashSet<string> CompletedIds { get; } = new(StringComparer.Ordinal);
    }
}
