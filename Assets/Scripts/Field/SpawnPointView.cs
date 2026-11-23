using UnityEngine;

namespace Mage
{
    /// <summary>숲에 고정된 적 무리 자리 표지 (씬 오브젝트, 로직 없음). 상태가 이 자리 둘레에 몬스터를 수만큼 놓음. 적 키는 {id}.{번호}</summary>
    public sealed class SpawnPointView : MonoBehaviour
    {
        /// <summary>자리 id (씬 안에서 겹치지 않게. 판 데이터 쓰러진 적 키에 씀. 인스펙터에서만 정함)</summary>
        [SerializeField] string _id;

        /// <summary>놓을 몬스터 (인스펙터에서만 연결)</summary>
        [SerializeField] MonsterSO _monster;

        /// <summary>놓을 수</summary>
        [SerializeField, Min(1)] int _count = 3;

        /// <summary>이 자리에서 흩어 놓는 반지름 (m, v1 무리 ±5)</summary>
        [SerializeField, Min(0f)] float _spread = 5f;

        /// <summary>자리 id</summary>
        public string Id => _id;

        /// <summary>놓을 몬스터</summary>
        public MonsterSO Monster => _monster;

        /// <summary>놓을 수</summary>
        public int Count => _count;

        /// <summary>흩어 놓는 반지름 (m)</summary>
        public float Spread => _spread;
    }
}
