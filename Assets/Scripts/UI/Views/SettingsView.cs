using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>설정 창 참조 (Boot 씬 UICanvas/Settings 루트에 붙음. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class SettingsView : MonoBehaviour
    {
        /// <summary>전체 음량 슬라이더 (인스펙터에서만 연결)</summary>
        [SerializeField] Slider _masterSlider;

        /// <summary>배경음 음량 슬라이더 (인스펙터에서만 연결)</summary>
        [SerializeField] Slider _bgmSlider;

        /// <summary>효과음 음량 슬라이더 (인스펙터에서만 연결)</summary>
        [SerializeField] Slider _sfxSlider;

        /// <summary>닫기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _closeButton;

        /// <summary>전체 음량 슬라이더</summary>
        public Slider MasterSlider => _masterSlider;

        /// <summary>배경음 음량 슬라이더</summary>
        public Slider BgmSlider => _bgmSlider;

        /// <summary>효과음 음량 슬라이더</summary>
        public Slider SfxSlider => _sfxSlider;

        /// <summary>닫기 버튼</summary>
        public Button CloseButton => _closeButton;
    }
}
