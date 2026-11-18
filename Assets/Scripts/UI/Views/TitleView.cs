using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>초기 화면 참조</summary>
    public sealed class TitleView : MonoBehaviour
    {
        /// <summary>새 게임 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _newGameButton;

        /// <summary>이어하기 버튼 (저장이 없어서 상태가 끔. 인스펙터에서만 연결)</summary>
        [SerializeField] Button _continueButton;

        /// <summary>설정 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _settingsButton;

        /// <summary>종료 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _quitButton;

        /// <summary>타이틀 배경음. 비면 무음 (인스펙터에서만 연결)</summary>
        [SerializeField] SoundDataSO _bgm;

        /// <summary>새 게임 버튼</summary>
        public Button NewGameButton => _newGameButton;

        /// <summary>이어하기 버튼</summary>
        public Button ContinueButton => _continueButton;

        /// <summary>설정 버튼</summary>
        public Button SettingsButton => _settingsButton;

        /// <summary>종료 버튼</summary>
        public Button QuitButton => _quitButton;

        /// <summary>타이틀 배경음. 비면 null</summary>
        public SoundDataSO Bgm => _bgm;
    }
}
