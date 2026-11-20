using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>목표 하나 (임무 일지 탭에 보임). 대사·물건이 줌</summary>
    [CreateAssetMenu(fileName = "Objective", menuName = "Mage/Objective")]
    public sealed class ObjectiveSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (inn_room …)</summary>
        [SerializeField] string _id;

        /// <summary>목표 이름 (Objective 테이블 objective.{id}.title)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>목표 설명 (Objective 테이블 objective.{id}.desc)</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>목표 이름</summary>
        public LocalizedString Title => _title;

        /// <summary>목표 설명</summary>
        public LocalizedString Description => _description;
    }
}
