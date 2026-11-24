using UnityEngine;

namespace Mage
{
    /// <summary>떨어진 금화 판 데이터 (RunEntry가 씬을 넘어 들고 다님). FieldLootHandler만 만짐</summary>
    public sealed class LootEntry
    {
        /// <summary>떨어진 금화. 없으면 0 (또 떨어지면 앞의 것은 사라지고 새로)</summary>
        public int Gold { get; set; }

        /// <summary>떨어진 금화 자리</summary>
        public Vector3 Position { get; set; }
    }
}
