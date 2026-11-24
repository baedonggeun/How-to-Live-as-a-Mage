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
    /// <summary>현장 시전 (숲): 그리기(F 꾹 → 양피지 → 선 → F 떼면 판정, 우클릭은 취소·갈무리)는 주문 그리기 패드(SpellDrawController)가 하고, 여기서는 패드 결과를 받아 처리함. 성공(성공 사운드) → 방호막은 바로, 나머지는 3초 조준(시점만, 좌클릭이나 3초 끝 발사, 우클릭은 취소·갈무리) → 효과(투사체·불벽·방호막, 효과 프리팹이 있으면 입힘). 실패(맞는 마법이 아님)는 실패 사운드·피격·체력·마나·스태미나 감소. 마나(초당 9 회복)·방호막 재사용, 만드는 룬 혼자 던지기, 생략 없이 처음 그린 다중 룬 마법 등록. 그리는 동안 이동·시점, 조준·갈무리 중 이동(갈무리는 시점·상호작용까지)을 막음. 처음엔 꺼져 있고 켜고 끄는 건 상태가 함</summary>
    public sealed class FieldCastController : MonoBehaviour
    {
        /// <summary>발사 입력 액션 경로 (전역 액션, 왼쪽 버튼. 조준 중에만)</summary>
        const string FireActionPath = "Player/Attack";

        /// <summary>조준 취소 입력 액션 경로 (전역 액션, 오른쪽 버튼. 조준 중에만)</summary>
        const string CancelActionPath = "Player/CancelCast";

        /// <summary>판정 뒤 조준 시간 (초, v1)</summary>
        const float AimSeconds = 3f;

        /// <summary>최대 마나 (v1: 100 + 마나 × 8, 마나 0. 임시)</summary>
        const float ManaMaxValue = 100f;

        /// <summary>초당 마나 회복 (v1)</summary>
        const float ManaRegen = 9f;

        /// <summary>시전 실패로 깎는 체력 (임시)</summary>
        const float FailHp = 10f;

        /// <summary>시전 실패로 깎는 마나 (임시)</summary>
        const float FailMana = 15f;

        /// <summary>시전 실패로 깎는 스태미나 (임시)</summary>
        const float FailStamina = 30f;

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

            /// <summary>패드에서 그리는 중 (F를 누르고 있음)</summary>
            Drawing,

            /// <summary>판정 뒤 3초 조준</summary>
            Aiming,
        }

        /// <summary>주문 그리기 패드 (HUD의 Draw. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellDrawController _draw;

        /// <summary>숲 HUD (조준점·조준 원·조준 글·알림 글·방호막 화면. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldHudView _hud;

        /// <summary>겨누는 눈 (플레이어 Head. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _eye;

        /// <summary>주문 탄 프리팹 (SpellProjectile.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellProjectileController _projectilePrefab;

        /// <summary>화염 벽 프리팹 (FlameWall.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] FlameWallController _wallPrefab;

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

        /// <summary>1인칭 조작 (잠금·스태미나)</summary>
        PlayerController _player;

        /// <summary>마법사 몸 (방호막·피해 출처·실패 피해)</summary>
        FieldUnitController _unit;

        /// <summary>바라본 지점 찾기 (갈무리 동안 꺼서 상호작용도 못 하게)</summary>
        InteractDetector _detector;

        /// <summary>발사 입력</summary>
        InputAction _fireAction;

        /// <summary>조준 취소 입력</summary>
        InputAction _cancelAction;

        /// <summary>지금 시전 상태</summary>
        CastState _state;

        /// <summary>조준 중인 주문</summary>
        FieldCastSpec _aim;

        /// <summary>조준 남은 시간 (초)</summary>
        float _aimLeft;

        /// <summary>조준 글에 마지막으로 넣은 남은 시간 (0.1초 단위, 바뀔 때만 글을 다시 씀)</summary>
        int _aimShown = -1;

        /// <summary>알림 글을 숨길 때까지 (초)</summary>
        float _noteLeft;

        /// <summary>방호막 남은 시간 (초)</summary>
        float _shieldLeft;

        /// <summary>방호막을 다시 펼칠 수 있을 때까지 (초)</summary>
        float _shieldCooldown;

        /// <summary>방호막에 입힌 효과 프리팹 (없으면 null)</summary>
        GameObject _shieldFx;

        /// <summary>지금 마나</summary>
        public float Mana { get; private set; }

        /// <summary>최대 마나</summary>
        public float ManaMax => ManaMaxValue;

        #region Awake - 연결 확인, 입력 액션 찾기, 처음엔 꺼 둠 (Reveal에서 상태가 켬)
        void Awake()
        {
            if (_draw == null || _hud == null || _eye == null || _projectilePrefab == null || _wallPrefab == null)
                Debug.LogError("[Cast] 그리기 패드·HUD·눈·탄·벽 연결 안 됨", this);
            _fireAction = FindAction(FireActionPath);
            _cancelAction = FindAction(CancelActionPath);
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

        #region Init - 판 데이터·주문·룬·현장 규칙·조작·몸·상호작용 감지기를 받아 비교 그림을 만들고 마나를 채움, HUD 조준·방호막 표시를 끔
        /// <param name="run">이번 판 데이터</param>
        /// <param name="spells">다중 룬 마법 전부</param>
        /// <param name="runes">룬 전부</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="player">1인칭 조작</param>
        /// <param name="unit">마법사 몸</param>
        /// <param name="detector">바라본 지점 찾기</param>
        public void Init(RunEntry run, IReadOnlyList<SpellSO> spells, IReadOnlyList<RuneSO> runes, FieldService service, PlayerController player, FieldUnitController unit, InteractDetector detector)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _runes = runes ?? throw new ArgumentNullException(nameof(runes));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _unit = unit != null ? unit : throw new ArgumentNullException(nameof(unit));
            _detector = detector != null ? detector : throw new ArgumentNullException(nameof(detector));
            _shapes = spells.Select(spell => spell != null ? spell.Shape : null)
                .Concat(runes.Select(rune => rune != null ? rune.Shape : null)).ToArray();
            RebuildCandidates();
            Mana = ManaMax;
            _state = CastState.Idle;
            if (_hud == null) return;
            _hud.AimRing.SetActive(false);
            _hud.AimText.gameObject.SetActive(false);
            _hud.NoteText.gameObject.SetActive(false);
            _hud.Crosshair.SetActive(true);
            if (_hud.Shield != null) _hud.Shield.SetActive(false);
        }
        #endregion

        #region OnEnable - 발사·취소 입력 구독, 패드에 판정을 넘기고 시작·갈무리 알림 구독
        void OnEnable()
        {
            if (_state == CastState.Off || _draw == null) return;
            if (_fireAction != null) _fireAction.performed += OnFirePressed;
            if (_cancelAction != null) _cancelAction.performed += OnCancelPressed;
            _draw.CastStarted += OnCastStarted;
            _draw.Locked += OnLocked;
            _draw.Unlocked += OnUnlocked;
            _draw.Begin(NameAt, Judge, OnJudged, CanStartCast);
        }
        #endregion

        #region OnDisable - 구독 해제, 패드를 끝내고 (그리던 것·겨누던 것·갈무리도 그만둠) 조작 잠금을 풂 (상태가 끄거나 마법사가 죽을 때)
        void OnDisable()
        {
            if (_fireAction != null) _fireAction.performed -= OnFirePressed;
            if (_cancelAction != null) _cancelAction.performed -= OnCancelPressed;
            if (_draw != null)
            {
                _draw.CastStarted -= OnCastStarted;
                _draw.Locked -= OnLocked;
                _draw.Unlocked -= OnUnlocked;
                _draw.End();
            }
            if (_state == CastState.Aiming) HideAim();
            if (_state != CastState.Off) _state = CastState.Idle;
            if (_player != null) UnlockPlayer();
            HideShield();
        }
        #endregion

        #region Update - 멈췄으면 안 함. 마나 회복, 방호막, 알림 글 지우기, 조준 중엔 남은 시간
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
            if (_state == CastState.Aiming) UpdateAim(dt);
        }
        #endregion

        #region CanStartCast - 패드가 시전을 시작해도 되는지: 그리지도 겨누지도 않을 때만
        bool CanStartCast() => _state == CastState.Idle;
        #endregion

        #region OnCastStarted - 패드가 시전 상태를 시작함: 이동·시점 잠금, 조준점 숨김
        void OnCastStarted()
        {
            _state = CastState.Drawing;
            _player.MoveLocked = true;
            _player.LookLocked = true;
            _hud.Crosshair.SetActive(false);
        }
        #endregion

        #region OnLocked - 갈무리 시작: 이동·시점을 막고 상호작용도 못 하게 감지기를 끔
        /// <param name="seconds">갈무리 초 (안 씀)</param>
        void OnLocked(float seconds)
        {
            _player.MoveLocked = true;
            _player.LookLocked = true;
            _detector.enabled = false;
        }
        #endregion

        #region OnUnlocked - 갈무리 끝: 이동·시점·상호작용을 되돌림
        void OnUnlocked()
        {
            UnlockPlayer();
            _detector.enabled = true;
        }
        #endregion

        #region UnlockPlayer - 이동·시점 잠금을 풂
        void UnlockPlayer()
        {
            _player.MoveLocked = false;
            _player.LookLocked = false;
        }
        #endregion

        #region OnFirePressed - 좌클릭: 조준 중이면 발사
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnFirePressed(InputAction.CallbackContext context)
        {
            if (_state == CastState.Aiming) Fire();
        }
        #endregion

        #region OnCancelPressed - 우클릭: 조준 중이면 조준을 취소하고 그 마법의 룬 수 × 1초 갈무리 (그리는 중 취소는 패드가 함)
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (_state != CastState.Aiming) return;
            // 취소한 마법의 룬 수
            int runes = _aim.Tier;
            HideAim();
            _draw.Recover(runes);
        }
        #endregion

        #region OnJudged - 패드가 판정·취소를 끝냄: 이동·시점 잠금을 풀고 조준점을 되살림. 실패면 벌칙, 맞는 마법이면 값을 만들어 막을 이유가 없으면 성공 사운드 → 방호막은 바로, 나머지는 3초 조준. 그린 것이 없음·직접 취소는 아무 일 없음 (우클릭 갈무리는 패드가 Locked로 따로 알림)
        /// <param name="spec">판정 결과</param>
        void OnJudged(SpellJudgeSpec spec)
        {
            _state = CastState.Idle;
            UnlockPlayer();
            _hud.Crosshair.SetActive(true);
            if (spec.IsFailure)
            {
                Fail();
                return;
            }
            if (!spec.IsCast) return;
            if (!TryMakeCast(spec, out FieldCastSpec cast) || !CanCast(cast)) return;
            PlaySuccess(cast);
            if (cast.Effect == SpellFieldEffect.Shield)
            {
                Cast(cast);
                return;
            }
            StartAim(cast);
        }
        #endregion

        #region Fail - 시전 실패: 실패 사운드, 마나·스태미나를 깎고 체력을 깎음 (깎이면 기존 피격 붉은 번쩍이 저절로 뜸. 체력이 0이 되면 죽음)
        void Fail()
        {
            // 소리를 낼 곳과 데이터 (둘 중 하나라도 없으면 무음)
            AudioManager audio = AudioManager.Instance;
            DataManager data = DataManager.Instance;
            if (audio != null && data != null) audio.Play(data.CastFailSound);
            Mana = Mathf.Max(0f, Mana - FailMana);
            _player.DrainStamina(FailStamina);
            _unit.TakeDamage(FailHp, null);
        }
        #endregion

        #region PlaySuccess - 성공 사운드를 2D로 냄 (소리를 안 넣었으면 조용히 넘어감)
        /// <param name="cast">받아들여진 주문</param>
        void PlaySuccess(FieldCastSpec cast)
        {
            AudioManager audio = AudioManager.Instance;
            if (audio != null) audio.Play(cast.Sound);
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
            // 마법의 룬 수
            int tier = spec.Candidate.Shape.PartRuneIds.Count;
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
                cast = FieldCastSpec.FromSpell(spell, name, spec.Accuracy, tier);
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
            cast = FieldCastSpec.FromRune(rune, name, spec.Accuracy, tier);
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

        #region StartAim - 3초 조준 시작: 이동만 잠그고 시점은 풂, 조준 원·글을 켬
        /// <param name="cast">조준할 주문</param>
        void StartAim(FieldCastSpec cast)
        {
            _aim = cast;
            _aimLeft = AimSeconds;
            _aimShown = -1;
            _state = CastState.Aiming;
            _player.MoveLocked = true;
            _player.LookLocked = false;
            _hud.Crosshair.SetActive(false);
            _hud.AimRing.SetActive(true);
            RefreshAimText();
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
            // 쏠 주문 (HideAim이 상태를 바꾸기 전에 꺼냄)
            FieldCastSpec cast = _aim;
            HideAim();
            if (CanCast(cast)) Cast(cast);
        }
        #endregion

        #region HideAim - 조준을 끝냄: 이동·시점 잠금을 풀고 조준 원·글을 끄고 조준점을 켬
        void HideAim()
        {
            if (_state == CastState.Aiming) _state = CastState.Idle;
            UnlockPlayer();
            if (_hud == null) return;
            _hud.AimRing.SetActive(false);
            _hud.AimText.gameObject.SetActive(false);
            _hud.Crosshair.SetActive(true);
        }
        #endregion

        #region Cast - 마나를 쓰고 효과를 냄 (정확이면 위력 ×1.4): 탄·폭발 탄·물탄·부채꼴 세 발·화염 벽·방호막. 효과 프리팹이 있으면 입힘
        /// <param name="cast">쓸 주문</param>
        void Cast(FieldCastSpec cast)
        {
            Mana -= cast.ManaCost;
            // 정확 배율을 곱한 위력
            float power = cast.Power * cast.Scale;
            switch (cast.Effect)
            {
                case SpellFieldEffect.Bolt:
                    Shoot(AimDirection(), power, 0f, 0f, FireColor, cast.EffectPrefab);
                    break;
                case SpellFieldEffect.Ball:
                    Shoot(AimDirection(), power, cast.Area, 0f, FireColor, cast.EffectPrefab);
                    break;
                case SpellFieldEffect.Water:
                    Shoot(AimDirection(), power, 0f, cast.Area, WaterColor, cast.EffectPrefab);
                    break;
                case SpellFieldEffect.Fan:
                    // 가운데 방향
                    Vector3 direction = AimDirection();
                    Shoot(Quaternion.AngleAxis(-cast.Area, Vector3.up) * direction, power, 0f, 0f, FireColor, cast.EffectPrefab);
                    Shoot(direction, power, 0f, 0f, FireColor, cast.EffectPrefab);
                    Shoot(Quaternion.AngleAxis(cast.Area, Vector3.up) * direction, power, 0f, 0f, FireColor, cast.EffectPrefab);
                    break;
                case SpellFieldEffect.Wall:
                    PlaceWall(cast, power);
                    break;
                case SpellFieldEffect.Shield:
                    _shieldLeft = cast.Duration * cast.Scale;
                    _shieldCooldown = cast.Cooldown;
                    _unit.DamageScale = cast.Power;
                    ShowShield(cast.EffectPrefab);
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

        #region Shoot - 눈 앞에서 탄 한 발을 쏨 (적 편을 노림). 효과 프리팹이 있으면 날아가는 모습으로 입힘
        /// <param name="direction">방향</param>
        /// <param name="damage">피해</param>
        /// <param name="splash">폭발 반지름 (m)</param>
        /// <param name="push">밀쳐내기 (m)</param>
        /// <param name="color">색</param>
        /// <param name="visualPrefab">효과 프리팹 (없으면 null)</param>
        void Shoot(Vector3 direction, float damage, float splash, float push, Color color, GameObject visualPrefab)
        {
            // 탄이 나갈 곳
            Vector3 from = _eye.position + direction * MuzzleOffset;
            // 쏜 탄
            SpellProjectileController shot = Instantiate(_projectilePrefab, from, Quaternion.LookRotation(direction));
            shot.Launch(direction, ProjectileSpeed, ProjectileLife, damage, splash, push, FieldTeam.Enemy, _unit, _service, color, visualPrefab);
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
            wall.Launch(center, _eye.right, cast.Area, cast.Duration, damagePerSecond, _unit, _service, cast.EffectPrefab);
        }
        #endregion

        #region UpdateShield - 방호막 재사용 대기를 줄이고, 지속이 끝나면 받는 피해 배율을 되돌리고 방호막 모습을 끔
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateShield(float dt)
        {
            if (_shieldCooldown > 0f) _shieldCooldown -= dt;
            if (_shieldLeft <= 0f) return;
            _shieldLeft -= dt;
            if (_shieldLeft > 0f) return;
            _unit.DamageScale = 1f;
            HideShield();
        }
        #endregion

        #region ShowShield - 방호막 모습을 켬: 효과 프리팹이 있으면 눈에 붙여 입히고, 없으면 임시 푸른 화면
        /// <param name="prefab">효과 프리팹 (없으면 null)</param>
        void ShowShield(GameObject prefab)
        {
            HideShield();
            if (prefab != null)
            {
                _shieldFx = Instantiate(prefab, _eye);
                _shieldFx.transform.localPosition = Vector3.zero;
                _shieldFx.transform.localRotation = Quaternion.identity;
                return;
            }
            if (_hud != null && _hud.Shield != null) _hud.Shield.SetActive(true);
        }
        #endregion

        #region HideShield - 방호막 모습을 끔 (입힌 효과는 지우고 임시 화면은 끔)
        void HideShield()
        {
            if (_shieldFx != null)
            {
                Destroy(_shieldFx);
                _shieldFx = null;
            }
            if (_hud != null && _hud.Shield != null) _hud.Shield.SetActive(false);
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
