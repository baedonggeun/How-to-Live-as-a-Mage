using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>로딩창 참조 (Boot CoverCanvas/Cover에 붙음)</summary>
    public sealed class LoadingView : MonoBehaviour
    {
        /// <summary>로딩창 전체. 투명도로 보이고 숨고, 보일 때만 클릭을 막음 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _group;

        /// <summary>로딩 바 채움 (Image Filled 가로. 인스펙터에서만 연결)</summary>
        [SerializeField] Image _progressFill;

        /// <summary>팁 묶음 (그림·제목·본문). 팁이 없으면 꺼짐 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _tipRoot;

        /// <summary>팁 묶음 투명도 (Tip의 Canvas Group). 팁 글자를 다 읽기 전엔 0 → 덮개가 이미 보일 때(첫 진입) 자리 글자가 안 비침 (인스펙터에서만 연결)</summary>
        [SerializeField] CanvasGroup _tipGroup;

        /// <summary>팁 그림. 팁에 그림이 없으면 꺼짐 (인스펙터에서만 연결)</summary>
        [SerializeField] Image _tipPicture;

        /// <summary>팁 제목 글자. 테이블·키는 TransitionManager가 넣음 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _tipTitle;

        /// <summary>팁 본문 글자. 테이블·키는 TransitionManager가 넣음 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _tipBody;

        /// <summary>'아무 키나 눌러 계속' 깜빡임. 로딩이 끝나면 켜짐 (인스펙터에서만 연결)</summary>
        [SerializeField] BlinkSequence _prompt;

        /// <summary>로딩이 끝났을 때 낼 소리. 비면 무음 (인스펙터에서만 연결)</summary>
        [SerializeField] SoundDataSO _readySound;

        /// <summary>로딩창 전체</summary>
        public CanvasGroup Group => _group;

        /// <summary>로딩 바 채움</summary>
        public Image ProgressFill => _progressFill;

        /// <summary>팁 묶음</summary>
        public GameObject TipRoot => _tipRoot;

        /// <summary>팁 묶음 투명도</summary>
        public CanvasGroup TipGroup => _tipGroup;

        /// <summary>팁 그림</summary>
        public Image TipPicture => _tipPicture;

        /// <summary>팁 제목 글자</summary>
        public LocalizeStringEvent TipTitle => _tipTitle;

        /// <summary>팁 본문 글자</summary>
        public LocalizeStringEvent TipBody => _tipBody;

        /// <summary>'아무 키나 눌러 계속' 깜빡임</summary>
        public BlinkSequence Prompt => _prompt;

        /// <summary>로딩이 끝났을 때 낼 소리. 비면 null</summary>
        public SoundDataSO ReadySound => _readySound;
    }
}
