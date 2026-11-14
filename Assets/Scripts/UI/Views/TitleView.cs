using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>초기 화면 참조</summary>
    public sealed class TitleView : MonoBehaviour
    {
        /// <summary>새 게임 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _newGameButton;

        /// <summary>새 게임 버튼</summary>
        public Button NewGameButton => _newGameButton;
    }
}
