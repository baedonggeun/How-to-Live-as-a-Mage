using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>튜토리얼 탭 목록 항목 참조 (TutorialListItem 프리팹 루트)</summary>
    public sealed class TutorialListItemView : MonoBehaviour
    {
        /// <summary>항목 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _button;

        /// <summary>항목 제목 글자 (키는 UIBinder가 넣음. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _title;

        /// <summary>항목 버튼</summary>
        public Button Button => _button;

        /// <summary>항목 제목 글자</summary>
        public LocalizeStringEvent Title => _title;
    }
}
