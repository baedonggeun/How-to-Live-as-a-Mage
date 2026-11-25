using System;
using UnityEngine;

namespace Mage
{
    /// <summary>설정 창 음량 슬라이더 ↔ 채널 음량, '창이 비활성이면 음소거' 토글 ↔ 기기 설정 (기기 설정·음량 바꾸기는 UIManager가 Init으로 넘김). 창이 켜질 때 지금 값으로 맞추고 구독, 꺼질 때 해제</summary>
    public sealed class SettingsUIBinder : MonoBehaviour
    {
        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] SettingsView _view;

        /// <summary>기기 설정 (음량 읽기·창이 비활성이면 음소거. Init 전이면 null)</summary>
        DeviceSettingsEntry _device;

        /// <summary>채널 음량 바꾸기 (기기 설정에 넣고 믹서에 반영. Init 전이면 null)</summary>
        Action<AudioChannel, float> _setVolume;

        #region Init - 기기 설정·음량 바꾸기 함수를 받음 (UIManager가 Start에서)
        /// <param name="device">기기 설정</param>
        /// <param name="setVolume">채널 음량 바꾸기</param>
        public void Init(DeviceSettingsEntry device, Action<AudioChannel, float> setVolume)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _setVolume = setVolume ?? throw new ArgumentNullException(nameof(setVolume));
        }
        #endregion

        #region OnEnable - 토글·슬라이더를 지금 값으로 맞추고(이벤트 없이) 바뀜을 구독. Init 전(Boot를 읽는 중 켜진 채였던 창)이면 안 함 (창을 여는 건 늘 Init 뒤)
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Settings] SettingsView 연결 안 됨");
                return;
            }
            if (_device == null) return;
            _view.MuteWhenUnfocusedToggle.SetIsOnWithoutNotify(_device.MuteWhenUnfocused);
            _view.MuteWhenUnfocusedToggle.onValueChanged.AddListener(OnMuteWhenUnfocusedChanged);
            _view.MasterSlider.SetValueWithoutNotify(_device.GetVolume(AudioChannel.Master));
            _view.BgmSlider.SetValueWithoutNotify(_device.GetVolume(AudioChannel.Bgm));
            _view.SfxSlider.SetValueWithoutNotify(_device.GetVolume(AudioChannel.Sfx));
            _view.MasterSlider.onValueChanged.AddListener(OnMasterChanged);
            _view.BgmSlider.onValueChanged.AddListener(OnBgmChanged);
            _view.SfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }
        #endregion

        #region OnDisable - 토글·슬라이더 구독 해제 (파일 저장은 UIManager.HideSettings가 함)
        void OnDisable()
        {
            if (_view == null) return;
            _view.MuteWhenUnfocusedToggle.onValueChanged.RemoveListener(OnMuteWhenUnfocusedChanged);
            _view.MasterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            _view.BgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            _view.SfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }
        #endregion

        #region OnMasterChanged - 전체 음량 바꿈
        /// <param name="value">슬라이더 값 (0~1)</param>
        void OnMasterChanged(float value) => _setVolume(AudioChannel.Master, value);
        #endregion

        #region OnBgmChanged - 배경음 음량 바꿈
        /// <param name="value">슬라이더 값 (0~1)</param>
        void OnBgmChanged(float value) => _setVolume(AudioChannel.Bgm, value);
        #endregion

        #region OnSfxChanged - 효과음 음량 바꿈
        /// <param name="value">슬라이더 값 (0~1)</param>
        void OnSfxChanged(float value) => _setVolume(AudioChannel.Sfx, value);
        #endregion

        #region OnMuteWhenUnfocusedChanged - '창이 비활성이면 음소거'를 기기 설정에 넣음 (다음에 창이 비활성이 될 때부터 적용)
        /// <param name="isOn">토글이 켜졌는지</param>
        void OnMuteWhenUnfocusedChanged(bool isOn) => _device.MuteWhenUnfocused = isOn;
        #endregion
    }
}
