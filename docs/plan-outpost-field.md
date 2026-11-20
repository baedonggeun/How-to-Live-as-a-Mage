# Plan: 전초기지 도형 필드 + 1인칭 플레이어 + E 지점 (3-1)

## 현행 분석

- `Assets/Scenes/Outpost.unity`: 3D 필드 없음. 루트는 `Main Camera`(AudioListener)와 `Canvas`(HubView). Canvas 아래 `PartyText`(LocalizedText, UI/`hub.party_count`), `RecruitButton`(UI/`hub.recruit`), `DepartButton`(UI/`hub.depart`)
- `HubState`: `OnSceneReady`에서 영입·출발 버튼 연결, 파티 수 표시. `WaitsForContinue`·`UsesPauseMenu` true. 출발 때 `RunEntry.StubQuest()`를 넣고 `FieldState`로. 토스트는 UI 테이블(`ShowToast(string, …)`)
- 흐름: `GameManager.SwitchState`가 로딩창 밑에서 `Begin`(→ `OnSceneReady`) → 넘기기 키 대기 → 덮개 걷음 → `Reveal`. 로딩창 동안 `Time.timeScale = 0`
- 일시정지: `GameManager.SetMenuPaused(true)`가 `Time.timeScale = 0`, `Player` 액션 묶음 Disable, 커서 풀기. 닫으면 묶음 Enable, 커서는 열기 전 상태로 되돌림
- `FieldState`: `Reveal`에서 커서 잠금 + `Player/Interact` 구독, `Exit`에서 해제·커서 풀기. 숲에서 E → `new HubState()`
- 입력(`Assets/InputSystem_Actions.inputactions`, 전역 `InputSystem.actions`): `Player/Move`(WASD·방향키), `Player/Look`(`<Pointer>/delta`), `Player/Sprint`(`<Keyboard>/leftShift`), `Player/Interact`(`<Keyboard>/e`, 그룹 `Keyboard&Mouse`)
- 패키지: ProBuilder 없음 → 원뿔은 메시 에셋을 만들어 씀
- 다국어: 테이블은 영역별 (CLAUDE.md). 테이블 GUID는 씬·에셋에 `GUID:{Shared Data GUID}`로 저장. 키 옮기기는 Shared Data `RemoveEntry` + 언어 표마다 `RemoveEntry(id)` + SetDirty

---

## C# 변경

### `Assets/Scripts/Player/PlayerController.cs` (새 파일)

**추가 내용:** 1인칭 이동·달리기·마우스 시점·중력. 처음엔 꺼져 있고 `HubState.Reveal`이 켬

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>1인칭 조작: 이동·달리기·마우스 시점·중력. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        /// <summary>이동 입력 액션 경로 (전역 액션, WASD)</summary>
        const string MoveActionPath = "Player/Move";

        /// <summary>시점 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>달리기 입력 액션 경로 (전역 액션, 왼쪽 Shift)</summary>
        const string SprintActionPath = "Player/Sprint";

        /// <summary>땅에 있을 때 아래로 누르는 속도 (m/s). 경사·턱에서 뜨지 않게</summary>
        const float GroundedFallSpeed = -2f;

        /// <summary>몸 (충돌·이동. 인스펙터에서만 연결)</summary>
        [SerializeField] CharacterController _body;

        /// <summary>머리 (카메라 자리, 위아래로 돎. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _head;

        /// <summary>걷기 속도 (m/s)</summary>
        [SerializeField, Min(0f)] float _walkSpeed = 5.6f;

        /// <summary>달리기 속도 (m/s)</summary>
        [SerializeField, Min(0f)] float _sprintSpeed = 8.2f;

        /// <summary>마우스 감도 (이동량 1당 도)</summary>
        [SerializeField, Min(0f)] float _lookSensitivity = 0.1f;

        /// <summary>위아래로 볼 수 있는 한도 (도)</summary>
        [SerializeField, Range(0f, 89f)] float _pitchLimit = 85f;

        /// <summary>중력 가속도 (m/s², 아래가 음수)</summary>
        [SerializeField] float _gravity = -20f;

        /// <summary>이동 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _move;

        /// <summary>시점 입력. 없으면 null</summary>
        InputAction _look;

        /// <summary>달리기 입력. 없으면 null</summary>
        InputAction _sprint;

        /// <summary>지금 위아래 각도 (도, 위를 보면 음수)</summary>
        float _pitch;

        /// <summary>지금 떨어지는 속도 (m/s, 아래가 음수)</summary>
        float _fallSpeed;

        /// <summary>머리 (카메라 자리)</summary>
        public Transform Head => _head;

        #region Awake - 연결 확인, 입력 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_body == null || _head == null) Debug.LogError("[Player] 몸·머리 연결 안 됨");
            _move = FindAction(MoveActionPath);
            _look = FindAction(LookActionPath);
            _sprint = FindAction(SprintActionPath);
            enabled = false;
        }
        #endregion

        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[Player] {path} 액션 없음");
            return action;
        }
        #endregion

        #region PlaceAt - 지점 위치·좌우 방향으로 옮기고 위아래 각도·낙하 속도 초기화 (CharacterController는 켜진 채 옮기면 무시돼서 잠깐 끔)
        /// <param name="point">옮길 지점</param>
        public void PlaceAt(Transform point)
        {
            if (point == null) throw new System.ArgumentNullException(nameof(point));
            _body.enabled = false;
            transform.SetPositionAndRotation(point.position, Quaternion.Euler(0f, point.eulerAngles.y, 0f));
            _body.enabled = true;
            _pitch = 0f;
            _fallSpeed = 0f;
            _head.localRotation = Quaternion.identity;
        }
        #endregion

        #region Update - 게임 시간이 멈췄으면(로딩창·일시정지) 아무것도 안 함. 아니면 시점 → 이동
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            Look();
            Move();
        }
        #endregion

        #region Look - 마우스 이동량으로 좌우는 몸, 위아래는 머리
        void Look()
        {
            if (_look == null) return;
            // 이번 프레임 회전량 (도)
            Vector2 delta = _look.ReadValue<Vector2>() * _lookSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            _pitch = Mathf.Clamp(_pitch - delta.y, -_pitchLimit, _pitchLimit);
            _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
        #endregion

        #region Move - 몸 기준 앞·옆으로 걷기·달리기, 땅이면 살짝 눌러 붙이고 아니면 중력으로 떨어짐
        void Move()
        {
            // 이동 입력 (x 옆, y 앞. 대각선도 1을 안 넘게)
            Vector2 input = _move != null ? Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            // 지금 속도 (달리기를 누르고 있으면 달리기 속도)
            float speed = _sprint != null && _sprint.IsPressed() ? _sprintSpeed : _walkSpeed;
            // 몸 기준 평면 이동 방향
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            _fallSpeed = _body.isGrounded ? GroundedFallSpeed : _fallSpeed + _gravity * Time.deltaTime;
            _body.Move((direction * speed + Vector3.up * _fallSpeed) * Time.deltaTime);
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `HubState.OnSceneReady` → `PlaceAt(view.Spawn)`
- `HubState.Reveal` → `enabled = true`, `HubState.Exit` → `enabled = false`
- 일시정지 중엔 `Player` 묶음이 꺼져 입력이 0, `Time.timeScale = 0`이라 Update가 바로 끝남

---

### `Assets/Scripts/Player/InteractDetector.cs` (새 파일)

**추가 내용:** 눈 앞 2.5m 레이캐스트로 바라본 `InteractPointView`를 찾고, 대상이 바뀌거나 E를 누르면 알림. 처음엔 꺼짐

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mage
{
    /// <summary>눈 앞 일정 거리 안에서 바라보는 상호작용 지점을 찾고, E(Player/Interact)를 누르면 알림. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
    public sealed class InteractDetector : MonoBehaviour
    {
        /// <summary>상호작용 입력 액션 경로 (전역 액션, E)</summary>
        const string InteractActionPath = "Player/Interact";

        /// <summary>키 이름을 읽을 바인딩 그룹 (키보드·마우스)</summary>
        const string KeyboardMouseGroup = "Keyboard&Mouse";

        /// <summary>레이를 쏠 눈 (플레이어 머리. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _eye;

        /// <summary>닿는 거리 (m)</summary>
        [SerializeField, Min(0.1f)] float _range = 2.5f;

        /// <summary>레이가 맞을 레이어</summary>
        [SerializeField] LayerMask _mask = ~0;

        /// <summary>상호작용 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _interact;

        /// <summary>지금 바라보는 지점. 없으면 null</summary>
        public InteractPointView Target { get; private set; }

        /// <summary>바라보는 지점이 바뀜 (없어지면 null)</summary>
        public event Action<InteractPointView> TargetChanged;

        /// <summary>지점을 바라보며 E를 누름</summary>
        public event Action<InteractPointView> Interacted;

        /// <summary>E 키 이름 (안내 글용, 키보드 기준. 액션이 없으면 빈 문자열)</summary>
        public string KeyName => _interact != null
            ? _interact.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardMouseGroup))
            : string.Empty;

        #region Awake - 연결 확인, E 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_eye == null) Debug.LogError("[Interact] 눈 연결 안 됨");
            _interact = InputSystem.actions.FindAction(InteractActionPath);
            if (_interact == null) Debug.LogError($"[Interact] {InteractActionPath} 액션 없음");
            enabled = false;
        }
        #endregion

        #region OnEnable - E 구독
        void OnEnable()
        {
            if (_interact != null) _interact.performed += OnInteract;
        }
        #endregion

        #region OnDisable - E 구독 해제, 바라보던 지점 비움 (알림)
        void OnDisable()
        {
            if (_interact != null) _interact.performed -= OnInteract;
            SetTarget(null);
        }
        #endregion

        #region Update - 눈 앞으로 레이를 쏴서 맞은 콜라이더(트리거 포함)의 지점 표지를 대상으로
        void Update()
        {
            if (_eye == null) return;
            // 맞은 곳의 지점 표지 (없으면 null)
            InteractPointView found = null;
            if (Physics.Raycast(_eye.position, _eye.forward, out RaycastHit hit, _range, _mask, QueryTriggerInteraction.Collide))
                found = hit.collider.GetComponentInParent<InteractPointView>();
            SetTarget(found);
        }
        #endregion

        #region SetTarget - 대상이 바뀌었을 때만 바꾸고 알림
        /// <param name="target">새 대상 (없으면 null)</param>
        void SetTarget(InteractPointView target)
        {
            if (target == Target) return;
            Target = target;
            TargetChanged?.Invoke(target);
        }
        #endregion

        #region OnInteract - 바라보는 지점이 있으면 E 알림
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnInteract(InputAction.CallbackContext context)
        {
            if (Target != null) Interacted?.Invoke(Target);
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `HubState.Reveal`에서 `TargetChanged`·`Interacted` 구독 후 `enabled = true`, `Exit`에서 해제·`enabled = false`
- 레이는 머리(캡슐 안)에서 출발해서 플레이어 자신의 CharacterController는 안 맞음

---

### `Assets/Scripts/World/InteractPointView.cs` (새 파일)

**추가 내용:** 상호작용 지점 표지 (로직 없음)

```csharp
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>E로 쓸 수 있는 곳 표지 (씬 오브젝트, 로직 없음). 무엇을 할지는 상태가 Id로 정함. 콜라이더가 같은 오브젝트나 자식에 있어야 바라볼 때 잡힘</summary>
    public sealed class InteractPointView : MonoBehaviour
    {
        /// <summary>상태가 구분할 id (전초기지: recruit, board, depart). 인스펙터에서만 정함</summary>
        [SerializeField] string _id;

        /// <summary>안내에 보일 이름 ("[E] 출발"의 출발). 인스펙터에서만 정함</summary>
        [SerializeField] LocalizedString _label;

        /// <summary>상태가 구분할 id</summary>
        public string Id => _id;

        /// <summary>안내에 보일 이름</summary>
        public LocalizedString Label => _label;
    }
}
```

---

### `Assets/Scripts/UI/Views/HubView.cs` (전체 교체)

**변경 내용:** `_recruitButton`·`_departButton` 삭제, `_interactPrompt`·`_player`·`_detector`·`_spawn` 추가. `_partyText`는 이름 그대로 (씬 참조 유지)

```csharp
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>전초기지 화면·씬 참조</summary>
    public sealed class HubView : MonoBehaviour
    {
        /// <summary>파티 수 글자 (Hub/hub.party_count, {0}/{1}. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _partyText;

        /// <summary>상호작용 안내 글 (UI/common.interact_prompt, {0} 키 이름·{1} 지점 이름. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _interactPrompt;

        /// <summary>1인칭 조작 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] PlayerController _player;

        /// <summary>바라본 지점 찾기 (씬의 Player. 인스펙터에서만 연결)</summary>
        [SerializeField] InteractDetector _detector;

        /// <summary>플레이어 시작 위치 (정문 안쪽. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _spawn;

        /// <summary>파티 수 글자</summary>
        public LocalizeStringEvent PartyText => _partyText;

        /// <summary>상호작용 안내 글</summary>
        public LocalizeStringEvent InteractPrompt => _interactPrompt;

        /// <summary>1인칭 조작</summary>
        public PlayerController Player => _player;

        /// <summary>바라본 지점 찾기</summary>
        public InteractDetector Detector => _detector;

        /// <summary>플레이어 시작 위치</summary>
        public Transform Spawn => _spawn;
    }
}
```

---

### `Assets/Scripts/Flow/HubState.cs` (전체 교체)

**변경 내용:** 버튼 대신 E 지점(영입·게시판·출발). 의뢰 설정을 출발에서 게시판으로 옮기고 출발은 파티 + 의뢰 검사. 토스트는 Hub 테이블. `Reveal`·`Exit` 추가

```csharp
using System;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>전초기지. 1인칭으로 돌아다니며 E 지점에서 임시 영입·의뢰 받기·출발 → 숲</summary>
    public sealed class HubState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "Outpost";

        /// <summary>출발에 필요한 용병 수</summary>
        public const int RequiredMercenaries = 2;

        /// <summary>영입소 지점 id</summary>
        const string RecruitId = "recruit";

        /// <summary>게시판 지점 id</summary>
        const string BoardId = "board";

        /// <summary>출발 표지판 지점 id</summary>
        const string DepartId = "depart";

        /// <summary>파티가 모자랄 때 토스트 키 ({0} 필요한 인원)</summary>
        const string PartyRequiredKey = "hub.party_required";

        /// <summary>의뢰를 받았을 때 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestTakenKey = "hub.quest_taken";

        /// <summary>의뢰 없이 출발할 때 토스트 키</summary>
        const string QuestRequiredKey = "hub.quest_required";

        /// <summary>전초기지 문구 테이블</summary>
        static readonly TableReference Table = "Hub";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>들어올 때 로딩창에 팁을 띄우고 넘기기 버튼을 기다림 (플레이 씬)</summary>
        public override bool WaitsForContinue => true;

        /// <summary>Esc로 일시정지 메뉴를 열 수 있음 (플레이 씬)</summary>
        public override bool UsesPauseMenu => true;

        /// <summary>이 씬의 화면 참조</summary>
        HubView _view;

        #region OnSceneReady - 플레이어를 시작 위치에, 파티 수 표시, 안내 글 숨김
        protected override void OnSceneReady()
        {
            _view = FindView<HubView>();
            if (_view == null) return;
            _view.Player.PlaceAt(_view.Spawn);
            _view.InteractPrompt.gameObject.SetActive(false);
            RefreshParty();
        }
        #endregion

        #region Reveal - 커서 잠금, 지점 알림 구독, 조작·지점 찾기 켬 (로딩창을 넘길 때 누른 E는 구독 전이라 안 들어감)
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.Player.enabled = true;
            _view.Detector.enabled = true;
        }
        #endregion

        #region Exit - 지점 알림 구독 해제, 조작·지점 찾기 끔, 커서 풀기
        public override void Exit()
        {
            if (_view != null)
            {
                _view.Detector.TargetChanged -= OnTargetChanged;
                _view.Detector.Interacted -= OnInteracted;
                _view.Player.enabled = false;
                _view.Detector.enabled = false;
            }
            Cursor.lockState = CursorLockMode.None;
        }
        #endregion

        #region OnTargetChanged - 바라보는 지점이 있으면 "[E] 이름", 없으면 숨김
        /// <param name="target">바라보는 지점 (없으면 null)</param>
        void OnTargetChanged(InteractPointView target)
        {
            // 안내 글
            var prompt = _view.InteractPrompt;
            if (target == null)
            {
                prompt.gameObject.SetActive(false);
                return;
            }
            prompt.StringReference.Arguments = new object[] { _view.Detector.KeyName, target.Label.GetLocalizedString() };
            prompt.gameObject.SetActive(true);
            prompt.RefreshString();
        }
        #endregion

        #region OnInteracted - 지점 id로 나눔: 영입·의뢰 받기·출발. 모르는 id는 경고
        /// <param name="target">E를 누를 때 바라본 지점</param>
        void OnInteracted(InteractPointView target)
        {
            if (string.Equals(target.Id, RecruitId, StringComparison.Ordinal)) Recruit();
            else if (string.Equals(target.Id, BoardId, StringComparison.Ordinal)) TakeQuest();
            else if (string.Equals(target.Id, DepartId, StringComparison.Ordinal)) Depart();
            else Debug.LogWarning($"[Hub] 모르는 지점 id: {target.Id} ({target.name})");
        }
        #endregion

        #region Recruit - 임시 영입: 파티가 비었을 때만 고정 용병을 넣음 (4번에서 후보 선택으로 바뀜)
        void Recruit()
        {
            if (Game.Run.Party.Count >= RequiredMercenaries) return;
            Game.Run.Party.AddRange(RunEntry.StubMercenaries());
            RefreshParty();
        }
        #endregion

        #region TakeQuest - 임시 의뢰 받기 + 토스트 (4번에서 게시판 창으로 바뀜)
        void TakeQuest()
        {
            Game.Run.QuestTitle = RunEntry.StubQuest();
            UIManager.Instance.ShowToast(Table, QuestTakenKey, Game.Run.QuestTitle.GetLocalizedString());
        }
        #endregion

        #region Depart - 파티가 모자라거나 의뢰가 없으면 토스트, 아니면 숲으로
        void Depart()
        {
            if (Game.Run.Party.Count < RequiredMercenaries)
            {
                UIManager.Instance.ShowToast(Table, PartyRequiredKey, RequiredMercenaries);
                return;
            }
            if (Game.Run.QuestTitle == null)
            {
                UIManager.Instance.ShowToast(Table, QuestRequiredKey);
                return;
            }
            Game.ChangeState(new FieldState());
        }
        #endregion

        #region RefreshParty - 파티 수 표시 갱신 ({0}/{1})
        void RefreshParty()
        {
            _view.PartyText.StringReference.Arguments = new object[] { Game.Run.Party.Count, RequiredMercenaries };
            _view.PartyText.RefreshString();
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `GameManager.SwitchState` → `Begin`(→ `OnSceneReady`) → 덮개 걷은 뒤 `Reveal`, 다음 전환 때 `Exit`
- `InteractDetector.TargetChanged` → `OnTargetChanged`, `InteractDetector.Interacted` → `OnInteracted`
- 디버그 판(Outpost 씬에서 바로 Play)은 `RunEntry.CreateDebug`가 파티 2명·의뢰를 이미 넣어서 출발 바로 가능

---

## 에셋 변경

### `Assets/Art/Placeholder/Meshes/Cone.asset` (새로, 에디터 스크립트로 한 번 만듦)

밑면 지름 1, 높이 1, 가운데가 원점 (기본 도형처럼 스케일로 크기 조절). 옆면은 면마다 꼭지점을 따로 둬서 각지게

```csharp
// 원뿔 메시 만들기 (sides = 24). 밑면 y -0.5, 꼭대기 y 0.5, 반지름 0.5
Mesh BuildCone(int sides)
{
    var vertices = new List<Vector3>();
    var triangles = new List<int>();
    var apex = new Vector3(0f, 0.5f, 0f);
    for (int i = 0; i < sides; i++)
    {
        float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
        var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, -0.5f, Mathf.Sin(a0) * 0.5f);
        var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, -0.5f, Mathf.Sin(a1) * 0.5f);
        int b = vertices.Count;
        vertices.Add(p0); vertices.Add(apex); vertices.Add(p1);
        triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 2);      // 옆면 (밖에서 보면 시계 방향)
    }
    int center = vertices.Count;
    vertices.Add(new Vector3(0f, -0.5f, 0f));
    for (int i = 0; i < sides; i++)
    {
        float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
        int b = vertices.Count;
        vertices.Add(new Vector3(Mathf.Cos(a0) * 0.5f, -0.5f, Mathf.Sin(a0) * 0.5f));
        vertices.Add(new Vector3(Mathf.Cos(a1) * 0.5f, -0.5f, Mathf.Sin(a1) * 0.5f));
        triangles.Add(center); triangles.Add(b); triangles.Add(b + 1);      // 밑면 (아래에서 보면 시계 방향)
    }
    var mesh = new Mesh { name = "Cone" };
    mesh.SetVertices(vertices);
    mesh.SetTriangles(triangles, 0);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    return mesh;
}
// AssetDatabase.CreateAsset(BuildCone(24), "Assets/Art/Placeholder/Meshes/Cone.asset");
```

### `Assets/Art/Placeholder/Materials/` (새로, 셰이더 `Universal Render Pipeline/Lit`, `_BaseColor`)

| 파일 | 색 (RGB) | 쓰는 곳 |
|---|---|---|
| `Ground.mat` | 0.36, 0.42, 0.24 | 바닥 |
| `Road.mat` | 0.55, 0.45, 0.32 | 길 |
| `Wood.mat` | 0.45, 0.32, 0.20 | 울타리·기둥·줄기·벽·판 |
| `Roof.mat` | 0.50, 0.18, 0.15 | 원뿔 지붕·문 |
| `Leaves.mat` | 0.20, 0.42, 0.20 | 나무 잎 |
| `Player.mat` | 0.30, 0.45, 0.75 | 플레이어 몸 |

---

## Prefab 변경

공통: 루트에 콜라이더(+ 스크립트), 겉모습은 `Visual` 자식. `Visual` 아래 기본 도형은 콜라이더 삭제. 원뿔은 빈 오브젝트 + MeshFilter(`Cone.asset`) + MeshRenderer. 미리보기 씬(`EditorSceneManager.NewPreviewScene`)에서 만들어 `PrefabUtility.SaveAsPrefabAsset`.

### `Assets/Prefabs/World/Player.prefab`

1. 루트 `Player`: Add Component → `CharacterController` (Radius 0.35, Height 1.8, Center (0, 0.9, 0), Step Offset 0.4, Slope Limit 45)
2. Add Component → `PlayerController`, `InteractDetector`
3. 자식 `Visual`:
   - `Body`: Capsule, 위치 (0, 0.9, 0), 스케일 (0.7, 0.9, 0.7), `Player.mat`
   - `HeadBall`: Sphere, 위치 (0, 1.6, 0), 스케일 0.3, `Player.mat`
4. 자식 `Head`: 빈 오브젝트, 위치 (0, 1.65, 0)
   - Add Component → `Camera` (Tag MainCamera, Field of View 70, Near 0.05), `AudioListener` (URP 카메라 데이터는 자동)
5. SerializeField 연결: `PlayerController._body` → 루트 CharacterController, `PlayerController._head` → `Head`, `InteractDetector._eye` → `Head`

### `Assets/Prefabs/World/Tree.prefab`

1. 루트: `CapsuleCollider` (Radius 0.3, Height 4, Center (0, 2, 0))
2. `Visual/Trunk`: Cylinder, 위치 (0, 1.5, 0), 스케일 (0.4, 1.5, 0.4), `Wood.mat`
3. `Visual/Leaves`: 원뿔, 위치 (0, 4.2, 0), 스케일 (2.4, 3.5, 2.4), `Leaves.mat`

### `Assets/Prefabs/World/Hut.prefab`

1. 루트: `CapsuleCollider` (Radius 2.5, Height 5, Center (0, 2.5, 0))
2. `Visual/Wall`: Cylinder, 위치 (0, 1.5, 0), 스케일 (5, 1.5, 5), `Wood.mat`
3. `Visual/Roof`: 원뿔, 위치 (0, 4.25, 0), 스케일 (6, 2.5, 6), `Roof.mat`
4. `Visual/Door`: Cube, 위치 (0, 1, -2.5), 스케일 (1.2, 2, 0.1), `Roof.mat` (문이 -z 쪽 = 정면)

### `Assets/Prefabs/World/Tower.prefab`

1. 루트: `BoxCollider` (Size (2.5, 8, 2.5), Center (0, 4, 0))
2. `Visual/Post`: Cylinder, 위치 (0, 3, 0), 스케일 (2, 3, 2), `Wood.mat`
3. `Visual/Roof`: 원뿔, 위치 (0, 7, 0), 스케일 (3, 2, 3), `Roof.mat`

### `Assets/Prefabs/World/Board.prefab`

1. 루트: `BoxCollider` (Size (2.4, 2.4, 0.4), Center (0, 1.2, 0))
2. `Visual/PostLeft`: Cylinder, 위치 (-1, 1.1, 0), 스케일 (0.15, 1.1, 0.15), `Wood.mat`
3. `Visual/PostRight`: Cylinder, 위치 (1, 1.1, 0), 스케일 (0.15, 1.1, 0.15), `Wood.mat`
4. `Visual/Panel`: Cube, 위치 (0, 1.6, 0), 스케일 (2.2, 1.2, 0.1), `Wood.mat`

### `Assets/Prefabs/World/Signpost.prefab`

1. 루트: `BoxCollider` (Size (1.4, 2.4, 0.4), Center (0, 1.2, 0))
2. `Visual/Post`: Cylinder, 위치 (0, 1.2, 0), 스케일 (0.12, 1.2, 0.12), `Wood.mat`
3. `Visual/Sign`: Cube, 위치 (0, 2, 0), 스케일 (1.2, 0.5, 0.08), `Wood.mat`

---

## 씬 변경

### `Assets/Scenes/Outpost.unity`

1. 지움: 루트 `Main Camera`, `Canvas/RecruitButton`, `Canvas/DepartButton`
2. `Directional Light`: 회전 (50, -30, 0), Intensity 1, Soft Shadows
3. `Ground`: Plane, 스케일 (30, 1, 30), `Ground.mat`
4. `Walls` (빈 부모) 아래 Cube 5개 (높이 3, 두께 0.5, `Wood.mat`, 콜라이더 유지):
   - North: 위치 (0, 1.5, 20), 스케일 (40, 3, 0.5)
   - East: (20, 1.5, 0), (0.5, 3, 40)
   - West: (-20, 1.5, 0), (0.5, 3, 40)
   - SouthLeft: (-11.5, 1.5, -20), (17, 3, 0.5)
   - SouthRight: (11.5, 1.5, -20), (17, 3, 0.5)
5. `Towers` 아래 Tower 프리팹 4개: (±20, 0, ±20)
6. `Gate` 아래: `PostLeft` Cylinder (-3.5, 2.5, -20) 스케일 (1, 2.5, 1), `PostRight` Cylinder (3.5, 2.5, -20) 같은 스케일, `Lintel` Cube (0, 5.2, -20) 스케일 (8, 0.6, 1). `Wood.mat`, 콜라이더 유지
7. `Road`: Cube, 위치 (0, 0.025, -74), 스케일 (4, 0.05, 132), `Road.mat` (정문 안 z -8 ~ 바깥 z -140)
8. `Trees` 아래 Tree 프리팹 40개: `System.Random(42)`로 x·z를 -120 ~ 120에서 뽑되, 울타리 안팎 여유(|x| < 26 이고 |z| < 26)와 길 통로(|x| < 6 이고 z < -20)는 건너뜀. Y 회전 0~360, 스케일 0.8~1.3
9. `Huts` 아래 Hut 프리팹 2개:
   - `RecruitHall`: 위치 (-10, 0, 8), Y 회전 -45 (문이 -z라 광장 쪽. 135면 모서리 쪽을 봄). Add Component → `InteractPointView` (`_id` = `recruit`, `_label` = Hub/`hub.recruit`)
   - `Storage`: 위치 (10, 0, 10), Y 회전 45 (문이 광장 쪽)
10. `Board` 프리팹: 위치 (6, 0, -4), Y 회전 -90. Add Component → `InteractPointView` (`_id` = `board`, `_label` = Hub/`hub.board`)
11. `Signpost` 프리팹: 위치 (3, 0, -16), Y 회전 0. Add Component → `InteractPointView` (`_id` = `depart`, `_label` = Hub/`hub.depart`)
12. `PlayerSpawn`: 빈 오브젝트, 위치 (0, 0, -12), 회전 (0, 0, 0)
13. `Player` 프리팹: 위치 (0, 0, -12)
14. `Canvas` (HubView 그대로):
    - `PartyText`: 앵커·피벗 (0, 1), 위치 (40, -40), 크기 (500, 60), 왼쪽 정렬. 키를 Hub/`hub.party_count`로 다시 연결
    - `Crosshair` 새로: Image (`UI/Skin/Knob.psd`), 가운데, 크기 (8, 8), 색 흰색 알파 0.8, Raycast Target 끔
    - `InteractPrompt` 새로: LocalizedText 프리팹, 가운데 기준 위치 (0, -80), 크기 (800, 50), 글자 32, 가운데 정렬, Raycast Target 끔, 키 UI/`common.interact_prompt`
15. HubView 연결: `_partyText` → PartyText, `_interactPrompt` → InteractPrompt, `_player` → Player의 PlayerController, `_detector` → Player의 InteractDetector, `_spawn` → PlayerSpawn
16. 저장 → 다시 열어 HubView 칸 5개, 지점 3개의 `_id`·`_label`, PlayerController·InteractDetector 칸이 비지 않았는지 확인

---

## 다국어 변경

1. 시트(`How to Live as a Mage Localization`)에 `Hub` 탭 추가, 머리글 15칸(Key, Comment, ko, en, ja, zh-Hans, zh-Hant, ru, de, fr, es, pt-BR, it, pl, tr):

| Key | Comment | ko | en |
|---|---|---|---|
| `hub.party_count` | 전초기지 파티 인원. {0} 지금 인원, {1} 필요한 인원 | 파티 {0}/{1} | Party {0}/{1} |
| `hub.recruit` | 영입소 안내 이름 (임시) | 용병 영입 (임시) | Recruit (temp) |
| `hub.depart` | 출발 표지판 안내 이름 | 출발 | Depart |
| `hub.party_required` | 용병이 모자랄 때 토스트. {0} 필요한 인원 | 용병 {0}명이 필요해 | You need {0} mercenaries |
| `hub.board` | 게시판 안내 이름 | 게시판 | Quest Board |
| `hub.quest_taken` | 게시판에서 의뢰를 받았을 때 토스트. {0} 의뢰 이름 | 의뢰를 받았어: {0} | Quest accepted: {0} |
| `hub.quest_required` | 의뢰 없이 출발할 때 토스트 | 게시판에서 의뢰를 먼저 받아 | Take a quest from the board first |

2. `UI` 탭: `hub.party_count`·`hub.recruit`·`hub.depart`·`hub.party_required` 줄 지움(지우기 직전 행 번호 다시 확인), 끝에 추가:

| Key | Comment | ko | en |
|---|---|---|---|
| `common.interact_prompt` | 상호작용 안내. {0} 키 이름, {1} 지점 이름 | [{0}] {1} | [{0}] {1} |

3. 두 탭을 키트 CSV 형식으로 `Assets/Localization/CSV/UI.csv`·`Hub.csv`에 씀
4. `LocalizationEditorSettings.CreateStringTableCollection("Hub", "Assets/Localization/Tables", LocalizationEditorSettings.GetLocales())`
5. `Tools > PaulowniaSky > Localization Kit > Import Tables from CSV`
6. UI 컬렉션에서 `hub.*` 4개 제거: `collection.RemoveEntry(키)` 전에 id를 적어 두고, 언어 표마다 `table.RemoveEntry(id)` + `EditorUtility.SetDirty(table)`, `AssetDatabase.SaveAssets()`
7. `Check Missing Translations`·`Check Missing Glyphs`

---

## 주의사항

- `PlayerController`·`InteractDetector`는 Awake에서 스스로 꺼짐. `HubState.Reveal`이 켜야 움직임 (로딩창 밑에서 시점이 돌지 않게)
- `PlaceAt`은 `OnSceneReady`(덮개 밑)에서 부름. CharacterController는 꺼야 위치가 바뀜
- 씬의 `PartyText`는 UI 테이블 키를 가리키고 있어서, UI에서 `hub.*`를 지운 뒤 반드시 Hub 테이블로 다시 연결
- 플레이어 `Visual` 도형의 콜라이더를 지워야 함. 남으면 레이캐스트·이동이 자기 몸에 걸림
- Outpost 씬엔 카메라가 Player 프리팹 하나뿐이어야 함 (AudioListener 2개 경고 방지)
- 빌더에서 에셋(머티리얼·메시·프리팹)은 씬을 연 뒤 쓰기 직전에 읽음
- `LocalizedText`의 키·테이블은 SerializedObject로 `m_StringReference.m_TableReference.m_TableCollectionName`(= `GUID:{Shared Data GUID}`)·`m_TableEntryReference.m_KeyId`에 넣음
- `InteractPointView._label`도 같은 방식 (`_label.m_TableReference…`, `_label.m_TableEntryReference.m_KeyId`)
- Play 검증은 사용자가 함. 컴파일 에러·경고 확인과 씬 다시 열기 확인까지만

---

## 진행 추적

- [x] 묶음 Logic — PlayerController·InteractDetector·InteractPointView 새로, HubView·HubState 교체, 컴파일 에러·경고 0
- [x] 묶음 Editor-Localization — 시트 Hub 탭·UI 탭 정리, CSV 2개, Hub 테이블 만들기·Import, UI의 hub.* 제거, 누락 검사
- [x] 묶음 Prefab — 원뿔 메시, 머티리얼 6개, 프리팹 6개 (Player·Tree·Hut·Tower·Board·Signpost)
- [x] 묶음 Editor-Scene — Outpost 씬 배치·HubView 연결, 저장 뒤 다시 열어 확인
- [ ] 묶음 Verify — 사용자 확인 목록 전달

---

## 확인 목록 (사용자, 에디터 Play)

- 새 게임 → 생성 → 로딩창에서 키 → 정문 안쪽에서 1인칭, 커서 잠김
- WASD·Shift·마우스 → 걷기·달리기·시점, 울타리·나무·오두막에 막힘
- 영입소 오두막을 바라봄 → "[E] 용병 영입 (임시)", E → 왼쪽 위 "파티 2/2"
- 의뢰 없이 출발 표지판 E → "게시판에서 의뢰를 먼저 받아"
- 게시판 E → "의뢰를 받았어: 숲 외곽 정리 (임시)"
- 출발 표지판 E → 숲, 숲에서 E → 다시 정문 안쪽
- Esc → 일시정지 메뉴, 시점·이동 멈춤, 커서 보임. 계속하기 → 다시 잠기고 움직임
- 설정 창에서 한국어 → 파티 수·안내 글 한국어
- Outpost 씬에서 바로 Play → 디버그 판(파티 2·의뢰 있음)이라 출발 표지판 E로 바로 숲
- 콘솔 에러·경고 없음

---

## 커밋 메시지

```
Add — 전초기지 도형 필드, 1인칭 플레이어, E 상호작용 지점

- Outpost 씬: 바닥·울타리·모서리 망루·정문·길·나무 40·오두막 2·게시판·출발 표지판 (원기둥·원뿔·상자 도형, 프리팹 Visual 자식 + 루트 콜라이더), 해
- Player 프리팹: CharacterController + PlayerController(이동·달리기·마우스 시점·중력) + InteractDetector(바라본 지점 2.5m, E), 머리에 카메라
- InteractPointView(지점 id·안내 이름): 영입소(임시 영입)·게시판(임시 의뢰 받기)·출발 표지판(파티 2명 + 의뢰 → 숲)
- HubState: 버튼 대신 E 지점, Reveal에서 커서 잠금·조작 켬, 화면 가운데 조준점·"[E] 이름" 안내
- 다국어: Hub 테이블 새로 (UI의 hub.* 4개 옮김 + board·quest_taken·quest_required), UI에 common.interact_prompt
- 원뿔 메시·회색 박스 머티리얼 6개 (Assets/Art/Placeholder)

Why: 역마차 오프닝(3-2)과 첫 만남 툴팁(3-3)이 올라갈 전초기지 필드와 1인칭 조작이 먼저 필요함
Alternatives: Rigidbody 이동 (미끄러짐·흔들림 손볼 게 많음), 가까운 지점 자동 선택 (여러 개면 헷갈림), ProBuilder 원뿔 (설치 필요), 출발이 의뢰까지 계속 설정 (게시판이 빈 지점이 됨)
```
