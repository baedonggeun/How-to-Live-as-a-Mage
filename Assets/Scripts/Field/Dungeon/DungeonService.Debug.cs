#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEngine;

namespace Mage
{
    /// <summary>고블린 던전 캡슐 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 던전 값 줄, 보관함에 금화 넣기(AddStash 경로), 이번 던전 보스 체력 절반·즉사 (마법사가 때린 것으로. 보스는 받은 현장 규칙의 적 목록에서 이번 세대 보스 키로 찾음)</summary>
    public sealed partial class DungeonService
    {
        /// <summary>보관함에 넣을 금화</summary>
        const int DebugStashGold = 100;

        /// <summary>'보스 체력 절반'이 남기는 체력 비율</summary>
        const float DebugBossHalfRatio = 0.5f;

        #region DebugStatus - 세대·보스 id·클리어·보관 금화
        [DebugControl("던전", "던전")]
        string DebugStatus => string.Format(CultureInfo.InvariantCulture, "{0}세대 · {1} · {2} · 보관 {3}",
            _data.Generation, string.IsNullOrEmpty(_data.BossId) ? "보스 없음" : _data.BossId, _data.Cleared ? "클리어" : "미클리어", _data.StashGold);
        #endregion

        #region DebugAddStash - 보관함에 금화 100 (몬스터가 보관할 때와 같은 AddStash)
        [DebugControl("던전", "보관함에 금화 100")]
        string DebugAddStash()
        {
            AddStash(DebugStashGold);
            return string.Format(CultureInfo.InvariantCulture, "보관함 금화 {0}", _data.StashGold);
        }
        #endregion

        #region DebugBossHalf - 이번 던전 보스를 마법사가 때린 것으로 체력 절반까지 (깨어 쫓기 → 쫓는 중이면 부하 소환)
        [DebugControl("던전", "보스 체력 절반")]
        string DebugBossHalf()
        {
            // 살아 있는 이번 던전 보스
            EnemyController boss = DebugFindBoss();
            if (boss == null) return "살아 있는 보스 없음";
            boss.Unit.TakeDamage(Mathf.Max(0.1f, boss.Unit.Hp - boss.Unit.MaxHp * DebugBossHalfRatio), _field.Player);
            return "보스 체력 절반. 보스가 쫓는 중이어야 부하 3마리 (보스방 앞에서 누르기)";
        }
        #endregion

        #region DebugBossKill - 이번 던전 보스를 마법사가 때린 것으로 즉사 (클리어 경로)
        [DebugControl("던전", "보스 즉사 (던전 클리어)")]
        string DebugBossKill()
        {
            // 살아 있는 이번 던전 보스
            EnemyController boss = DebugFindBoss();
            if (boss == null) return "살아 있는 보스 없음";
            boss.Unit.TakeDamage(float.MaxValue, _field.Player);
            return "보스 즉사 → \"고블린 동굴을 정리했다\"";
        }
        #endregion

        #region DebugFindBoss - 현장 규칙에 등록된 적 중 이번 세대 보스 키를 가진 살아 있는 적 (없으면 null)
        EnemyController DebugFindBoss()
        {
            // 이번 던전 보스 키
            string key = string.Format(CultureInfo.InvariantCulture, BossKeyFormat, _data.Generation);
            foreach (EnemyController enemy in _field.DebugEnemies)
                if (enemy != null && enemy.Unit.IsAlive && string.Equals(enemy.SpawnKey, key, StringComparison.Ordinal)) return enemy;
            return null;
        }
        #endregion
    }
}
#endif
