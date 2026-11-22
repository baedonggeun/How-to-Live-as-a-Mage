using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>영입 창 참조 (Boot 씬 UICanvas/Recruit 루트. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class RecruitView : MonoBehaviour
    {
        /// <summary>후보 줄을 넣을 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _candidateList;

        /// <summary>파티 줄을 넣을 자리 (VerticalLayoutGroup. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _partyList;

        /// <summary>용병 한 줄 프리팹 (인스펙터에서만 연결)</summary>
        [SerializeField] RecruitRowView _rowPrefab;

        /// <summary>닫기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _closeButton;

        /// <summary>후보 줄 자리</summary>
        public RectTransform CandidateList => _candidateList;

        /// <summary>파티 줄 자리</summary>
        public RectTransform PartyList => _partyList;

        /// <summary>용병 한 줄 프리팹</summary>
        public RecruitRowView RowPrefab => _rowPrefab;

        /// <summary>닫기 버튼</summary>
        public Button CloseButton => _closeButton;
    }
}
