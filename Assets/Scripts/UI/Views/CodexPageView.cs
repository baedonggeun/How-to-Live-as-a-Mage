using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>코덱스 한 장 참조 (룬 주문·다중 룬 마법 장, 넘어가는 종이). 내용은 CodexController가 넣음</summary>
    public sealed class CodexPageView : MonoBehaviour
    {
        /// <summary>묶음 이름 (룬 주문, N룬 마법. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _category;

        /// <summary>이름 (불 룬 주문, 화염구. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _title;

        /// <summary>그림 (칸에 맞춤. 인스펙터에서만 연결)</summary>
        [SerializeField] StrokeDrawer _drawing;

        /// <summary>필요한 룬 (다중 룬 마법만. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _runes;

        /// <summary>설명 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _body;

        /// <summary>쪽 번호 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _number;

        /// <summary>탭에 보일 장이 없을 때 글 (첫 장만. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _empty;

        /// <summary>묶음 이름</summary>
        public LocalizeStringEvent Category => _category;

        /// <summary>이름</summary>
        public LocalizeStringEvent Title => _title;

        /// <summary>그림</summary>
        public StrokeDrawer Drawing => _drawing;

        /// <summary>필요한 룬</summary>
        public LocalizeStringEvent Runes => _runes;

        /// <summary>설명</summary>
        public LocalizeStringEvent Body => _body;

        /// <summary>쪽 번호</summary>
        public LocalizeStringEvent Number => _number;

        /// <summary>탭에 보일 장이 없을 때 글</summary>
        public LocalizeStringEvent Empty => _empty;
    }
}
