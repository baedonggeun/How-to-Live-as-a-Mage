using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>현장 시전: 좌클릭하면 시전 상태 → 화면 가운데 그리기 칸에 펜이 생기고 커서를 잠근 채 마우스 이동량으로 그림 (그리기 칸 SpellDrawController에 가상 펜으로 넘김) → 대기·Space 판정 → 3초 조준(시점만, 좌클릭이나 3초 끝 발사, 우클릭 취소) → 효과. 방호막은 조준 없이 바로. 마나(초당 9 회복)·방호막 재사용, 만드는 룬 혼자 던지기, 생략 없이 처음 그린 다중 룬 마법 등록. 그리는 동안 이동·시점, 조준 중 이동을 막음. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
    public sealed class FieldCastController : MonoBehaviour
    {
        /// <summary>그리기·발사 입력 액션 경로 (전역 액션, 왼쪽 버튼)</summary>
        const string DrawActionPath = "Player/Attack";

        /// <summary>취소 입력 액션 경로 (전역 액션, 오른쪽 버튼)</summary>
        const string CancelActionPath = "Player/CancelCast";

        /// <summary>펜을 움직일 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>현장 시전 튜토리얼 id (처음 판정까지 그리면 배움)</summary>
        const string FieldCastTutorialId = "field_cast";

        /// <summary>판정 뒤 조준 시간 (초, v1)</summary>
        const float AimSeconds = 3f;

        /// <summary>최대 마나 (v1: 100 + 마나 × 8, 마나 0. 임시)</summary>
        const float ManaMaxValue = 100f;

        /// <summary>초당 마나 회복 (v1)</summary>
        const float ManaRegen = 9f;

        /// <summary>조준 보정: 앞쪽 최대 거리 (m, v1)</summary>
        const float AimMaxDistance = 60f;

        /// <summary>조준 보정: 시선에서 최대 가로 거리 (m, v1)</summary>
        const float AimMaxOffset = 3.5f;

        /// <summary>주문 탄 속도 (m/s, v1)</summary>
        const float ProjectileSpeed = 34f;

        /// <summary>주문 탄 최대 비행 시간 (초, v1)</summary>
        const float ProjectileLife = 1.9f;

        /// <summary>탄이 눈 앞 이만큼에서 나감 (m, v1)</summary>
        const float MuzzleOffset = 0.7f;

        /// <summary>화염 벽 조준점 최대 거리 (m, v1)</summary>
        const float WallMaxDistance = 40f;

        /// <summary>판정 글·그린 획이 남는 시간 (초, v1)</summary>
        const float ClearSeconds = 1.4f;

        /// <summary>알림 글이 남는 시간 (초)</summary>
        const float NoteSeconds = 1.6f;

        /// <summary>조준 글 키 ({0} 이름, {1} 일치율 %, {2} 남은 초)</summary>
        const string AimKey = "field.aim";

        /// <summary>정확 조준 글 키 (같은 자리 값)</summary>
        const string AimExactKey = "field.aim_exact";

        /// <summary>알림: 마나 모자람</summary>
        const string NoManaKey = "field.note.no_mana";

        /// <summary>알림: 방호막 재사용 대기 ({0} 남은 초)</summary>
        const string ShieldCooldownKey = "field.note.shield_cooldown";

        /// <summary>알림: 두루마리는 현장에서 안 만듦</summary>
        const string ScrollKey = "field.note.scroll";

        /// <summary>알림: 혼자서는 효과 없는 룬 ({0} 룬 주문 이름)</summary>
        const string RuneAloneKey = "field.note.rune_alone";

        /// <summary>알림: 현장 효과가 아직 없는 주문 ({0} 주문 이름)</summary>
        const string NoEffectKey = "field.note.no_effect";

        /// <summary>알림: 조준 취소</summary>
        const string CanceledKey = "field.note.canceled";

        /// <summary>룬 주문 이름 키 (Codex 테이블, {0} 룬 이름)</summary>
        const string RuneSpellKey = "codex.rune_spell";

        /// <summary>마법 등록 토스트 키 (Codex 테이블, {0} 마법 이름)</summary>
        const string RegisteredKey = "codex.toast.registered";

        /// <summary>숲 문구 테이블</summary>
        static readonly TableReference Table = "Field";

        /// <summary>코덱스 문구 테이블 (룬 주문 이름·등록 토스트)</summary>
        static readonly TableReference CodexTable = "Codex";

        /// <summary>불 탄 색</summary>
        static readonly Color FireColor = new(1f, 0.55f, 0.15f);

        /// <summary>물 탄 색</summary>
        static readonly Color WaterColor = new(0.35f, 0.65f, 1f);

        /// <summary>시전 상태</summary>
        enum CastState
        {
            /// <summary>Init 전</summary>
            Off,

            /// <summary>그리지도 겨누지도 않음</summary>
            Idle,

            /// <summary>그리기 칸에 그리는 중 (판정 전)</summary>
            Drawing,

            /// <summary>판정 뒤 3초 조준</summary>
            Aiming,
        }

        /// <summary>그리기 칸 (HUD의 SpellDrawController. 펜 좌표는 이 칸 가운데 기준. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellDrawController _draw;

        /// <summary>숲 HUD (조준점·조준 원·조준 글·알림 글·펜 점. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldHudView _hud;

        /// <summary>겨누는 눈 (플레이어 Head. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _eye;

        /// <summary>주문 탄 프리팹 (SpellProjectile.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellProjectileController _projectilePrefab;

        /// <summary>화염 벽 프리팹 (FlameWall.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] FlameWallController _wallPrefab;

        /// <summary>펜 감도 (마우스 이동량 1당 그리기 칸 단위, 캔버스 배율로 나눔)</summary>
        [SerializeField, Min(0.1f)] float _penSpeed = 1f;

        /// <summary>익힌 룬 id (비교 그림을 만들 때 채움)</summary>
        readonly HashSet<string> _learnedIds = new(StringComparer.Ordinal);

        /// <summary>판정 비교 그림 (등록이 바뀌면 다시 만듦)</summary>
        List<SpellCandidateEntry> _candidates = new();

        /// <summary>판정 비교 그림 원본 (다중 룬 마법 순서 뒤에 룬 순서. ShapeIndex가 이 번호)</summary>
        SpellShapeEntry[] _shapes;

        /// <summary>다중 룬 마법 전부 (DataManager 순서)</summary>
        IReadOnlyList<SpellSO> _spells;

        /// <summary>룬 전부 (DataManager 순서)</summary>
        IReadOnlyList<RuneSO> _runes;

        /// <summary>이번 판 데이터</summary>
        RunEntry _run;

        /// <summary>현장 규칙</summary>
        FieldService _service;

        /// <summary>1인칭 조작 (잠금)</summary>
        PlayerController _player;

        /// <summary>마법사 몸 (방호막·피해 출처)</summary>
        FieldUnitController _unit;

        /// <summary>그리기 칸이 든 캔버스 (펜 감도를 화면 크기와 맞춤)</summary>
        Canvas _canvas;

        /// <summary>그리기·발사 입력</summary>
        InputAction _drawAction;

        /// <summary>취소 입력</summary>
        InputAction _cancelAction;

        /// <summary>펜 입력 (마우스 이동량)</summary>
        InputAction _lookAction;

        /// <summary>지금 시전 상태</summary>
        CastState _state;

        /// <summary>펜 자리 (그리기 칸 가운데 기준)</summary>
        Vector2 _pen;

        /// <summary>펜을 누르고 있는지</summary>
        bool _penDown;

        /// <summary>조준 중인 주문</summary>
        FieldCastSpec _aim;

        /// <summary>조준 남은 시간 (초)</summary>
        float _aimLeft;

        /// <summary>조준 글에 마지막으로 넣은 남은 시간 (0.1초 단위, 바뀔 때만 글을 다시 씀)</summary>
        int _aimShown = -1;

        /// <summary>판정 글·획을 지울 때까지 (초, 0이면 안 지움)</summary>
        float _clearLeft;

        /// <summary>알림 글을 숨길 때까지 (초)</summary>
        float _noteLeft;

        /// <summary>방호막 남은 시간 (초)</summary>
        float _shieldLeft;

        /// <summary>방호막을 다시 펼칠 수 있을 때까지 (초)</summary>
        float _shieldCooldown;

        /// <summary>지금 마나</summary>
        public float Mana { get; private set; }

        /// <summary>최대 마나</summary>
        public float ManaMax => ManaMaxValue;

        #region Awake - 연결 확인, 입력 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_draw == null || _hud == null || _eye == null || _projectilePrefab == null || _wallPrefab == null)
                Debug.LogError("[Cast] 그리기 칸·HUD·눈·탄·벽 연결 안 됨", this);
            _drawAction = FindAction(DrawActionPath);
            _cancelAction = FindAction(CancelActionPath);
            _lookAction = FindAction(LookActionPath);
            if (_draw != null) _canvas = _draw.GetComponentInParent<Canvas>();
            enabled = false;
        }
        #endregion

        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[Cast] {path} 액션 없음");
            return action;
        }
        #endregion

        #region Init - 판 데이터·주문·룬·현장 규칙·조작·몸을 받아 비교 그림을 만들고 마나를 채움, 그리기 칸에 판정을 넘김, HUD 조준 표시를 끔
        /// <param name="run">이번 판 데이터</param>
        /// <param name="spells">다중 룬 마법 전부</param>
        /// <param name="runes">룬 전부</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="player">1인칭 조작</param>
        /// <param name="unit">마법사 몸</param>
        public void Init(RunEntry run, IReadOnlyList<SpellSO> spells, IReadOnlyList<RuneSO> runes, FieldService service, PlayerController player, FieldUnitController unit)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _runes = runes ?? throw new ArgumentNullException(nameof(runes));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _unit = unit != null ? unit : throw new ArgumentNullException(nameof(unit));
            _shapes = spells.Select(spell => spell != null ? spell.Shape : null)
                .Concat(runes.Select(rune => rune != null ? rune.Shape : null)).ToArray();
            RebuildCandidates();
            Mana = ManaMax;
            _state = CastState.Idle;
            if (_draw != null) _draw.Begin(NameAt, Judge, OnJudged);
            if (_hud == null) return;
            _hud.AimRing.SetActive(false);
            _hud.AimText.gameObject.SetActive(false);
            _hud.NoteText.gameObject.SetActive(false);
            _hud.Pen.gameObject.SetActive(false);
            _hud.Crosshair.SetActive(true);
        }
        #endregion

        #region OnEnable - 그리기·취소 입력 구독
        void OnEnable()
        {
            if (_drawAction != null)
            {
                _drawAction.performed += OnDrawPressed;
                _drawAction.canceled += OnDrawReleased;
            }
            if (_cancelAction != null) _cancelAction.performed += OnCancelPressed;
        }
        #endregion

        #region OnDisable - 입력 구독 해제, 그리던 것·겨누던 것을 그만둠 (상태가 끄거나 마법사가 죽을 때)
        void OnDisable()
        {
            if (_drawAction != null)
            {
                _drawAction.performed -= OnDrawPressed;
                _drawAction.canceled -= OnDrawReleased;
            }
            if (_cancelAction != null) _cancelAction.performed -= OnCancelPressed;
            if (_state == CastState.Drawing && _draw != null) _draw.CancelCast();
            else if (_state == CastState.Aiming) EndCast();
        }
        #endregion

        #region Update - 멈췄으면 안 함. 마나 회복, 방호막, 알림 글·판정 글 지우기, 그리는 중엔 펜, 조준 중엔 남은 시간
        void Update()
        {
            if (_state == CastState.Off) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Mana = Mathf.Min(ManaMax, Mana + ManaRegen * dt);
            UpdateShield(dt);
            if (_noteLeft > 0f)
            {
                _noteLeft -= dt;
                if (_noteLeft <= 0f) _hud.NoteText.gameObject.SetActive(false);
            }
            if (_clearLeft > 0f && _state != CastState.Drawing)
            {
                _clearLeft -= dt;
                if (_clearLeft <= 0f) _draw.Begin(NameAt, Judge, OnJudged);
            }
            if (_state == CastState.Drawing) UpdatePen();
            else if (_state == CastState.Aiming) UpdateAim(dt);
        }
        #endregion

        #region OnDrawPressed - 좌클릭: 조준 중이면 발사, 아니면 (시전 상태가 아니면 시작하고) 펜을 누름
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnDrawPressed(InputAction.CallbackContext context)
        {
            if (_state == CastState.Aiming)
            {
                Fire();
                return;
            }
            if (_state == CastState.Idle) StartDrawing();
            if (_state != CastState.Drawing) return;
            _penDown = true;
            _draw.PenDown(_pen);
        }
        #endregion

        #region OnDrawReleased - 좌클릭을 뗌: 펜을 뗌 (그 뒤 대기 시간이 흐름)
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnDrawReleased(InputAction.CallbackContext context)
        {
            if (_state != CastState.Drawing || !_penDown) return;
            _penDown = false;
            _draw.PenUp();
        }
        #endregion

        #region OnCancelPressed - 우클릭: 그리는 중이면 취소(판정 칸이 '직접 취소'로 끝냄), 조준 중이면 조준 취소
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (_state == CastState.Drawing)
            {
                _draw.CancelCast();
                return;
            }
            if (_state != CastState.Aiming) return;
            Note(CanceledKey);
            EndCast();
        }
        #endregion

        #region StartDrawing - 시전 상태 시작: 이동·시점 잠금, 펜을 가운데에, 펜 점을 켜고 조준점을 끔
        void StartDrawing()
        {
            _state = CastState.Drawing;
            _clearLeft = 0f;
            _pen = Vector2.zero;
            _player.MoveLocked = true;
            _player.LookLocked = true;
            _hud.Crosshair.SetActive(false);
            _hud.Pen.gameObject.SetActive(true);
            _hud.Pen.anchoredPosition = _pen;
        }
        #endregion

        #region UpdatePen - 마우스 이동량으로 펜을 옮기고 그리기 칸 안(반지름 = 짧은 변 절반)으로 자름. 누르고 있으면 획에 점을 더함
        void UpdatePen()
        {
            // 캔버스 배율 (화면 픽셀 → 칸 단위)
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            // 이번 프레임 펜 이동 (칸 단위)
            Vector2 delta = _lookAction != null ? _lookAction.ReadValue<Vector2>() * (_penSpeed / scale) : Vector2.zero;
            // 그리기 칸 크기
            Rect area = ((RectTransform)_draw.transform).rect;
            _pen = Vector2.ClampMagnitude(_pen + delta, Mathf.Min(area.width, area.height) * 0.5f);
            _hud.Pen.anchoredPosition = _pen;
            if (_penDown) _draw.PenMove(_pen);
        }
        #endregion

        #region OnJudged - 판정 뒤: 펜 점을 끄고 판정 글을 잠시 뒤 지우게 함, 직접 취소가 아니면 현장 시전 배움. 시전이면 주문 값을 만들어 막을 이유가 없으면 방호막은 바로, 나머지는 3초 조준
        /// <param name="spec">판정 결과</param>
        void OnJudged(SpellJudgeSpec spec)
        {
            _penDown = false;
            _hud.Pen.gameObject.SetActive(false);
            _clearLeft = ClearSeconds;
            if (spec.Reason != SpellJudgeReason.Canceled) TutorialManager.Instance.MarkLearned(FieldCastTutorialId);
            if (!spec.IsCast || !TryMakeCast(spec, out FieldCastSpec cast) || !CanCast(cast))
            {
                EndCast();
                return;
            }
            if (cast.Effect == SpellFieldEffect.Shield)
            {
                Cast(cast);
                EndCast();
                return;
            }
            _aim = cast;
            _aimLeft = AimSeconds;
            _aimShown = -1;
            _state = CastState.Aiming;
            _player.LookLocked = false;
            _hud.AimRing.SetActive(true);
            RefreshAimText();
        }
        #endregion

        #region TryMakeCast - 판정한 그림으로 현장 값을 만듦. 두루마리·현장 효과 없는 주문·혼자 효과 없는 룬이면 알림 후 false. 다중 룬 마법을 생략 없이 그렸으면 등록
        /// <param name="spec">시전 판정</param>
        /// <param name="cast">만든 현장 값</param>
        bool TryMakeCast(SpellJudgeSpec spec, out FieldCastSpec cast)
        {
            cast = default;
            // 판정한 그림 번호
            int index = spec.Candidate.ShapeIndex;
            // 이름 (지금 언어)
            string name = NameAt(index);
            if (index >= 0 && index < _spells.Count)
            {
                // 그린 다중 룬 마법
                SpellSO spell = _spells[index];
                if (spell == null) return false;
                if (spell.Kind == SpellKind.Scroll)
                {
                    Note(ScrollKey);
                    return false;
                }
                if (spec.Candidate.Omit.Count == 0) Register(spell);
                if (spell.FieldEffect == SpellFieldEffect.None)
                {
                    Note(NoEffectKey, name);
                    return false;
                }
                cast = FieldCastSpec.FromSpell(spell, name, spec.Accuracy);
                return true;
            }
            // 룬 번호
            int runeIndex = index - _spells.Count;
            // 그린 룬
            RuneSO rune = runeIndex >= 0 && runeIndex < _runes.Count ? _runes[runeIndex] : null;
            if (rune == null) return false;
            if (rune.SoloEffect == SpellFieldEffect.None)
            {
                Note(RuneAloneKey, name);
                return false;
            }
            cast = FieldCastSpec.FromRune(rune, name, spec.Accuracy);
            return true;
        }
        #endregion

        #region CanCast - 방호막 재사용 대기·마나가 모자라면 알림 후 false
        /// <param name="cast">쓸 주문</param>
        bool CanCast(FieldCastSpec cast)
        {
            if (cast.Effect == SpellFieldEffect.Shield && _shieldCooldown > 0f)
            {
                Note(ShieldCooldownKey, Mathf.CeilToInt(_shieldCooldown));
                return false;
            }
            if (Mana >= cast.ManaCost) return true;
            Note(NoManaKey);
            return false;
        }
        #endregion

        #region UpdateAim - 조준 남은 시간을 셈. 다 되면 발사, 아니면 조준 글 갱신
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateAim(float dt)
        {
            _aimLeft -= dt;
            if (_aimLeft <= 0f)
            {
                Fire();
                return;
            }
            RefreshAimText();
        }
        #endregion

        #region RefreshAimText - 조준 글 "이름 일치율% · 남은 초 · 좌클릭 시전 · 우클릭 취소" (0.1초가 바뀔 때만 다시 씀, 정확이면 정확 표시)
        void RefreshAimText()
        {
            // 보일 남은 시간 (0.1초 단위)
            int tenths = Mathf.CeilToInt(Mathf.Max(0f, _aimLeft) * 10f);
            if (tenths == _aimShown) return;
            _aimShown = tenths;
            // 조준 글의 String Reference
            var text = _hud.AimText.StringReference;
            text.SetReference(Table, _aim.IsExact ? AimExactKey : AimKey);
            text.Arguments = new object[] { _aim.Name, Percent(_aim.Accuracy), (tenths / 10f).ToString("F1", CultureInfo.InvariantCulture) };
            _hud.AimText.gameObject.SetActive(true);
            _hud.AimText.RefreshString();
        }
        #endregion

        #region Fire - 조준을 끝내고, 마나·재사용을 다시 보고 되면 시전
        void Fire()
        {
            if (_state != CastState.Aiming) return;
            // 쏠 주문 (EndCast가 상태를 바꾸기 전에 꺼냄)
            FieldCastSpec cast = _aim;
            EndCast();
            if (CanCast(cast)) Cast(cast);
        }
        #endregion

        #region EndCast - 시전 상태를 끝냄: 이동·시점 잠금 해제, 조준 원·글·펜 점 끄고 조준점 켬
        void EndCast()
        {
            _state = _state == CastState.Off ? CastState.Off : CastState.Idle;
            _penDown = false;
            if (_player != null)
            {
                _player.MoveLocked = false;
                _player.LookLocked = false;
            }
            if (_hud == null) return;
            _hud.AimRing.SetActive(false);
            _hud.AimText.gameObject.SetActive(false);
            _hud.Pen.gameObject.SetActive(false);
            _hud.Crosshair.SetActive(true);
        }
        #endregion

        #region Cast - 마나를 쓰고 효과를 냄 (정확이면 위력 ×1.4): 탄·폭발 탄·물탄·부채꼴 세 발·화염 벽·방호막
        /// <param name="cast">쓸 주문</param>
        void Cast(FieldCastSpec cast)
        {
            Mana -= cast.ManaCost;
            // 정확 배율을 곱한 위력
            float power = cast.Power * cast.Scale;
            switch (cast.Effect)
            {
                case SpellFieldEffect.Bolt:
                    Shoot(AimDirection(), power, 0f, 0f, FireColor);
                    break;
                case SpellFieldEffect.Ball:
                    Shoot(AimDirection(), power, cast.Area, 0f, FireColor);
                    break;
                case SpellFieldEffect.Water:
                    Shoot(AimDirection(), power, 0f, cast.Area, WaterColor);
                    break;
                case SpellFieldEffect.Fan:
                    // 가운데 방향
                    Vector3 direction = AimDirection();
                    Shoot(Quaternion.AngleAxis(-cast.Area, Vector3.up) * direction, power, 0f, 0f, FireColor);
                    Shoot(direction, power, 0f, 0f, FireColor);
                    Shoot(Quaternion.AngleAxis(cast.Area, Vector3.up) * direction, power, 0f, 0f, FireColor);
                    break;
                case SpellFieldEffect.Wall:
                    PlaceWall(cast, power);
                    break;
                case SpellFieldEffect.Shield:
                    _shieldLeft = cast.Duration * cast.Scale;
                    _shieldCooldown = cast.Cooldown;
                    _unit.DamageScale = cast.Power;
                    break;
            }
        }
        #endregion

        #region AimDirection - 조준 보정 대상(시선 60m 안·가로 3.5m 안 적)이 있으면 그 겨눌 점으로, 없으면 시선 방향 (v1)
        Vector3 AimDirection()
        {
            // 시선
            var ray = new Ray(_eye.position, _eye.forward);
            // 보정 대상 (없으면 null)
            FieldUnitController target = _service.AimTarget(ray, AimMaxDistance, AimMaxOffset);
            return target != null ? (target.AimPoint - _eye.position).normalized : _eye.forward;
        }
        #endregion

        #region Shoot - 눈 앞에서 탄 한 발을 쏨 (적 편을 노림)
        /// <param name="direction">방향</param>
        /// <param name="damage">피해</param>
        /// <param name="splash">폭발 반지름 (m)</param>
        /// <param name="push">밀쳐내기 (m)</param>
        /// <param name="color">색</param>
        void Shoot(Vector3 direction, float damage, float splash, float push, Color color)
        {
            // 탄이 나갈 곳
            Vector3 from = _eye.position + direction * MuzzleOffset;
            // 쏜 탄
            SpellProjectileController shot = Instantiate(_projectilePrefab, from, Quaternion.LookRotation(direction));
            shot.Launch(direction, ProjectileSpeed, ProjectileLife, damage, splash, push, FieldTeam.Enemy, _unit, _service, color);
        }
        #endregion

        #region PlaceWall - 시선이 40m 안에서 닿은 곳(적이면 그 발밑, 없으면 앞 40m 땅)에 시선과 수직인 불벽
        /// <param name="cast">화염 벽 주문</param>
        /// <param name="damagePerSecond">초당 피해 (정확 배율 적용)</param>
        void PlaceWall(FieldCastSpec cast, float damagePerSecond)
        {
            // 시선
            var ray = new Ray(_eye.position, _eye.forward);
            // 시선의 평면 앞 방향
            Vector3 flatForward = Vector3.ProjectOnPlane(_eye.forward, Vector3.up).normalized;
            // 벽 가운데 (기본: 앞 40m 마법사 발 높이)
            Vector3 center = _unit.transform.position + flatForward * WallMaxDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, WallMaxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                // 닿은 몸 (몸이 아니면 null)
                FieldUnitController unit = hit.collider.GetComponentInParent<FieldUnitController>();
                center = unit != null ? unit.transform.position : hit.point;
            }
            // 세운 벽
            FlameWallController wall = Instantiate(_wallPrefab, center, Quaternion.identity);
            wall.Launch(center, _eye.right, cast.Area, cast.Duration, damagePerSecond, _unit, _service);
        }
        #endregion

        #region UpdateShield - 방호막 재사용 대기를 줄이고, 지속이 끝나면 받는 피해 배율을 되돌림
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateShield(float dt)
        {
            if (_shieldCooldown > 0f) _shieldCooldown -= dt;
            if (_shieldLeft <= 0f) return;
            _shieldLeft -= dt;
            if (_shieldLeft <= 0f) _unit.DamageScale = 1f;
        }
        #endregion

        #region Note - 조준점 아래 알림 글을 잠깐 띄움 (숲 테이블)
        /// <param name="key">문구 키</param>
        /// <param name="args">자리 값</param>
        void Note(string key, params object[] args)
        {
            // 알림 글의 String Reference
            var text = _hud.NoteText.StringReference;
            text.SetReference(Table, key);
            text.Arguments = args;
            _hud.NoteText.gameObject.SetActive(true);
            _hud.NoteText.RefreshString();
            _noteLeft = NoteSeconds;
        }
        #endregion

        #region Register - 등록 안 한 다중 룬 마법이면 등록하고 토스트, 비교 그림을 다시 만듦 (생략본이 생김)
        /// <param name="spell">생략 없이 그린 마법</param>
        void Register(SpellSO spell)
        {
            if (_run.CollectedSpells.Contains(spell)) return;
            _run.CollectedSpells.Add(spell);
            UIManager.Instance.ShowToast(CodexTable, RegisteredKey, spell.Name.GetLocalizedString());
            RebuildCandidates();
        }
        #endregion

        #region RebuildCandidates - 익힌 룬·등록한 마법·생략 가능 수로 판정 비교 그림을 다시 만듦 (코덱스와 같은 방식)
        void RebuildCandidates()
        {
            _learnedIds.Clear();
            foreach (RuneSO rune in _run.LearnedRunes)
                if (rune != null) _learnedIds.Add(rune.Id);
            // 등록한 마법 id
            var collected = new HashSet<string>(StringComparer.Ordinal);
            foreach (SpellSO spell in _run.CollectedSpells)
                if (spell != null) collected.Add(spell.Id);
            _candidates = SpellRecognitionCalculator.BuildCandidates(_shapes, _learnedIds, collected, _run.OmitLimit);
        }
        #endregion

        #region Judge - 그린 획 판정 (기준은 기본값, 임시)
        /// <param name="strokes">그린 획</param>
        SpellJudgeSpec Judge(IReadOnlyList<Vector2[]> strokes) =>
            SpellRecognitionCalculator.Judge(strokes, _candidates, _learnedIds, SpellRecognitionCalculator.DefaultPartMin, SpellRecognitionCalculator.DefaultAllMin);
        #endregion

        #region NameAt - 판정 그림 번호의 이름 (지금 언어): 다중 룬 마법 이름, 그 뒤 번호는 '불 룬 주문'
        /// <param name="index">판정 ShapeIndex</param>
        string NameAt(int index)
        {
            if (_spells == null || _runes == null || index < 0) return string.Empty;
            if (index < _spells.Count) return _spells[index] != null ? _spells[index].Name.GetLocalizedString() : string.Empty;
            // 룬 번호
            int rune = index - _spells.Count;
            if (rune >= _runes.Count || _runes[rune] == null) return string.Empty;
            return new LocalizedString(CodexTable, RuneSpellKey).GetLocalizedString(_runes[rune].Name.GetLocalizedString());
        }
        #endregion

        #region Percent - 0~1을 % 정수로
        /// <param name="value">0~1 값</param>
        static int Percent(float value) => Mathf.RoundToInt(value * 100f);
        #endregion
    }
}
