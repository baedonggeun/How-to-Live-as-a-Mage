using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>몬스터 하나 (현장 적). 수치는 v1 값 임시. 숲 적 자리(SpawnPointView)와 의뢰 소탕 대상(QuestSO)이 가리킴</summary>
    [CreateAssetMenu(fileName = "Monster", menuName = "Mage/Monster")]
    public sealed class MonsterSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (goblin, wolf, boar, spider, bear, mutant)</summary>
        [SerializeField] string _id;

        /// <summary>몬스터 이름 (Monster 테이블 monster.{id}.name)</summary>
        [SerializeField] LocalizedString _name;

        /// <summary>최대 체력</summary>
        [SerializeField, Min(1f)] float _maxHp = 35f;

        /// <summary>쫓을 때 속도 (m/s)</summary>
        [SerializeField, Min(0.1f)] float _speed = 5.2f;

        /// <summary>한 번 때릴 때 피해</summary>
        [SerializeField, Min(0f)] float _damage = 7f;

        /// <summary>때리고 다음에 때릴 때까지 (초)</summary>
        [SerializeField, Min(0.1f)] float _attackInterval = 1.2f;

        /// <summary>공격이 닿는 거리 (m, 두 몸 반지름 밖으로)</summary>
        [SerializeField, Min(0.1f)] float _attackRange = 1.4f;

        /// <summary>알아채는 거리 (m). 이 안에 파티가 오면 무리 전체가 깨어 쫓아옴</summary>
        [SerializeField, Min(0f)] float _aggroRange = 18f;

        /// <summary>동료를 두고 26m 안의 마법사를 노릴 확률 (0~1, 태어날 때 한 번 정함)</summary>
        [SerializeField, Range(0f, 1f)] float _mageFocusChance;

        /// <summary>몸 크기 배율 (회색 도형·몸 반지름에 곱함)</summary>
        [SerializeField, Min(0.1f)] float _size = 0.82f;

        /// <summary>회색 도형 색</summary>
        [SerializeField] Color _color = Color.gray;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>몬스터 이름</summary>
        public LocalizedString Name => _name;

        /// <summary>최대 체력</summary>
        public float MaxHp => _maxHp;

        /// <summary>쫓을 때 속도 (m/s)</summary>
        public float Speed => _speed;

        /// <summary>한 번 때릴 때 피해</summary>
        public float Damage => _damage;

        /// <summary>때리고 다음에 때릴 때까지 (초)</summary>
        public float AttackInterval => _attackInterval;

        /// <summary>공격이 닿는 거리 (m)</summary>
        public float AttackRange => _attackRange;

        /// <summary>알아채는 거리 (m)</summary>
        public float AggroRange => _aggroRange;

        /// <summary>마법사를 노릴 확률 (0~1)</summary>
        public float MageFocusChance => _mageFocusChance;

        /// <summary>몸 크기 배율</summary>
        public float Size => _size;

        /// <summary>회색 도형 색</summary>
        public Color Color => _color;
    }
}
