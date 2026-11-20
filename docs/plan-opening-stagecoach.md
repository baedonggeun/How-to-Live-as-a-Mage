# Plan: 전초기지 첫 도착 역마차 연출 (3-2)

## 현행 분석

- 흐름(구현 전): CharacterCreation → `Opening` 씬(UI/`opening.body` 본문 + 계속 버튼, `OpeningState`·`OpeningView`) → Outpost(`HubState`). Opening은 빌드 목록과 `GameManager.CreateStartState` 분기에 있었음
- `HubState`(3-1): `OnSceneReady`에서 `PlaceAt(Spawn)`·파티 수, `Reveal`에서 커서 잠금·조작 켬, `Exit`에서 해제
- 패키지: Timeline 1.8.10, Cinemachine 3.1.7 (Splines·Mathematics 같이 깔림)
- Cinemachine Timeline: `CinemachineTrack`은 `CinemachineBrain`에 바인딩, `CinemachineShot`은 `ExposedReference`로 가상 카메라 참조. 겹친 샷은 실행 때 블렌드가 자동 계산됨 (`TimelinePlayable`이 그래프를 만들 때 `ComputeBlendsFromOverlaps`)
- AnimationTrack: 바인딩된 Animator의 루트 Transform만 track offset 영향을 받음 → 루트(`WagonRig`)는 두고 자식(`Stagecoach`) 위치를 움직임
- Timeline 클립 밖 시간(클립 사이·끝난 뒤)엔 Animator가 씬 값을 씀 → 씬 값을 연출 끝 상태로 둠 (마차 z -27, 덮개·자막 알파 0)
- `CinemachineCamera.Target`·`CinemachineFollow.TrackerSettings`는 struct (복사해서 고친 뒤 다시 넣음). `BindingMode`는 `Unity.Cinemachine.TargetTracking` 네임스페이스

---

## C# 변경

### `Assets/Scripts/UI/Sequences/OpeningSequence.cs` (새 파일)

**추가 내용:** Timeline 재생·끝 알림, 연출 카메라는 연출 동안만 켬, 새로 누른 키를 1초 누르고 있으면 끝으로 건너뜀 (누르면 안내 글 보임)

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Mage
{
    /// <summary>전초기지 첫 도착 역마차 연출: Timeline을 틀고 끝나면 알림. 연출 동안만 연출 카메라를 켬. 새로 누른 키를 정해진 시간 누르고 있으면 끝으로 건너뜀 (누르면 안내 글이 보임)</summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        /// <summary>연출 Timeline을 트는 디렉터 (Play On Awake 끔, Wrap Mode None. 인스펙터에서만 연결)</summary>
        [SerializeField] PlayableDirector _director;

        /// <summary>연출 카메라 (Camera + CinemachineBrain + AudioListener). 연출 동안만 켬 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _cinematicCamera;

        /// <summary>건너뛰기 안내 글 (연출 중 키를 누르면 보임. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _skipHint;

        /// <summary>건너뛰려면 키를 누르고 있어야 하는 시간 (초, 게임 시간)</summary>
        [SerializeField, Min(0f)] float _skipHoldSeconds = 1f;

        /// <summary>연출이 끝남 (끝까지 봤거나 건너뜀). 알릴 땐 연출 카메라가 이미 꺼져 있음</summary>
        public event Action Finished;

        /// <summary>연출 중인지</summary>
        bool _playing;

        /// <summary>건너뛰기 키를 세기 시작했는지. 연출을 시작할 때 눌려 있던 키(로딩창 넘기기)를 다 뗀 뒤부터 셈</summary>
        bool _armed;

        /// <summary>건너뛰기 키를 이어서 누르고 있던 시간 (초)</summary>
        float _held;

        #region Awake - 연결 확인, 끝 알림 구독, 연출 카메라·안내 글 끄고 처음엔 꺼 둠 (Play에서 켬)
        void Awake()
        {
            if (_director == null || _cinematicCamera == null || _skipHint == null)
                Debug.LogError("[Opening] 디렉터·연출 카메라·건너뛰기 안내 연결 안 됨", this);
            if (_director != null) _director.stopped += OnStopped;
            if (_cinematicCamera != null) _cinematicCamera.SetActive(false);
            if (_skipHint != null) _skipHint.SetActive(false);
            enabled = false;
        }
        #endregion

        #region OnDestroy - 끝 알림 구독 해제
        void OnDestroy()
        {
            if (_director != null) _director.stopped -= OnStopped;
        }
        #endregion

        #region Play - 연출 카메라를 켜고 Timeline을 처음부터 틂. 첫 프레임(검은 화면)을 바로 적용. 디렉터가 없으면 바로 끝 알림
        public void Play()
        {
            _playing = true;
            if (_director == null)
            {
                Finish();
                return;
            }
            _armed = false;
            _held = 0f;
            enabled = true;
            if (_cinematicCamera != null) _cinematicCamera.SetActive(true);
            _director.time = 0d;
            _director.Play();
            _director.Evaluate();
        }
        #endregion

        #region Cancel - 끝 알림 없이 연출을 멈춤 (연출 중에 다른 상태로 갈 때. 씬이 내려가며 멈춤 알림이 와도 무시)
        public void Cancel()
        {
            if (!_playing) return;
            _playing = false;
            enabled = false;
            if (_director != null) _director.Stop();
        }
        #endregion

        #region Update - 게임 시간이 멈췄으면(일시정지) 안 셈. 시작 때 눌려 있던 키를 다 뗀 뒤부터, 키를 누르면 안내 글을 보이고 이어서 누른 시간이 차면 건너뜀
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            // 지금 키보드 키가 하나라도 눌려 있는지
            bool pressed = Keyboard.current != null && Keyboard.current.anyKey.isPressed;
            if (!_armed)
            {
                _armed = !pressed;
                return;
            }
            if (!pressed)
            {
                _held = 0f;
                return;
            }
            if (_skipHint != null) _skipHint.SetActive(true);
            _held += Time.deltaTime;
            if (_held >= _skipHoldSeconds) Skip();
        }
        #endregion

        #region Skip - 끝 프레임을 적용하고 멈춤 (멈춤 알림으로 끝 처리)
        void Skip()
        {
            _director.time = _director.duration;
            _director.Evaluate();
            _director.Stop();
        }
        #endregion

        #region OnStopped - Timeline이 끝나거나 멈춤 → 끝 처리
        /// <param name="director">멈춘 디렉터</param>
        void OnStopped(PlayableDirector director) => Finish();
        #endregion

        #region Finish - 연출 중이었으면 안내 글·연출 카메라를 끄고 끝 알림
        void Finish()
        {
            if (!_playing) return;
            _playing = false;
            enabled = false;
            if (_skipHint != null) _skipHint.SetActive(false);
            if (_cinematicCamera != null) _cinematicCamera.SetActive(false);
            Finished?.Invoke();
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `HubState.Reveal`(첫 도착) → `Finished` 구독 후 `Play()`. `HubState.Exit` → `Finished` 해제 후 `Cancel()`
- `PlayableDirector.stopped` → `OnStopped` (Wrap None이라 끝까지 가면 멈춤 알림, 건너뛰기·Cancel의 `Stop()`도 같은 알림)

---

### `Assets/Scripts/Core/RunEntry.cs`

**변경 내용:** 첫 도착 연출을 봤는지 추가, 디버그 판은 본 것으로

```csharp
        /// <summary>캐릭터 생성에서 고른 요소 (습득 요소). 숲의 주문 판정이 씀</summary>
        public List<ElementSO> LearnedElements { get; } = new();

        /// <summary>전초기지 첫 도착 연출(역마차)을 봤는지. 새 판은 false, 디버그 판은 true (연출은 새 게임으로 확인)</summary>
        public bool HasArrived { get; set; }   // ← 추가
```

```csharp
        // CreateDebug 안
        var run = new RunEntry { PlayerName = "디버그 마법사", QuestTitle = StubQuest(), HasArrived = true };   // ← HasArrived = true 추가
```

---

### `Assets/Scripts/UI/Views/HubView.cs`

**변경 내용:** `_spawn` 아래에 칸 3개, `Spawn` 프로퍼티 아래에 프로퍼티 3개

```csharp
        /// <summary>첫 도착 역마차 연출 (씬의 Opening. 인스펙터에서만 연결)</summary>
        [SerializeField] OpeningSequence _opening;

        /// <summary>첫 도착 때 플레이어가 서는 곳 (마차 오른쪽, 정문을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _arrivalPoint;

        /// <summary>조작하는 동안만 보이는 화면 글자 묶음 (파티 수·조준점·안내 글. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _hud;

        // ...

        /// <summary>첫 도착 역마차 연출</summary>
        public OpeningSequence Opening => _opening;

        /// <summary>첫 도착 때 플레이어가 서는 곳</summary>
        public Transform ArrivalPoint => _arrivalPoint;

        /// <summary>조작하는 동안만 보이는 화면 글자 묶음</summary>
        public GameObject Hud => _hud;
```

---

### `Assets/Scripts/Flow/HubState.cs`

**변경 내용:** 첫 도착 분기. `_arriving` 칸 추가, `OnSceneReady`·`Reveal`·`Exit` 교체, `OnOpeningFinished`·`StartControl` 새로 (`StartControl`은 3-1 `Reveal`의 조작 켜는 부분을 옮긴 것). 클래스 설명 "첫 도착이면 역마차 연출부터" 추가

```csharp
        /// <summary>이번 진입이 첫 도착이라 역마차 연출을 트는지 (OnSceneReady에서 정함)</summary>
        bool _arriving;

        #region OnSceneReady - 첫 도착이면 플레이어를 하차 지점에 두고 숨김·화면 글자 숨김(연출 뒤 켬), 아니면 시작 위치. 파티 수 표시, 안내 글 숨김
        protected override void OnSceneReady()
        {
            _view = FindView<HubView>();
            if (_view == null) return;
            _arriving = !Game.Run.HasArrived;
            _view.Player.PlaceAt(_arriving ? _view.ArrivalPoint : _view.Spawn);
            _view.Player.gameObject.SetActive(!_arriving);
            _view.Hud.SetActive(!_arriving);
            _view.InteractPrompt.gameObject.SetActive(false);
            RefreshParty();
        }
        #endregion

        #region Reveal - 커서 잠금. 첫 도착이면 연출을 틀고 끝나면 조작 시작, 아니면 바로 조작 시작
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            if (!_arriving)
            {
                StartControl();
                return;
            }
            _view.Opening.Finished += OnOpeningFinished;
            _view.Opening.Play();
        }
        #endregion

        #region OnOpeningFinished - 연출이 끝남: 도착 표시, 플레이어·화면 글자 켜고 조작 시작 (연출 카메라는 이미 꺼져서 같은 프레임에 플레이어 카메라로 넘어감)
        void OnOpeningFinished()
        {
            _view.Opening.Finished -= OnOpeningFinished;
            _arriving = false;
            Game.Run.HasArrived = true;
            _view.Player.gameObject.SetActive(true);
            _view.Hud.SetActive(true);
            StartControl();
        }
        #endregion

        #region StartControl - 지점 알림 구독, 조작·지점 찾기 켬 (로딩창을 넘길 때 누른 E는 구독 전이라 안 들어감)
        void StartControl()
        {
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.Player.enabled = true;
            _view.Detector.enabled = true;
        }
        #endregion

        #region Exit - 연출 중이면 알림 없이 멈춤, 알림 구독 해제, 조작·지점 찾기 끔, 커서 풀기
        public override void Exit()
        {
            if (_view != null)
            {
                _view.Opening.Finished -= OnOpeningFinished;
                _view.Opening.Cancel();
                _view.Detector.TargetChanged -= OnTargetChanged;
                _view.Detector.Interacted -= OnInteracted;
                _view.Player.enabled = false;
                _view.Detector.enabled = false;
            }
            Cursor.lockState = CursorLockMode.None;
        }
        #endregion
```

**호출/구독 관계:**
- `GameManager.SwitchState` → `Begin`(→ `OnSceneReady`) → 로딩창 넘김 → `Reveal` → (첫 도착) `OpeningSequence.Play` → `Finished` → `OnOpeningFinished` → `StartControl`
- 숲에서 돌아오거나 디버그 판이면 `Reveal` → 바로 `StartControl`

---

### `Assets/Scripts/Flow/CharacterCreationState.cs`

**변경 내용:** `StartRun` 끝에서 오프닝 대신 전초기지로

```csharp
        #region StartRun - 이름·고른 요소(카드 순서)를 판 데이터에 넣고 전초기지로 (첫 도착이라 역마차 연출부터)
        // ...
            Game.ChangeState(new HubState());   // ← new OpeningState() 에서 바꿈
```

---

### `Assets/Scripts/Managers/GameManager.cs`

**변경 내용:** `CreateStartState`의 switch에서 `OpeningState.Scene => new OpeningState(),` 줄 삭제

---

### 지운 파일

- `Assets/Scripts/Flow/OpeningState.cs`, `Assets/Scripts/UI/Views/OpeningView.cs` (`AssetDatabase.MoveAssetToTrash`)

---

## Prefab 변경

### `Assets/Prefabs/World/Stagecoach.prefab`

원점 = 짐칸 가운데 바닥, +z로 달림. 미리보기 씬에서 만들어 `SaveAsPrefabAsset`. 겉모습 도형은 콜라이더 삭제

1. 루트 `Stagecoach`: `BoxCollider` (Size (2.2, 2, 7.2), Center (0, 1, 1.55))
2. `Visual` 아래 (Cube, `Wood.mat`, 따로 적은 것만 다름):

| 이름 | 위치 | 스케일 | 비고 |
|---|---|---|---|
| Bed | (0, 1, 0) | (2, 0.2, 4) | 바닥 윗면 y 1.1 |
| SideLeft / SideRight | (±0.95, 1.45, 0) | (0.1, 0.7, 4) | |
| Back / Front | (0, 1.45, ∓1.95) | (2, 0.7, 0.1) | |
| BenchLeft / BenchRight | (±0.65, 1.325, -0.3) | (0.5, 0.45, 2.6) | 의자 윗면 y 1.55 |
| WheelFL·FR·BL·BR | (±1.05, 0.45, ±1.4) | (0.9, 0.08, 0.9) | Cylinder, 회전 (0, 0, 90), `Roof.mat` |
| ShaftLeft / ShaftRight | (±0.45, 1, 2.8) | (0.08, 0.08, 1.7) | |
| HorseBody | (0, 1.35, 3.7) | (0.7, 0.8, 1.8) | `Roof.mat` |
| HorseHead | (0, 1.95, 4.75) | (0.4, 0.5, 0.7) | `Roof.mat` |
| HorseLegFL·FR·BL·BR | (±0.22, 0.48, 4.35 / 3.05) | (0.16, 0.48, 0.16) | Cylinder, `Roof.mat` |

3. `Passenger` (꺼 둠): 위치 (0.65, 1.55, -0.3). 자식 `Body` Capsule (0, 0.45, 0) 스케일 (0.7, 0.45, 0.7), `HeadBall` Sphere (0, 0.8, 0) 스케일 0.3, 둘 다 `Player.mat`
4. `SeatHead`: 빈 오브젝트, 위치 (0.65, 2.4, -0.3), 회전 (0, 270, 0) (마차 안쪽을 봄 → 왼쪽이 마차 뒤, 오른쪽이 앞)

---

## 에셋 변경

### `Assets/Timelines/Opening.playable` (새로, 27초)

| 트랙 | 종류 | 바인딩 | 클립 |
|---|---|---|---|
| Cameras | CinemachineTrack | `CinematicCamera`의 CinemachineBrain | SeatForward 0–7.5, SeatLeft 6.5–10, SeatRight 9–14, OutOfBody 11.5–21, SeatRight 18–25.5, Dismount 24–27 (겹친 구간이 블렌드) |
| Wagon | AnimationTrack | `WagonRig` Animator | WagonDrive 0–23: `Stagecoach` `m_LocalPosition` x 0, y 0, z 키 (0, -140, 접선 6) → (23, -27, 접선 0) |
| Overlay | AnimationTrack | `OpeningOverlay` Animator | OverlayFade 0–5.2: 자기 CanvasGroup `m_Alpha` (0,1)(4.2,1)(4.45,0.35)(4.65,1)(5.2,0), `Monologue` CanvasGroup `m_Alpha` (0,0)(0.4,0)(0.9,1)(3.2,1)(3.7,0)(5.2,0), 구간마다 직선 |
| Passenger | ActivationTrack (Post-playback Inactive) | `Stagecoach/Passenger` | Ride 0–24 |

- AnimationClip 2개(WagonDrive·OverlayFade)는 Timeline 에셋 안 하위 에셋 (`AddObjectToAsset` 뒤 `AnimationTrack.CreateClip`)
- 샷마다 `VirtualCamera.exposedName`에 새 GUID, `director.SetReferenceValue`로 가상 카메라 연결

---

## 씬 변경

### `Assets/Scenes/Outpost.unity`

1. `ArrivalPoint`: 빈 오브젝트, 위치 (2.2, 0, -27), 회전 (0, 0, 0)
2. `Opening` (원점): `PlayableDirector` (Play On Awake 끔, Wrap Mode None, Update Method Game Time, Playable = `Opening.playable`) + `OpeningSequence` (`_director` → 이 디렉터, `_cinematicCamera` → `CinematicCamera`, `_skipHint` → `Canvas/SkipHint`, `_skipHoldSeconds` 1)
3. `Opening/CinematicCamera`: `Camera` (FOV 70, Near 0.05), `CinemachineBrain` (기본값), `AudioListener`. 꺼 둠
4. `Opening/WagonRig` (원점): `Animator` (컨트롤러 없음, Culling Always Animate). 자식 `Stagecoach` 프리팹, 위치 (0, 0, -27)
5. `Stagecoach/SeatHead` 아래 가상 카메라 3개: `CinemachineCamera` (FOV 70, Near 0.05, Far 1000) + `CinemachineBasicMultiChannelPerlin` (Handheld_normal_mild, Amplitude 1, Frequency 1.5)
   - `SeatForward` 회전 (3, 0, 0), `SeatLeft` (5, -70, 0), `SeatRight` (0, 70, 0) → 월드 yaw 270·200·340
6. `Opening/Cameras`:
   - `OutOfBody`: `CinemachineCamera` (FOV 60, Tracking Target = `Stagecoach`), `CinemachineFollow` (Follow Offset (7, 9, -12), Binding Mode World Space), `CinemachineRotationComposer` (Target Offset (0, 1, 8))
   - `Dismount`: `CinemachineCamera` (FOV 70, Near 0.05), 위치 (2.2, 1.65, -27), 회전 (0, 0, 0)
7. `Canvas`:
   - `Hud` 새로 (꽉 채움, 첫 자식): `PartyText`·`Crosshair`·`InteractPrompt`를 이 아래로 옮김
   - `OpeningOverlay` 새로 (꽉 채움): `Image` (검정, Raycast Target 끔), `CanvasGroup` (Alpha 0, Interactable·Blocks Raycasts 끔), `Animator` (Always Animate)
     - `Monologue`: LocalizedText 프리팹, 가운데, 크기 (1400, 120), 글자 40, 가운데 정렬, Raycast Target 끔, `CanvasGroup` (Alpha 0) 추가, 키 Opening/`opening.monologue`
   - `SkipHint` 새로: LocalizedText 프리팹, 앵커·피벗 (1, 0), 위치 (-40, 40), 크기 (600, 40), 글자 26, 오른쪽 정렬, Raycast Target 끔, 꺼 둠, 키 Opening/`opening.skip_hint`
8. 디렉터 바인딩: Cameras → `CinematicCamera` CinemachineBrain, Wagon → `WagonRig` Animator, Overlay → `OpeningOverlay` Animator, Passenger → `Stagecoach/Passenger`
9. HubView 연결: `_opening` → `Opening`의 OpeningSequence, `_arrivalPoint` → `ArrivalPoint`, `_hud` → `Canvas/Hud`
10. 저장 → 다시 열어 바인딩 4개·샷 6개 카메라 참조, OpeningSequence 3칸, HubView 8칸, 자막·안내 키, 켜진 리스너·카메라 각 1개 확인

### 지운 씬

- `Assets/Scenes/Opening.unity` (`MoveAssetToTrash`), 빌드 목록에서 Opening 뺌 (`EditorBuildSettings.scenes`)

---

## 다국어 변경

1. 시트에 `Opening` 탭 추가 (sheetId 1377437768), 머리글 15칸:

| Key | Comment | ko | en |
|---|---|---|---|
| `opening.monologue` | 오프닝 첫 검은 화면 혼잣말 자막 (임시 문구) | 빌어먹을, 저곳이 앞으로 뺑이칠 곳이군 | Damn it. So that's where I'll be grinding away from now on. |
| `opening.skip_hint` | 오프닝 연출 중 키를 누르면 보이는 건너뛰기 안내 | 아무 키나 길게 눌러 건너뛰기 | Hold any key to skip |

2. `UI` 탭: `opening.body` 줄 지움 (지우기 직전 행 번호 다시 확인)
3. `Assets/Localization/CSV/Opening.csv` 새로, `UI.csv`에서 `opening.body` 줄 뺌
4. `LocalizationEditorSettings.CreateStringTableCollection("Opening", "Assets/Localization/Tables", GetLocales())` (Shared Data GUID `0302f61167524ed44b56ddef704a58c9`)
5. `Import Tables from CSV`
6. UI 컬렉션에서 `opening.body` 제거: id를 적어 두고 `RemoveEntry` + 언어 표마다 `RemoveEntry(id)` + SetDirty, `SaveAssets`
7. `Check Missing Translations`·`Check Missing Glyphs` (빠진 글자 0)

---

## 주의사항

- 첫 도착 판단은 `RunEntry.HasArrived`. 디버그 판(중간 씬에서 바로 Play)은 true라 연출 안 나옴 → 연출 확인은 새 게임이나 CharacterCreation 씬에서 Play
- 연출 동안 Player 오브젝트를 꺼서 몸·카메라·리스너가 빠짐. 리스너는 연출 카메라 것 하나. 끝날 때 같은 프레임에 연출 카메라를 끄고 Player를 켬
- `Dismount` 카메라 = `ArrivalPoint` + (0, 1.65, 0), FOV 70·Near 0.05 = Player 카메라. 하나라도 다르면 넘어갈 때 화면이 튐
- 씬 값을 연출 끝 상태로 둠 (마차 z -27, 덮개·자막 알파 0). Timeline 클립 밖 시간엔 씬 값이 보임
- 좌석 왼쪽·오른쪽은 yaw 200·340. 180° 차이면 블렌드가 앞(270)을 안 지나고 반대로 돌 수 있음
- 연출 중 '타이틀로'는 `Exit` → `Cancel`로 멈춤 (끝 알림 없음, 승객은 Post-playback으로 꺼짐)
- 건너뛰기: 로딩창을 넘길 때 누른 키는 다 뗀 뒤부터 셈, 일시정지(timeScale 0) 중엔 안 셈
- 빌더에서 에셋(프리팹·노이즈 프리셋)은 씬을 연 뒤 쓰기 직전에 읽음. LocalizedText 키는 SerializedObject로 `m_StringReference…`에 넣음

---

## 진행 추적

- [x] 묶음 Logic — OpeningSequence 새로, RunEntry·HubView·HubState·CharacterCreationState·GameManager 수정, OpeningState·OpeningView·Opening 씬 지움·빌드 목록에서 뺌. 컴파일 에러·경고 0
- [x] 묶음 Editor-Localization — 시트 Opening 탭·UI 탭 opening.body 지움, CSV 2개, Opening 테이블 만들기·Import, UI의 opening.body 제거, 누락 검사 (빠진 글자 0)
- [x] 묶음 Prefab — Stagecoach (짐칸·의자·바퀴·말·끌채, 승객, SeatHead)
- [x] 묶음 Editor-Scene — Outpost 연출 묶음·Canvas Hud·덮개·안내, Opening.playable·바인딩, 저장 뒤 다시 열어 확인
- [x] 묶음 Verify — 사용자 확인 (2026-11-20)

---

## 확인 목록 (사용자, 에디터 Play)

- 새 게임 → 생성 → 로딩창에서 키 → 검은 화면에 혼잣말 자막 → 깜빡이며 눈 뜸 → 흔들리는 마차 좌석에서 마차 안쪽이 보임
- 왼쪽(마차 뒤 길) → 오른쪽(말과 전초기지) 순서로 고개를 돌림
- 위로 빠져나가며 3인칭: 오른쪽 뒤 위에서 승객이 탄 마차와 전초기지가 보임
- 다시 좌석 시점 → 정문 앞에서 멈춤 → 오른쪽으로 내려서 정문을 봄 → 화면이 튀지 않고 바로 조작, 파티 수·조준점이 나타남
- 연출 중 아무 키 → 오른쪽 아래 "아무 키나 길게 눌러 건너뛰기", 1초 누르고 있으면 하차 지점에서 바로 조작. 로딩창을 넘길 때 누른 키로는 안 건너뜀
- 연출 중 Esc → 일시정지 메뉴가 뜨고 연출 멈춤. 계속하기 → 이어서 진행. 타이틀로 → 에러 없이 타이틀
- 연출이 끝나면 마차가 정문 밖 길에 서 있고 부딪힘
- 숲 갔다가 돌아오면 연출 없이 정문 안쪽에서 시작
- Outpost 씬에서 바로 Play → 연출 없음. CharacterCreation 씬에서 바로 Play → 생성을 끝내면 연출 나옴
- 영어로 바꾸면 자막·안내 글도 영어
- 콘솔 에러·경고 없음 (오디오 리스너 2개 경고 포함)

---

## 커밋 메시지

```
Add — 전초기지 첫 도착 역마차 연출 (Timeline + Cinemachine), Opening 씬 지움

- 연출: 검은 화면 혼잣말 → 눈 뜸 → 좌석에서 앞·왼쪽(마차 뒤)·오른쪽(전초기지) → 3인칭 유체이탈 → 다시 좌석 → 정문 밖에 멈춰 하차 → 1인칭 조작 (약 27초)
- Assets/Timelines/Opening.playable: Cinemachine 샷 6개(겹쳐서 블렌드), 마차 이동(z -140 → -27), 검은 덮개 깜빡임·자막 알파, 승객 인형 켜기
- OpeningSequence: Timeline 재생·끝 알림, 연출 카메라(Brain·리스너)는 연출 동안만 켬, 새로 누른 키 1초 길게 눌러 건너뛰기 + 안내 글
- HubState: 첫 도착(RunEntry.HasArrived)이면 플레이어·HUD 숨기고 연출 → 하차 지점(ArrivalPoint)에서 조작 시작. 숲에서 돌아오면 정문 안쪽. 디버그 판은 도착한 판
- Stagecoach 프리팹(짐칸·의자·바퀴·말 도형, 승객·눈 위치), Canvas에 Hud 묶음·검은 덮개·건너뛰기 안내
- 생성 → 바로 전초기지. Opening 씬·OpeningState·OpeningView·빌드 목록 Opening·UI의 opening.body 지움
- 다국어: Opening 테이블 새로 (opening.monologue·opening.skip_hint)
- Cinemachine 3.1.7 설치

Why: 오프닝을 슬라이드가 아니라 전초기지 필드 위 연출로 해서 도착부터 플레이까지 끊김 없이 잇기 위함
Alternatives: Timeline만으로 카메라 키프레임 (블렌드·흔들림·따라가기를 직접 만들어야 함), 연출 중 실제 플레이어를 마차에 태움 (3인칭에서 서 있는 몸이 보이고 CharacterController를 끄고 켜야 함), 건너뛰기 없음 (반복 확인이 느림)
```
