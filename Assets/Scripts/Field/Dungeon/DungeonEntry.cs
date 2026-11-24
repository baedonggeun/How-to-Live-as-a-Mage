using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>고블린 던전 판 데이터 (RunEntry가 씬을 넘어 들고 다님). DungeonService만 만짐</summary>
    public sealed class DungeonEntry
    {
        /// <summary>던전 세대 (새로 만들 때마다 오름. 던전 적 키에 들어감)</summary>
        public int Generation { get; set; }

        /// <summary>이번 던전 보스 몬스터 id (던전을 만들 때 고름. 아직 없으면 null)</summary>
        public string BossId { get; set; }

        /// <summary>던전에서 쓰러뜨린 적: 키 → 쓰러진 곳 (던전을 새로 만들면 비움)</summary>
        public Dictionary<string, Vector3> Dead { get; } = new(StringComparer.Ordinal);

        /// <summary>이번 던전 보스를 쓰러뜨렸는지 (클리어)</summary>
        public bool Cleared { get; set; }

        /// <summary>던전을 새로 만들어야 하는지 (클리어하고 숲을 나가면 켬. 며칠 뒤 조건은 보류)</summary>
        public bool NeedsReset { get; set; }

        /// <summary>고블린이 던전에 보관한 금화</summary>
        public int StashGold { get; set; }
    }
}
