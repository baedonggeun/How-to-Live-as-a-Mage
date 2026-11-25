#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mage
{
    /// <summary>숲 적 캡슐 확인 진입점 (에디터 전용, 빌드에 안 들어감): 쓰러뜨린 수 상태 줄, 살아 있는 숲 적 목록 (현장 규칙에 등록된 적 중 이 캡슐이 놓은 키. 숲 상태 확인 버튼이 쓰러뜨릴 적을 고름)</summary>
    public sealed partial class ForestEnemyService
    {
        #region CheckStatus - 쓰러뜨린 숲 적 / 숲 적 자리 수 합
        [CheckStatus("숲 적")]
        string CheckStatus() => string.Format(CultureInfo.InvariantCulture, "쓰러뜨림 {0} / {1}", DeadCount, Total);
        #endregion

        #region CheckAlive - 살아 있는 숲 적을 result에 채움 (먼저 비움. 현장 규칙에 등록된 적 중 이 캡슐 적 키를 가진 것, 리셋으로 살아난 적 포함)
        /// <param name="result">채울 목록</param>
        public void CheckAlive(List<EnemyController> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            // 이 캡슐이 놓는 적 키
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (SpawnPointView point in _points)
            {
                if (point == null) continue;
                for (int i = 0; i < point.Count; i++) keys.Add(string.Format(CultureInfo.InvariantCulture, SpawnKeyFormat, point.Id, i));
            }
            foreach (EnemyController enemy in _field.CheckEnemies)
                if (enemy != null && enemy.Unit.IsAlive && keys.Contains(enemy.SpawnKey)) result.Add(enemy);
        }
        #endregion
    }
}
#endif
