using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>일시정지 메뉴 참조 (Boot 씬 UICanvas/Pause 루트에 붙음. 켜고 끄는 단위는 이 오브젝트)</summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        /// <summary>계속하기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _resumeButton;

        /// <summary>설정 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _settingsButton;

        /// <summary>타이틀로 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _toTitleButton;

        /// <summary>계속하기 버튼</summary>
        public Button ResumeButton => _resumeButton;

        /// <summary>설정 버튼</summary>
        public Button SettingsButton => _settingsButton;

        /// <summary>타이틀로 버튼</summary>
        public Button ToTitleButton => _toTitleButton;
    }
}
