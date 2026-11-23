using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>숲 HUD ← 마법사 체력·마나·스태미나, 동료 체력, 구역·지도 진행도, 마지막으로 때린 적 체력(2.5초), 맞음(0.35초)·체력 30% 미만 붉은 화면 (v1). 출처는 FieldState가 Bind로 넘김 (조준·알림 글·펜은 FieldCastController)</summary>
    public sealed class FieldHudUIBinder : MonoBehaviour
    {
        /// <summary>때린 적 체력을 보이는 시간 (초, v1)</summary>
        const float TargetSeconds = 2.5f;

        /// <summary>맞았을 때 붉은 화면 시간 (초, v1)</summary>
        const float HurtSeconds = 0.35f;

        /// <summary>이 비율 아래면 체력 낮음 (v1)</summary>
        const float LowHpRatio = 0.3f;

        /// <summary>체력 낮음 붉은 화면 투명도</summary>
        const float LowHpAlpha = 0.35f;

        /// <summary>죽은 동료 줄 투명도</summary>
        const float DeadRowAlpha = 0.4f;

        /// <summary>체력 글 키 ({0} 지금, {1} 최대)</summary>
        const string HpKey = "field.hud.hp";

        /// <summary>마나 글 키 ({0} 지금, {1} 최대)</summary>
        const string ManaKey = "field.hud.mana";

        /// <summary>구역 줄 키 ({0} 구역 이름, {1} 지도 %)</summary>
        const string ZoneLineKey = "field.zone_line";

        /// <summary>구역 이름 키 형식 ({0} 구역 번호 1~3)</summary>
        const string ZoneKeyFormat = "field.zone.{0}";

        /// <summary>숲 문구 테이블</summary>
        static readonly TableReference Table = "Field";

        /// <summary>같은 HUD의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] FieldHudView _view;

        /// <summary>마법사 몸 (없으면 안 함)</summary>
        FieldUnitController _player;

        /// <summary>현장 시전 (마나)</summary>
        FieldCastController _cast;

        /// <summary>동료 (파티 순서)</summary>
        IReadOnlyList<MercenaryController> _mercs;

        /// <summary>현장 규칙 (마지막으로 때린 적)</summary>
        FieldService _service;

        /// <summary>안개 지도 (진행도)</summary>
        FogMapService _fog;

        /// <summary>지금 구역 번호 (1~3)</summary>
        Func<int> _zoneAt;

        /// <summary>맞음 붉은 화면 남은 시간 (초)</summary>
        float _hurtLeft;

        /// <summary>마지막으로 보인 체력 (정수)</summary>
        int _shownHp = -1;

        /// <summary>마지막으로 보인 마나 (정수)</summary>
        int _shownMana = -1;

        /// <summary>마지막으로 보인 구역 번호</summary>
        int _shownZone = -1;

        /// <summary>마지막으로 보인 지도 %</summary>
        int _shownPercent = -1;

        /// <summary>마지막으로 이름을 넣은 때린 적</summary>
        FieldUnitController _shownTarget;

        #region Bind - 출처를 받고 맞음 알림 구독, 스태미나 막대에 조작을 넘김, 동료 줄 이름을 넣음 (남는 줄 숨김)
        /// <param name="player">마법사 몸</param>
        /// <param name="controller">1인칭 조작 (스태미나)</param>
        /// <param name="cast">현장 시전 (마나)</param>
        /// <param name="mercs">동료 (파티 순서)</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="fog">안개 지도</param>
        /// <param name="zoneAt">지금 구역 번호</param>
        public void Bind(FieldUnitController player, PlayerController controller, FieldCastController cast, IReadOnlyList<MercenaryController> mercs,
            FieldService service, FogMapService fog, Func<int> zoneAt)
        {
            Unbind();
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _cast = cast != null ? cast : throw new ArgumentNullException(nameof(cast));
            _mercs = mercs ?? throw new ArgumentNullException(nameof(mercs));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _fog = fog ?? throw new ArgumentNullException(nameof(fog));
            _zoneAt = zoneAt ?? throw new ArgumentNullException(nameof(zoneAt));
            _player.Damaged += OnPlayerDamaged;
            _shownHp = _shownMana = _shownZone = _shownPercent = -1;
            _shownTarget = null;
            _hurtLeft = 0f;
            if (_view == null) return;
            _view.Stamina.Bind(controller);
            for (int i = 0; i < _view.PartyRows.Length; i++)
            {
                // 이 줄
                PartyRowView row = _view.PartyRows[i];
                // 이 줄에 넣을 동료가 있는지
                bool has = i < _mercs.Count && _mercs[i] != null;
                row.gameObject.SetActive(has);
                if (!has) continue;
                // 동료 이름
                LocalizedString name = _mercs[i].Entry.Name;
                row.NameText.StringReference.SetReference(name.TableReference, name.TableEntryReference);
            }
        }
        #endregion

        #region Unbind - 맞음 알림 구독 해제, 출처를 놓음 (상태를 나갈 때)
        public void Unbind()
        {
            if (_player != null) _player.Damaged -= OnPlayerDamaged;
            _player = null;
            _cast = null;
            _mercs = null;
            _service = null;
            _fog = null;
            _zoneAt = null;
            if (_view != null) _view.Stamina.Bind(null);
        }
        #endregion

        #region OnDestroy - 출처를 놓음
        void OnDestroy() => Unbind();
        #endregion

        #region OnPlayerDamaged - 맞으면 붉은 화면 시간을 채움
        /// <param name="unit">맞은 몸 (마법사)</param>
        /// <param name="amount">실제 피해</param>
        /// <param name="source">때린 몸</param>
        void OnPlayerDamaged(FieldUnitController unit, float amount, FieldUnitController source) => _hurtLeft = HurtSeconds;
        #endregion

        #region Update - 출처가 있으면 막대·동료·구역·때린 적·붉은 화면 갱신
        void Update()
        {
            if (_player == null || _view == null) return;
            RefreshBar(_view.HpFill, _view.HpText, HpKey, _player.Hp, _player.MaxHp, ref _shownHp);
            RefreshBar(_view.ManaFill, _view.ManaText, ManaKey, _cast.Mana, _cast.ManaMax, ref _shownMana);
            RefreshParty();
            RefreshZone();
            RefreshTarget();
            RefreshHurt();
        }
        #endregion

        #region RefreshBar - 채움을 맞추고, 보이는 정수가 바뀌었을 때만 글을 다시 씀
        /// <param name="fill">채움</param>
        /// <param name="text">글자</param>
        /// <param name="key">글 키</param>
        /// <param name="value">지금 값</param>
        /// <param name="max">최대 값</param>
        /// <param name="shown">마지막으로 보인 정수</param>
        static void RefreshBar(Image fill, LocalizeStringEvent text, string key, float value, float max, ref int shown)
        {
            fill.fillAmount = max > 0f ? value / max : 0f;
            // 보일 정수 (올림)
            int rounded = Mathf.CeilToInt(value);
            if (rounded == shown) return;
            shown = rounded;
            text.StringReference.SetReference(Table, key);
            text.StringReference.Arguments = new object[] { rounded, Mathf.CeilToInt(max) };
            text.RefreshString();
        }
        #endregion

        #region RefreshParty - 동료 줄 체력 채움, 죽었으면 흐리게
        void RefreshParty()
        {
            for (int i = 0; i < _view.PartyRows.Length && i < _mercs.Count; i++)
            {
                // 이 동료
                MercenaryController merc = _mercs[i];
                if (merc == null) continue;
                // 이 줄
                PartyRowView row = _view.PartyRows[i];
                row.HpFill.fillAmount = merc.Unit.MaxHp > 0f ? merc.Unit.Hp / merc.Unit.MaxHp : 0f;
                row.Group.alpha = merc.Unit.IsAlive ? 1f : DeadRowAlpha;
            }
        }
        #endregion

        #region RefreshZone - 구역이나 지도 %가 바뀌었을 때만 "구역 이름 · 지도 N%"를 다시 씀
        void RefreshZone()
        {
            // 지금 구역·지도 %
            int zone = _zoneAt();
            int percent = Mathf.RoundToInt(_fog.Progress * 100f);
            if (zone == _shownZone && percent == _shownPercent) return;
            _shownZone = zone;
            _shownPercent = percent;
            // 구역 이름 (지금 언어)
            string zoneName = new LocalizedString(Table, string.Format(CultureInfo.InvariantCulture, ZoneKeyFormat, zone)).GetLocalizedString();
            // 구역 줄의 String Reference
            var text = _view.ZoneText.StringReference;
            text.SetReference(Table, ZoneLineKey);
            text.Arguments = new object[] { zoneName, percent };
            _view.ZoneText.RefreshString();
        }
        #endregion

        #region RefreshTarget - 마법사가 2.5초 안에 때린 산 적이 있으면 이름·체력을 보이고, 아니면 숨김
        void RefreshTarget()
        {
            // 마지막으로 때린 적
            FieldUnitController target = _service.LastHit;
            // 보일지
            bool show = target != null && target.IsAlive && Time.time - _service.LastHitTime <= TargetSeconds;
            _view.TargetRoot.SetActive(show);
            if (!show) return;
            _view.TargetFill.fillAmount = target.MaxHp > 0f ? target.Hp / target.MaxHp : 0f;
            if (target == _shownTarget) return;
            _shownTarget = target;
            if (!target.TryGetComponent(out EnemyController enemy) || enemy.Monster == null) return;
            // 적 이름
            LocalizedString name = enemy.Monster.Name;
            _view.TargetName.StringReference.SetReference(name.TableReference, name.TableEntryReference);
        }
        #endregion

        #region RefreshHurt - 맞음 시간과 체력 낮음 중 큰 쪽으로 붉은 화면 투명도
        void RefreshHurt()
        {
            if (_hurtLeft > 0f) _hurtLeft -= Time.deltaTime;
            // 맞음 투명도
            float hurt = _hurtLeft > 0f ? _hurtLeft / HurtSeconds : 0f;
            // 체력 낮음 투명도
            float low = _player.MaxHp > 0f && _player.Hp / _player.MaxHp < LowHpRatio ? LowHpAlpha : 0f;
            _view.Hurt.alpha = Mathf.Max(hurt, low);
        }
        #endregion
    }
}
