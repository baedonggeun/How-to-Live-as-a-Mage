# Plan: 설정 저장 JSON 전환 (settings_device.json)

## 현행 분석

- 음량: AudioManager가 채널별 float을 PlayerPrefs(레지스트리) `Mage.Volume.Master`·`Mage.Volume.Bgm`·`Mage.Volume.Sfx`에 둠. InitSingleton(Awake)에서 읽고 Start에서 믹서에 넣음 (믹서 기본 스냅샷이 Awake의 SetFloat를 덮어써서). SetVolume마다 `PlayerPrefs.SetFloat`, 디스크 기록은 종료 때 Unity가 함
- 호출: SettingsUIBinder만. 설정 창 OnEnable에 `GetVolume` 3번(`SetValueWithoutNotify`), 슬라이더 `onValueChanged` → `SetVolume`
- 설정 창: Boot `UICanvas/Settings` (씬에서 꺼져 있음). 여는 곳은 TitleState 설정 버튼 → `UIManager.ShowSettings`. 닫기 버튼·Esc(`OnCancel`) 둘 다 `UIManager.HideSettings`
- 매니저: Boot `Managers` 아래 MonoSingleton 6개. 서로 Awake 순서 없음 → 다른 매니저는 Start부터 씀. GameManager.Start가 전부 있는지 확인
- 언어: 키트 `PlayerPrefLocaleSelector` → PlayerPrefs. 이번 범위 밖
- 파일 저장 코드 없음. `Application.persistentDataPath` = `C:\Users\<사용자>\AppData\LocalLow\DefaultCompany\How-to-Live-as-a-Mage` (에디터·빌드 같은 폴더)

---

## C# 변경

### `Assets/Scripts/Settings/DeviceSettingsEntry.cs` (신규)

**추가 내용:**
- 기기 설정 데이터. JSON 파일 내용과 1:1 (`[Serializable]`, `[SerializeField] private` 필드 이름 = JSON 키)
- 채널별 GetVolume / SetVolume, 파일에서 읽은 값을 고치는 Validate
- `Assets/Scripts/Settings/` 폴더 새로 생김 (.meta는 Unity가 만듦)

```csharp
using System;
using UnityEngine;

namespace Mage
{
    /// <summary>기기 설정 (이 PC에만 맞는 값, 지금은 음량). settings_device.json 내용 그대로. 필드 이름이 JSON 키라 바꾸면 CurrentVersion을 올리고 읽을 때 옮겨야 함</summary>
    [Serializable]
    public sealed class DeviceSettingsEntry
    {
        /// <summary>지금 파일 구조 버전. 필드 이름·뜻을 바꾸면 올림</summary>
        public const int CurrentVersion = 1;

        /// <summary>저장된 값이 없을 때 쓰는 음량 (믹서에 만든 그대로)</summary>
        public const float DefaultVolume = 1f;

        /// <summary>이 값을 쓴 파일 구조 버전. 구조가 바뀔 때 옮기는 기준</summary>
        [SerializeField] int _version = CurrentVersion;

        /// <summary>전체 음량 (0~1)</summary>
        [SerializeField] float _masterVolume = DefaultVolume;

        /// <summary>배경음 음량 (0~1)</summary>
        [SerializeField] float _bgmVolume = DefaultVolume;

        /// <summary>효과음 음량 (0~1)</summary>
        [SerializeField] float _sfxVolume = DefaultVolume;

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

        #region Validate - 파일에서 읽은 값을 쓸 수 있게 고침 (음량 0~1·NaN, 모르는 버전은 지금 버전으로). 고친 게 있으면 true
        public bool Validate()
        {
            // 고친 전체 음량
            float master = ToValid(_masterVolume);
            // 고친 배경음 음량
            float bgm = ToValid(_bgmVolume);
            // 고친 효과음 음량
            float sfx = ToValid(_sfxVolume);
            // 하나라도 고쳤는지. NaN은 어떤 값과도 != 라서 고친 걸로 잡힘
            bool changed = _version != CurrentVersion
                || master != _masterVolume || bgm != _bgmVolume || sfx != _sfxVolume;
            _version = CurrentVersion;
            _masterVolume = master;
            _bgmVolume = bgm;
            _sfxVolume = sfx;
            return changed;
        }
        #endregion

        #region ToValid - 음량을 0~1로 자름. NaN은 Clamp01이 그대로 돌려줘서 기본값으로
        /// <param name="volume">고칠 음량</param>
        static float ToValid(float volume) => float.IsNaN(volume) ? DefaultVolume : Mathf.Clamp01(volume);
        #endregion
    }
}
```

저장된 파일 모양 (`JsonUtility.ToJson(entry, true)`):

```json
{
    "_version": 1,
    "_masterVolume": 1.0,
    "_bgmVolume": 0.5,
    "_sfxVolume": 1.0
}
```

**호출/구독 관계:**
- SettingsManager가 만들고 들고 있음 (`SettingsManager.Device`). Validate는 SettingsManager.ReadDevice만 부름
- GetVolume / SetVolume은 AudioManager가 부름

---

### `Assets/Scripts/Managers/SettingsManager.cs` (신규)

**추가 내용:**
- MonoSingleton. InitSingleton(Awake)에 기기 설정 읽기: 본 파일 → `.bak` → 기본값. 깨진 본 파일은 `.corrupt`로 옮김
- Save: `.tmp`에 다 쓴 뒤 `File.Replace`로 본 파일과 바꿈 (전 본 파일은 `.bak`). 본 파일이 없으면 `File.Move`
- 종료 때(OnApplicationQuit)도 Save

```csharp
using System;
using System.IO;
using UnityEngine;

namespace Mage
{
    /// <summary>설정 파일 담당. 지금은 기기 설정(settings_device.json) 하나. 세이브 슬롯과 따로 persistentDataPath에 둠</summary>
    public sealed class SettingsManager : MonoSingleton<SettingsManager>
    {
        /// <summary>기기 설정 파일 이름 (persistentDataPath 바로 아래)</summary>
        const string DeviceFileName = "settings_device.json";

        /// <summary>임시 파일 (본 파일 이름 + 이것). 다 쓴 뒤 본 파일과 바꿈</summary>
        const string TempSuffix = ".tmp";

        /// <summary>바로 전 본 파일 (본 파일 이름 + 이것). 본 파일이 없거나 깨졌을 때 읽음</summary>
        const string BackupSuffix = ".bak";

        /// <summary>깨진 본 파일을 옮겨 둘 이름 (본 파일 이름 + 이것). 다음 저장이 깨진 내용으로 .bak을 덮지 않게</summary>
        const string CorruptSuffix = ".corrupt";

        /// <summary>기기 설정 파일 전체 경로</summary>
        string _devicePath;

        /// <summary>지금 기기 설정. InitSingleton 뒤엔 늘 있음</summary>
        DeviceSettingsEntry _device;

        /// <summary>지금 기기 설정 (음량). 바꾼 값은 Save를 불러야 파일에 남음</summary>
        public DeviceSettingsEntry Device => _device;

        #region InitSingleton - 기기 설정 읽기: 본 파일 → .bak → 기본값 (다른 매니저는 Start부터 Device를 씀)
        protected override void InitSingleton()
        {
            _devicePath = Path.Combine(Application.persistentDataPath, DeviceFileName);
            _device = ReadDevice(_devicePath, moveIfCorrupt: true);
            if (_device == null)
            {
                _device = ReadDevice(_devicePath + BackupSuffix, moveIfCorrupt: false);
                if (_device != null)
                    Debug.LogWarning($"[Settings] 본 파일 대신 백업에서 읽음: {_devicePath}{BackupSuffix}");
            }
            if (_device == null) _device = new DeviceSettingsEntry();
        }
        #endregion

        #region Save - 기기 설정을 파일에 씀. .tmp에 다 쓴 뒤 본 파일과 바꾸고 전 본 파일은 .bak (쓰다 꺼져도 본 파일이 안 깨지게). 실패하면 에러만 남기고 지금 값은 그대로 씀
        public void Save()
        {
            if (_device == null) return;
            // 다 쓴 뒤 본 파일과 바꿀 임시 파일
            string tmp = _devicePath + TempSuffix;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(tmp, JsonUtility.ToJson(_device, true));
                // File.Replace는 바꿀 본 파일이 있어야 함. 처음 저장·깨진 파일을 옮긴 뒤엔 이름만 바꿈
                if (File.Exists(_devicePath))
                    File.Replace(tmp, _devicePath, _devicePath + BackupSuffix);
                else
                    File.Move(tmp, _devicePath);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.LogError($"[Settings] 저장 실패: {_devicePath} ({e.Message}). 이번 실행 동안은 바뀐 값 그대로 씀");
            }
        }
        #endregion

        #region OnApplicationQuit - 종료 표시 후 저장 (설정 창을 연 채 끄는 경우. 에디터는 Play 정지)
        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            Save();
        }
        #endregion

        #region ReadDevice - 파일 하나에서 기기 설정 읽기. 없거나 못 읽거나 깨졌으면 null
        /// <param name="path">읽을 파일</param>
        /// <param name="moveIfCorrupt">깨졌으면 .corrupt로 옮길지 (본 파일만 true)</param>
        static DeviceSettingsEntry ReadDevice(string path, bool moveIfCorrupt)
        {
            if (!File.Exists(path)) return null;
            // 파일 내용
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.LogError($"[Settings] 못 읽음: {path} ({e.Message})");
                return null;
            }

            // 읽은 값. 파일에 없는 키는 기본값 그대로
            var entry = new DeviceSettingsEntry();
            // 깨진 이유. null이면 정상
            string error = null;
            if (string.IsNullOrWhiteSpace(json))
                error = "빈 파일";
            else
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, entry);
                }
                catch (ArgumentException e)
                {
                    error = e.Message;
                }
            }
            if (error != null)
            {
                Debug.LogWarning($"[Settings] 깨진 설정 파일: {path} ({error})");
                if (moveIfCorrupt) MoveAside(path);
                return null;
            }

            if (entry.Validate())
                Debug.LogWarning($"[Settings] 잘못된 값을 고쳐 읽음: {path}");
            return entry;
        }
        #endregion

        #region MoveAside - 깨진 파일을 .corrupt로 옮김 (전에 옮겨 둔 게 있으면 지우고)
        /// <param name="path">옮길 파일</param>
        static void MoveAside(string path)
        {
            // 옮겨 둘 이름
            string corrupt = path + CorruptSuffix;
            try
            {
                File.Delete(corrupt);
                File.Move(path, corrupt);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.LogError($"[Settings] 깨진 파일 못 옮김: {path} ({e.Message})");
            }
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `Device`: AudioManager.GetVolume / SetVolume (Start 이후)
- `Save`: UIManager.HideSettings (닫기 버튼·Esc), 자기 OnApplicationQuit
- GameManager.Start가 있는지 확인

---

### `Assets/Scripts/Managers/AudioManager.cs`

**변경 내용:**
- PlayerPrefs 키·읽기·쓰기, `DefaultVolume`(DeviceSettingsEntry로 옮김), 음량 배열 `_volumes` 삭제
- 음량은 `SettingsManager.Instance.Device`에서 읽고 씀. ApplyToMixer 인자를 채널로

1. 필드 3개 삭제

```csharp
        /// <summary>저장된 음량이 없을 때 쓰는 음량 (믹서에 만든 그대로)</summary>
        const float DefaultVolume = 1f;
```

```csharp
        /// <summary>채널별 음량 저장 키 (AudioChannel 순서). 기기 설정이라 세이브 슬롯과 따로 PlayerPrefs에</summary>
        static readonly string[] PrefsKeys = { "Mage.Volume.Master", "Mage.Volume.Bgm", "Mage.Volume.Sfx" };
```

```csharp
        /// <summary>채널별 지금 음량 (0~1, AudioChannel 순서)</summary>
        readonly float[] _volumes = new float[MixerParams.Length];
```

2. InitSingleton — region 제목 바꾸고 PlayerPrefs 읽기 두 줄(+ 뒤 빈 줄) 삭제. 나머지는 그대로

```csharp
        // 제목 before
        #region InitSingleton - 믹서 연결 확인, 저장된 음량 읽기, BGM 소스와 효과음 풀 만들기
        // 제목 after
        #region InitSingleton - 믹서 연결 확인, BGM 소스와 효과음 풀 만들기 (저장된 음량은 SettingsManager가 읽음)
```

```csharp
            // 삭제
            for (int i = 0; i < _volumes.Length; i++)
                _volumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKeys[i], DefaultVolume));
```

3. Start 전체 교체

```csharp
        #region Start - 저장된 음량을 믹서에 넣기 (믹서 기본 스냅샷이 Awake의 SetFloat를 덮어써서 Start에서. SettingsManager는 Awake에 파일을 읽어 둠)
        void Start()
        {
            if (SettingsManager.Instance == null) return;
            for (int i = 0; i < MixerParams.Length; i++)
                ApplyToMixer((AudioChannel)i);
        }
        #endregion
```

4. GetVolume 전체 교체

```csharp
        #region GetVolume - 채널 음량 (0~1). 값은 SettingsManager의 기기 설정이 들고 있음
        /// <param name="channel">읽을 채널</param>
        public float GetVolume(AudioChannel channel) => SettingsManager.Instance.Device.GetVolume(channel);
        #endregion
```

5. SetVolume 전체 교체

```csharp
        #region SetVolume - 채널 음량을 기기 설정에 넣고 믹서에 반영 (파일 저장은 설정 창을 닫을 때·종료 때 SettingsManager가 함)
        /// <param name="channel">바꿀 채널</param>
        /// <param name="volume">새 음량 (0~1, 범위 밖은 자름)</param>
        public void SetVolume(AudioChannel channel, float volume)
        {
            SettingsManager.Instance.Device.SetVolume(channel, volume);
            ApplyToMixer(channel);
        }
        #endregion
```

6. ApplyToMixer 전체 교체 — 인자를 채널로, 음량은 GetVolume에서

```csharp
        #region ApplyToMixer - 채널 음량을 dB로 바꿔 믹서에 넣음 (log10 × 20, 0이면 -80dB)
        /// <param name="channel">넣을 채널</param>
        void ApplyToMixer(AudioChannel channel)
        {
            if (_mixer == null) return;
            // 채널 배열 위치
            int index = ToIndex(channel);
            // 기기 설정에 든 0~1 음량
            float volume = GetVolume(channel);
            // 0~1 음량을 바꾼 dB. 0.0001(-80dB) 이하는 최저값
            float db = volume <= 0.0001f ? MinDb : Mathf.Log10(volume) * 20f;
            if (!_mixer.SetFloat(MixerParams[index], db))
                Debug.LogError($"[Audio] 믹서에 {MixerParams[index]} 파라미터 없음");
        }
        #endregion
```

ToIndex·재생 코드(PlayBgm·Play·PlayAt·풀)는 그대로.

**호출/구독 관계:**
- SettingsUIBinder → GetVolume / SetVolume (그대로)
- AudioManager → SettingsManager.Device (Start 이후만)

---

### `Assets/Scripts/Managers/UIManager.cs`

**변경 내용:** HideSettings에서 설정 저장. 전체 교체

```csharp
        #region HideSettings - 설정 창 끄고 바뀐 설정을 파일에 저장 (닫기 버튼·Esc 둘 다 여기로)
        public void HideSettings()
        {
            if (_settings != null) _settings.gameObject.SetActive(false);
            // 설정 파일 담당 (Boot 상주)
            var settingsManager = SettingsManager.Instance;
            if (settingsManager != null) settingsManager.Save();
        }
        #endregion
```

**호출/구독 관계:** 닫기 버튼 onClick(InitWindows), OnCancel(Esc) — 그대로

---

### `Assets/Scripts/Managers/GameManager.cs`

**변경 내용:** Start의 매니저 확인에 SettingsManager 추가

```csharp
            // before
            bool allReady = TransitionManager.Instance != null && UIManager.Instance != null
                && DataManager.Instance != null && AudioManager.Instance != null
                && TutorialManager.Instance != null;

            // after
            bool allReady = TransitionManager.Instance != null && UIManager.Instance != null
                && DataManager.Instance != null && SettingsManager.Instance != null
                && AudioManager.Instance != null && TutorialManager.Instance != null;
```

---

### `Assets/Scripts/UI/Binders/SettingsUIBinder.cs`

**변경 내용:** OnDisable region 제목만

```csharp
        // before
        #region OnDisable - 슬라이더 구독 해제 (음량 저장은 SetVolume이 이미 함)
        // after
        #region OnDisable - 슬라이더 구독 해제 (파일 저장은 UIManager.HideSettings가 함)
```

---

## Scene 변경

### `Assets/Scenes/Boot.unity`

1. Boot 씬 열기
2. Hierarchy `Managers` 우클릭 → Create Empty → 이름 `SettingsManager` (Transform 기본값)
3. Inspector → Add Component → `SettingsManager` (연결할 칸 없음)
4. 씬 저장 → 다시 열어 `Managers/SettingsManager`에 컴포넌트 붙어 있는지 확인

---

## 주의사항

- 순서: SettingsManager는 Awake(InitSingleton)에 읽고 AudioManager는 Start부터 Device를 씀. 같은 씬의 Awake는 전부 Start보다 먼저라 안전. AudioManager.InitSingleton에서 Device 쓰면 안 됨
- 설정 창(`UICanvas/Settings`)은 씬에서 꺼진 채 유지. 켜 두면 SettingsUIBinder.OnEnable(GetVolume)이 SettingsManager Awake보다 먼저 돌 수 있음 → NRE
- File.Replace: 바꿀 본 파일이 없으면 FileNotFoundException → 없을 땐 File.Move. `.bak`이 이미 있으면 덮음. `.tmp`·본 파일은 같은 폴더(같은 볼륨)
- 깨진 본 파일은 꼭 `.corrupt`로 옮김. 그대로 두면 다음 저장의 File.Replace가 깨진 내용을 `.bak`으로 밀어 넣어 좋은 백업을 잃음
- JsonUtility는 `[SerializeField]` private 필드만 씀 (const·프로퍼티 안 씀). JSON 키 = 필드 이름. 필드 이름·뜻을 바꾸면 CurrentVersion을 올리고 ReadDevice의 FromJsonOverwrite 뒤·Validate 앞에 옮기는 코드
- `Mathf.Clamp01(NaN)`은 NaN을 그대로 돌려줌 → IsNaN 먼저
- `_version`은 Validate에서 읽음. 아무 데서도 안 읽으면 CS0414 경고
- 저장 시점: HideSettings + 종료. 슬라이더 드래그 중엔 디스크 안 씀. 창을 연 채 크래시 나면 그동안 바꾼 값은 사라짐
- 에디터·빌드가 같은 파일을 씀. companyName(`DefaultCompany`)·productName 바꾸면 폴더 위치가 바뀜
- 기존 PlayerPrefs `Mage.Volume.*`은 안 읽음 → 개발 PC 음량 한 번 1로 돌아감. 레지스트리 키는 둬도 됨
- 언어는 그대로 PlayerPrefs (키트 `PlayerPrefLocaleSelector`)

---

## 진행 추적

- [x] 묶음 Logic — DeviceSettingsEntry·SettingsManager 추가, AudioManager·UIManager·GameManager·SettingsUIBinder 수정. 컴파일 에러·경고 0
- [x] 묶음 Scene — Boot `Managers/SettingsManager`. 저장 뒤 다시 열어 확인
- [x] 묶음 검증 (파일: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\How-to-Live-as-a-Mage\settings_device.json`)
  - [x] 파일 없이 Play → 음량 1·1·1, 콘솔 에러·경고 0
  - [x] 설정 창 BGM 0.5 → 닫기 → 파일 `_bgmVolume` 0.5, 믹서 BgmVol -6.02dB
  - [x] Play 다시 → BGM 슬라이더 0.5, BgmVol -6.02dB
  - [x] 두 번째 저장에 `.bak` 생김, 세 번째 저장(`.bak` 덮어쓰기)도 에러 없음
  - [x] Esc로 닫아도 저장됨
  - [x] 설정 창 연 채 Play 정지 → 파일에 바뀐 값
  - [x] 본 파일을 `{"_bgm`으로 깨뜨림 → Play → 경고 2개(깨짐·백업에서 읽음), `.corrupt` 생김, 값은 `.bak` 것
  - [x] `_masterVolume` 5 → Play → 경고 1개, 전체 슬라이더 1
  - [x] 파일 전부 지움 → Play → 기본값, 에러 0
- [x] 묶음 기획 — 검증 끝난 뒤 `d:\obsidian\Vault\1_Projects\How to live as a mage\docs\design\10-art-and-tech.md` 사운드 표 "음량 저장" 줄 교체 (레포 밖이라 커밋엔 안 들어감)

  ```markdown
  <!-- before -->
  | 음량 저장 | PlayerPrefs. 기기 설정이라 세이브 슬롯과 따로 둔다 |
  <!-- after -->
  | 음량 저장 | `settings_device.json` (persistentDataPath, `SettingsManager`). 기기 설정이라 세이브 슬롯과 따로 둔다 |
  ```

---

## 커밋 메시지

```
Update — 음량 설정 저장을 PlayerPrefs에서 JSON 파일로 (settings_device.json)

- DeviceSettingsEntry: 기기 설정(version, 전체·배경음·효과음 음량). 채널별 Get/Set, 읽은 값 검증(NaN → 기본값, 0~1로 자름, 모르는 버전 → 지금 버전)
- SettingsManager: Awake에 persistentDataPath/settings_device.json 읽기 (본 파일 → .bak → 기본값, 깨진 본 파일은 .corrupt로). Save는 .tmp에 쓴 뒤 File.Replace(전 본 파일 .bak), 본 파일 없으면 File.Move. 종료 때도 저장
- AudioManager: PlayerPrefs 키·읽기·쓰기와 음량 배열 뺌. 음량은 SettingsManager.Device에서 읽고 씀
- UIManager.HideSettings(닫기 버튼·Esc)에서 저장, GameManager 매니저 확인에 SettingsManager 추가
- Boot 씬 Managers에 SettingsManager
- 검증: 컴파일·콘솔 에러·경고 0. 파일 없이 시작 → 1·1·1. BGM 0.5 → 닫기 → 파일 0.5·BgmVol -6.02dB (드래그 중엔 안 씀), 다시 Play해도 유지. 2번째 저장에 .bak, 3번째(.bak 덮어쓰기) 에러 없음. Esc(OnCancel)·창 연 채 Play 정지도 저장. 본 파일 깨뜨림 → 경고 2개, .corrupt로 옮기고 .bak 값으로 시작, 종료 저장이 .bak 안 덮음. 전체 음량 5 → 경고 1개·1로 고침. 파일 다 지움 → 기본값·에러 0

Why: PlayerPrefs(레지스트리)는 디스크 기록이 종료 때라 크래시 나면 그 실행의 변경이 날아가고, 구조 버전이 없고, 파일이 아니라 Steam Cloud 대상이 못 됨. 그래픽·키 배치 옵션이 붙기 전에 설정 파일 자리를 먼저 만듦
Alternatives: PlayerPrefs 유지 (옵션 적을 땐 충분하지만 그래픽·클라우드 때 다시 옮겨야 함), Newtonsoft (값이 단순해서 JsonUtility로 충분), 슬라이더 움직일 때마다 저장 (드래그 중 디스크 쓰기 반복), 사용자 설정 파일까지 (아직 넣을 값 없음), OS 공통 쓰기 Copy·Delete·Move (Windows 전용이라 File.Replace)
```
