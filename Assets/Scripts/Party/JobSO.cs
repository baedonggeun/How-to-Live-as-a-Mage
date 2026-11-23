using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>용병 직업 하나 (레인저·짐꾼·스피어 가드·듀얼리스트). 영입 후보가 이 중에서 무작위로 뽑힘</summary>
    [CreateAssetMenu(fileName = "Job", menuName = "Mage/Job")]
    public sealed class JobSO : ScriptableObject
    {
        /// <summary>전투 수치 최솟값</summary>
        public const int MinCombat = 1;

        /// <summary>전투 수치 최댓값</summary>
        public const int MaxCombat = 5;

        /// <summary>바뀌지 않는 id (ranger, porter, spear_guard, duelist)</summary>
        [SerializeField] string _id;

        /// <summary>직업 이름 (Job 테이블 job.{id}.name)</summary>
        [SerializeField] LocalizedString _name;

        /// <summary>무기와 역할 설명 (Job 테이블 job.{id}.desc). 영입 창 툴팁에 보임</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>전투 수치 (1~5, 임시). 숲 단계에서 동료 체력·공격에 씀</summary>
        [SerializeField, Range(MinCombat, MaxCombat)] int _combat = 3;

        /// <summary>현장에서 활로 떨어져서 쏘는지 (레인저). 아니면 붙어서 싸움</summary>
        [SerializeField] bool _ranged;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>직업 이름</summary>
        public LocalizedString Name => _name;

        /// <summary>무기와 역할 설명</summary>
        public LocalizedString Description => _description;

        /// <summary>전투 수치 (1~5)</summary>
        public int Combat => _combat;

        /// <summary>현장에서 활로 떨어져서 쏘는지</summary>
        public bool Ranged => _ranged;
    }
}
