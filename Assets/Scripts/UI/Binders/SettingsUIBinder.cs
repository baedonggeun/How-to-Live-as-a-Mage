using UnityEngine;

namespace Mage
{
    /// <summary>설정 창 음량 슬라이더 ↔ AudioManager 채널 음량. 창이 켜질 때 지금 음량으로 맞추고 구독, 꺼질 때 해제</summary>
    public sealed class SettingsUIBinder : MonoBehaviour
    {
        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] SettingsView _view;

        #region OnEnable - 슬라이더를 지금 음량으로 맞추고(이벤트 없이) 바뀜을 구독
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Settings] SettingsView 연결 안 됨");
                return;
            }
            // 음량을 읽을 곳 (Boot 상주)
            var audio = AudioManager.Instance;
            if (audio == null) return;
            _view.MasterSlider.SetValueWithoutNotify(audio.GetVolume(AudioChannel.Master));
            _view.BgmSlider.SetValueWithoutNotify(audio.GetVolume(AudioChannel.Bgm));
            _view.SfxSlider.SetValueWithoutNotify(audio.GetVolume(AudioChannel.Sfx));
            _view.MasterSlider.onValueChanged.AddListener(OnMasterChanged);
            _view.BgmSlider.onValueChanged.AddListener(OnBgmChanged);
            _view.SfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }
        #endregion

        #region OnDisable - 슬라이더 구독 해제 (파일 저장은 UIManager.HideSettings가 함)
        void OnDisable()
        {
            if (_view == null) return;
            _view.MasterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            _view.BgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            _view.SfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }
        #endregion

        #region OnMasterChanged - 전체 음량 바꿈
        /// <param name="value">슬라이더 값 (0~1)</param>
        static void OnMasterChanged(float value) => AudioManager.Instance.SetVolume(AudioChannel.Master, value);
        #endregion

        #region OnBgmChanged - 배경음 음량 바꿈
        /// <param name="value">슬라이더 값 (0~1)</param>
        static void OnBgmChanged(float value) => AudioManager.Instance.SetVolume(AudioChannel.Bgm, value);
        #endregion

        #region OnSfxChanged - 효과음 음량 바꿈
        /// <param name="value">슬라이더 값 (0~1)</param>
        static void OnSfxChanged(float value) => AudioManager.Instance.SetVolume(AudioChannel.Sfx, value);
        #endregion
    }
}
