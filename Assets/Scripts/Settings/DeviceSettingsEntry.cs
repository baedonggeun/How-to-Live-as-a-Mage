using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mage
{
    /// <summary>기기 설정 (이 PC에만 맞는 값: 음량, 창이 비활성이면 음소거, 배운 튜토리얼). settings_device.json 내용 그대로. 필드 이름이 JSON 키라 바꾸면 CurrentVersion을 올리고 읽을 때 옮겨야 함 (새 필드는 파일에 없으면 기본값이라 안 올려도 됨)</summary>
    [Serializable]
    public sealed partial class DeviceSettingsEntry
    {
        /// <summary>지금 파일 구조 버전. 필드 이름·뜻을 바꾸면 올림</summary>
        public const int CurrentVersion = 1;

        /// <summary>저장된 값이 없을 때 쓰는 음량 (믹서에 만든 그대로)</summary>
        public const float DefaultVolume = 1f;

        /// <summary>저장된 값이 없을 때 쓰는 '창이 비활성이면 음소거' 값</summary>
        public const bool DefaultMuteWhenUnfocused = true;

        /// <summary>이 값을 쓴 파일 구조 버전. 구조가 바뀔 때 옮기는 기준</summary>
        [SerializeField] int _version = CurrentVersion;

        /// <summary>전체 음량 (0~1)</summary>
        [SerializeField] float _masterVolume = DefaultVolume;

        /// <summary>배경음 음량 (0~1)</summary>
        [SerializeField] float _bgmVolume = DefaultVolume;

        /// <summary>효과음 음량 (0~1)</summary>
        [SerializeField] float _sfxVolume = DefaultVolume;

        /// <summary>창이 비활성이면 소리를 끌지. 게임은 어느 쪽이든 계속 돎</summary>
        [SerializeField] bool _muteWhenUnfocused = DefaultMuteWhenUnfocused;

        /// <summary>이 PC에서 배운 튜토리얼 id (TutorialSO.Id). 배운 조작 안내는 다시 안 띄움</summary>
        [SerializeField] List<string> _learnedTutorials = new();

        /// <summary>창이 비활성이면 소리를 끌지 (설정 창 토글)</summary>
        public bool MuteWhenUnfocused
        {
            get => _muteWhenUnfocused;
            set => _muteWhenUnfocused = value;
        }

        #region HasLearnedTutorial - 배운 튜토리얼인지 (대소문자 구분)
        /// <param name="id">튜토리얼 id</param>
        public bool HasLearnedTutorial(string id) => !string.IsNullOrEmpty(id) && _learnedTutorials.Contains(id);
        #endregion

        #region AddLearnedTutorial - 배운 튜토리얼 추가. 새로 넣었으면 true (빈 id·이미 있는 id는 false)
        /// <param name="id">튜토리얼 id</param>
        public bool AddLearnedTutorial(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || _learnedTutorials.Contains(id)) return false;
            _learnedTutorials.Add(id);
            return true;
        }
        #endregion

        #region GetVolume - 채널 음량 (0~1). 정의 안 된 채널이면 예외
        /// <param name="channel">읽을 채널</param>
        public float GetVolume(AudioChannel channel) => channel switch
        {
            AudioChannel.Master => _masterVolume,
            AudioChannel.Bgm => _bgmVolume,
            AudioChannel.Sfx => _sfxVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "정의 안 된 음량 채널"),
        };
        #endregion

        #region SetVolume - 채널 음량 바꿈 (범위 밖은 자르고 NaN은 기본값). 정의 안 된 채널이면 예외
        /// <param name="channel">바꿀 채널</param>
        /// <param name="volume">새 음량</param>
        public void SetVolume(AudioChannel channel, float volume)
        {
            // 쓸 수 있게 고친 음량
            float valid = ToValid(volume);
            switch (channel)
            {
                case AudioChannel.Master: _masterVolume = valid; break;
                case AudioChannel.Bgm: _bgmVolume = valid; break;
                case AudioChannel.Sfx: _sfxVolume = valid; break;
                default: throw new ArgumentOutOfRangeException(nameof(channel), channel, "정의 안 된 음량 채널");
            }
        }
        #endregion

        #region Validate - 파일에서 읽은 값을 쓸 수 있게 고침 (음량 0~1·NaN, 배운 튜토리얼 빈 id·겹침, 모르는 버전은 지금 버전으로). 고친 게 있으면 true
        public bool Validate()
        {
            // 고친 전체 음량
            float master = ToValid(_masterVolume);
            // 고친 배경음 음량
            float bgm = ToValid(_bgmVolume);
            // 고친 효과음 음량
            float sfx = ToValid(_sfxVolume);
            // 고친 배운 튜토리얼 목록 (빈 id·겹친 id 뺌)
            List<string> learned = (_learnedTutorials ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();
            // 하나라도 고쳤는지. NaN은 어떤 값과도 != 라서 고친 걸로 잡힘
            bool changed = _version != CurrentVersion
                || master != _masterVolume || bgm != _bgmVolume || sfx != _sfxVolume
                || _learnedTutorials == null || learned.Count != _learnedTutorials.Count;
            _version = CurrentVersion;
            _masterVolume = master;
            _bgmVolume = bgm;
            _sfxVolume = sfx;
            _learnedTutorials = learned;
            return changed;
        }
        #endregion

        #region ToValid - 음량을 0~1로 자름. NaN은 Clamp01이 그대로 돌려줘서 기본값으로
        /// <param name="volume">고칠 음량</param>
        static float ToValid(float volume) => float.IsNaN(volume) ? DefaultVolume : Mathf.Clamp01(volume);
        #endregion
    }
}
