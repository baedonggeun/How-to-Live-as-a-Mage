using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>로딩창 팁 하나 (조작·기능 설명). 씬 전환 때 무작위로 하나 보임</summary>
    [CreateAssetMenu(fileName = "Tip", menuName = "Mage/Tip")]
    public sealed class TipSO : ScriptableObject
    {
        /// <summary>팁 제목 (Tip 테이블 tip.{이름}.title)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>팁 본문 (Tip 테이블 tip.{이름}.body)</summary>
        [SerializeField] LocalizedString _body;

        /// <summary>팁 그림 (조작 그림 등). 비면 안 보임</summary>
        [SerializeField] Sprite _picture;

        /// <summary>팁 제목</summary>
        public LocalizedString Title => _title;

        /// <summary>팁 본문</summary>
        public LocalizedString Body => _body;

        /// <summary>팁 그림. 비면 null</summary>
        public Sprite Picture => _picture;
    }
}
