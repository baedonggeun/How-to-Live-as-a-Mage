using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>주문 요소 하나 (주문의 재료). 캐릭터 생성에서 골라 습득함</summary>
    [CreateAssetMenu(fileName = "Element", menuName = "Mage/Element")]
    public sealed class ElementSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (프로토타입 spell-core.js와 같음: fire, condense, line, block, water, spread, wall)</summary>
        [SerializeField] string _id;

        /// <summary>요소 이름 (Element 테이블 element.{id}.name)</summary>
        [SerializeField] LocalizedString _name;

        /// <summary>요소 설명 (Element 테이블 element.{id}.desc)</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>카드에 보일 그림 (요소 그림 모양). 비면 안 보임</summary>
        [SerializeField] Sprite _icon;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>요소 이름</summary>
        public LocalizedString Name => _name;

        /// <summary>요소 설명</summary>
        public LocalizedString Description => _description;

        /// <summary>카드에 보일 그림. 비면 null</summary>
        public Sprite Icon => _icon;
    }
}
