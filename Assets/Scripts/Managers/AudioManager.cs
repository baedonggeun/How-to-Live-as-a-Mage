using UnityEngine;
using UnityEngine.Audio;

namespace Mage
{
    /// <summary>믹서 음량 채널. 값은 AudioManager 채널 배열의 위치</summary>
    public enum AudioChannel
    {
        /// <summary>전체 음량</summary>
        Master = 0,
        /// <summary>배경음</summary>
        Bgm = 1,
        /// <summary>효과음</summary>
        Sfx = 2,
    }

    /// <summary>소리 재생 방법 담당 (BGM·효과음, AudioSource 풀, 믹서, 음량). 어떤 소리를 낼지는 콘텐츠 SO가 정함</summary>
    public sealed class AudioManager : MonoSingleton<AudioManager>
    {
        /// <summary>음량 0일 때 넣을 dB (믹서 최저값)</summary>
        const float MinDb = -80f;

        /// <summary>저장된 음량이 없을 때 쓰는 음량 (믹서에 만든 그대로)</summary>
        const float DefaultVolume = 1f;

        /// <summary>효과음 동시 재생 상한. 다 차면 가장 오래전에 시작한 소리를 끊고 뺏음</summary>
        const int SfxPoolSize = 16;

        /// <summary>채널별 믹서 노출 파라미터 이름 (AudioChannel 순서)</summary>
        static readonly string[] MixerParams = { "MasterVol", "BgmVol", "SfxVol" };

        /// <summary>채널별 음량 저장 키 (AudioChannel 순서). 기기 설정이라 세이브 슬롯과 따로 PlayerPrefs에</summary>
        static readonly string[] PrefsKeys = { "Mage.Volume.Master", "Mage.Volume.Bgm", "Mage.Volume.Sfx" };

        /// <summary>Master·BGM·SFX 그룹과 음량 파라미터가 있는 믹서 (Assets/Audio/MainMixer)</summary>
        [SerializeField] AudioMixer _mixer;

        /// <summary>BGM 소스가 내보낼 믹서 그룹</summary>
        [SerializeField] AudioMixerGroup _bgmGroup;

        /// <summary>효과음 소스가 내보낼 믹서 그룹</summary>
        [SerializeField] AudioMixerGroup _sfxGroup;

        /// <summary>채널별 지금 음량 (0~1, AudioChannel 순서)</summary>
        readonly float[] _volumes = new float[MixerParams.Length];

        /// <summary>BGM 소스 (이 오브젝트에 붙음, 2D)</summary>
        AudioSource _bgm;

        /// <summary>효과음 소스. 위치를 따로 옮기려고 소스마다 자식 오브젝트 하나</summary>
        AudioSource[] _sfxSources;

        /// <summary>효과음 소스별 마지막 재생 순번 (_sfxSources 순서). 작을수록 오래전</summary>
        int[] _sfxPlayOrder;

        /// <summary>지금까지 낸 효과음 수. 다음 재생 순번으로 씀</summary>
        int _sfxPlayCount;

        #region InitSingleton - 믹서 연결 확인, 저장된 음량 읽기, BGM 소스와 효과음 풀 만들기
        protected override void InitSingleton()
        {
            if (_mixer == null || _bgmGroup == null || _sfxGroup == null)
                Debug.LogError("[Audio] 믹서·그룹 연결 안 됨. 음량 설정이 소리에 안 먹음");

            for (int i = 0; i < _volumes.Length; i++)
                _volumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKeys[i], DefaultVolume));

            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.playOnAwake = false;
            _bgm.outputAudioMixerGroup = _bgmGroup;

            _sfxSources = new AudioSource[SfxPoolSize];
            _sfxPlayOrder = new int[SfxPoolSize];
            for (int i = 0; i < SfxPoolSize; i++)
            {
                // 소스 하나를 담는 자식 오브젝트
                var holder = new GameObject("SfxSource");
                holder.transform.SetParent(transform, false);
                // 이 자식의 효과음 소스
                var source = holder.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = _sfxGroup;
                // 뺏어 쓸 때 위치가 순간이동해 도플러로 피치가 튀지 않게 (풀 소스는 재생 중 안 움직임)
                source.dopplerLevel = 0f;
                _sfxSources[i] = source;
            }
        }
        #endregion

        #region Start - 저장된 음량을 믹서에 넣기 (믹서 기본 스냅샷이 Awake의 SetFloat를 덮어써서 Start에서)
        void Start()
        {
            for (int i = 0; i < _volumes.Length; i++)
                ApplyToMixer(i);
        }
        #endregion

        #region GetVolume - 채널 음량 (0~1)
        /// <param name="channel">읽을 채널</param>
        public float GetVolume(AudioChannel channel) => _volumes[ToIndex(channel)];
        #endregion

        #region SetVolume - 채널 음량을 바꿔 믹서에 넣고 PlayerPrefs에 기록 (디스크 저장은 종료 때 Unity가 함)
        /// <param name="channel">바꿀 채널</param>
        /// <param name="volume">새 음량 (0~1, 범위 밖은 자름)</param>
        public void SetVolume(AudioChannel channel, float volume)
        {
            // 채널 배열 위치
            int index = ToIndex(channel);
            _volumes[index] = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefsKeys[index], _volumes[index]);
            ApplyToMixer(index);
        }
        #endregion

        #region PlayBgm - BGM 바꾸기. 같은 곡이 이미 나오고 있으면 처음부터 다시 틀지 않음
        /// <param name="data">틀 곡. null이면 무시 (멈추려면 StopBgm)</param>
        /// <param name="loop">반복 여부. 보스 승리·게임 오버 같은 짧은 곡은 false</param>
        public void PlayBgm(SoundDataSO data, bool loop = true)
        {
            if (!HasClip(data)) return;
            _bgm.loop = loop;
            _bgm.volume = data.Volume;
            if (_bgm.isPlaying && _bgm.clip == data.Clip) return;
            _bgm.clip = data.Clip;
            _bgm.Play();
        }
        #endregion

        #region StopBgm - BGM 멈춤
        public void StopBgm() => _bgm.Stop();
        #endregion

        #region Play - 효과음을 2D로 (UI, 주문 그리기처럼 화면·마법사 자신의 소리)
        /// <param name="data">낼 소리. null이면 무시 (소리 안 넣은 콘텐츠)</param>
        public void Play(SoundDataSO data) => PlaySfx(data, 0f, transform.position);
        #endregion

        #region PlayAt - 효과음을 그 위치에서 3D로 (몬스터, 착탄, 동료처럼 현장 소리)
        /// <param name="data">낼 소리. null이면 무시 (소리 안 넣은 콘텐츠)</param>
        /// <param name="position">소리가 날 월드 위치</param>
        public void PlayAt(SoundDataSO data, Vector3 position) => PlaySfx(data, 1f, position);
        #endregion

        #region PlaySfx - 풀 소스 하나에 소리 하나를 넣고 재생. PlayOneShot은 뺏어 쓸 때 이전 소리가 남고 피치·위치가 같이 바뀌어서 clip + Play()
        /// <param name="data">낼 소리</param>
        /// <param name="spatialBlend">0이면 2D, 1이면 3D</param>
        /// <param name="position">소스를 옮길 월드 위치 (2D면 의미 없음)</param>
        void PlaySfx(SoundDataSO data, float spatialBlend, Vector3 position)
        {
            if (!HasClip(data)) return;
            // 이번 소리를 낼 소스
            AudioSource source = AcquireSfxSource();
            source.transform.position = position;
            source.spatialBlend = spatialBlend;
            source.clip = data.Clip;
            source.volume = data.Volume;
            source.pitch = Random.Range(data.PitchRange.x, data.PitchRange.y);
            source.Play();
        }
        #endregion

        #region AcquireSfxSource - 빈 소스 중 가장 오래전에 쓴 것. 빈 게 없으면 가장 오래전에 시작한 소스를 뺏음
        AudioSource AcquireSfxSource()
        {
            // 빈 소스 중 가장 오래전에 쓴 것 (-1이면 빈 소스 없음). 같은 프레임에 연달아 불러도 방금 준 소스를 다시 안 고르게
            int free = -1;
            // 전체 중 가장 오래전에 시작한 것 (다 찼을 때 뺏을 소스)
            int oldest = 0;
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                if (_sfxPlayOrder[i] < _sfxPlayOrder[oldest]) oldest = i;
                if (!_sfxSources[i].isPlaying && (free < 0 || _sfxPlayOrder[i] < _sfxPlayOrder[free])) free = i;
            }
            // 고른 소스 위치
            int pick = free >= 0 ? free : oldest;
            _sfxPlayOrder[pick] = ++_sfxPlayCount;
            return _sfxSources[pick];
        }
        #endregion

        #region HasClip - 낼 소리가 있는지. SO가 비면 조용히 false (소리 안 넣은 콘텐츠), SO에 클립만 없으면 경고
        /// <param name="data">확인할 소리 데이터</param>
        static bool HasClip(SoundDataSO data)
        {
            if (data == null) return false;
            if (data.Clip == null)
            {
                Debug.LogWarning($"[Audio] {data.name}에 클립 없음");
                return false;
            }
            return true;
        }
        #endregion

        #region ApplyToMixer - 채널 음량을 dB로 바꿔 믹서에 넣음 (log10 × 20, 0이면 -80dB)
        /// <param name="index">채널 배열 위치</param>
        void ApplyToMixer(int index)
        {
            if (_mixer == null) return;
            // 0~1 음량을 바꾼 dB. 0.0001(-80dB) 이하는 최저값
            float db = _volumes[index] <= 0.0001f ? MinDb : Mathf.Log10(_volumes[index]) * 20f;
            if (!_mixer.SetFloat(MixerParams[index], db))
                Debug.LogError($"[Audio] 믹서에 {MixerParams[index]} 파라미터 없음");
        }
        #endregion

        #region ToIndex - 채널을 배열 위치로. 정의 안 된 값이면 예외
        /// <param name="channel">바꿀 채널</param>
        static int ToIndex(AudioChannel channel)
        {
            // 채널 배열 위치
            int index = (int)channel;
            if (index < 0 || index >= MixerParams.Length)
                throw new System.ArgumentOutOfRangeException(nameof(channel), channel, "정의 안 된 음량 채널");
            return index;
        }
        #endregion
    }
}
