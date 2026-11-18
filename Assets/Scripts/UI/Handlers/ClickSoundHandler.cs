using UnityEngine;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>같은 오브젝트의 버튼이 눌리면 클릭음을 냄 (Button 프리팹에 붙음. 꺼진 버튼은 onClick이 안 와서 소리도 안 남)</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ClickSoundHandler : MonoBehaviour
    {
        /// <summary>낼 클릭음. 비면 무음 (소리 파일 넣기 전)</summary>
        [SerializeField] SoundDataSO _sound;

        #region Awake - 같은 오브젝트의 버튼에 클릭음 연결
        void Awake() => GetComponent<Button>().onClick.AddListener(OnClick);
        #endregion

        #region OnClick - 클릭음 재생. 소리가 비었으면 아무것도 안 함
        void OnClick()
        {
            if (_sound == null) return;
            // 소리를 낼 곳 (Boot 상주)
            var audio = AudioManager.Instance;
            if (audio != null) audio.Play(_sound);
        }
        #endregion
    }
}
