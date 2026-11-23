using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 한 판의 현장 규칙 (MonoBehaviour 아님. FieldState가 만들어 Controller들에 넘김): 몸 목록, 가까운 상대·조준 보정 대상·범위 안 몸 찾기, 무리 깨우기, 전투 상황, 마법사가 마지막으로 때린 적</summary>
    public sealed class FieldService
    {
        /// <summary>등록된 몸 (마법사·용병·적, 등록 순서. 죽어도 남음)</summary>
        readonly List<FieldUnitController> _units = new();

        /// <summary>등록된 적 (무리 깨우기·노림 확인용)</summary>
        readonly List<EnemyController> _enemies = new();

        /// <summary>쫓는 중인 적 수 (0보다 크면 전투 상황)</summary>
        int _engaged;

        /// <summary>마법사 몸</summary>
        public FieldUnitController Player { get; }

        /// <summary>전투 상황인지 (적이 알아채고 공격하러 오는 중)</summary>
        public bool InCombat => _engaged > 0;

        /// <summary>마법사가 마지막으로 때린 적 몸 (없으면 null)</summary>
        public FieldUnitController LastHit { get; private set; }

        /// <summary>마법사가 마지막으로 때린 시각 (게임 시간, 초)</summary>
        public float LastHitTime { get; private set; }

        /// <summary>전투 상황이 바뀜 (true = 시작)</summary>
        public event Action<bool> CombatChanged;

        #region FieldService - 마법사 몸을 받아 첫 몸으로 등록
        /// <param name="player">마법사 몸</param>
        public FieldService(FieldUnitController player)
        {
            Player = player != null ? player : throw new ArgumentNullException(nameof(player));
            Register(player);
        }
        #endregion

        #region Register - 몸 등록 (비었거나 이미 있으면 무시)
        /// <param name="unit">등록할 몸</param>
        public void Register(FieldUnitController unit)
        {
            if (unit == null || _units.Contains(unit)) return;
            _units.Add(unit);
        }
        #endregion

        #region RegisterEnemy - 적 등록 (몸도 같이)
        /// <param name="enemy">등록할 적</param>
        public void RegisterEnemy(EnemyController enemy)
        {
            if (enemy == null || _enemies.Contains(enemy)) return;
            _enemies.Add(enemy);
            Register(enemy.Unit);
        }
        #endregion

        #region Nearest - 그 편의 살아 있는 몸 중 기준점에서 가장 가까운 것 (최대 거리 안, 평면 거리). 없으면 null
        /// <param name="team">찾을 편</param>
        /// <param name="from">기준점</param>
        /// <param name="maxDistance">최대 거리 (m)</param>
        public FieldUnitController Nearest(FieldTeam team, Vector3 from, float maxDistance)
        {
            // 찾은 몸과 거리 제곱
            FieldUnitController best = null;
            float bestSq = maxDistance * maxDistance;
            foreach (FieldUnitController unit in _units)
            {
                if (unit == null || !unit.IsAlive || unit.Team != team) continue;
                // 평면 거리 제곱
                float sq = Flat(unit.transform.position - from).sqrMagnitude;
                if (sq > bestSq) continue;
                bestSq = sq;
                best = unit;
            }
            return best;
        }
        #endregion

        #region AimTarget - 시선 앞 maxDistance 안에서 시선과 가로 거리가 maxOffset 안인 적 중 가로 거리가 가장 작은 것 (v1 조준 보정). 없으면 null
        /// <param name="ray">시선 (눈 위치·방향)</param>
        /// <param name="maxDistance">앞쪽 최대 거리 (m)</param>
        /// <param name="maxOffset">시선에서 최대 가로 거리 (m)</param>
        public FieldUnitController AimTarget(Ray ray, float maxDistance, float maxOffset)
        {
            // 찾은 적과 가로 거리
            FieldUnitController best = null;
            float bestOffset = maxOffset;
            foreach (FieldUnitController unit in _units)
            {
                if (unit == null || !unit.IsAlive || unit.Team != FieldTeam.Enemy) continue;
                // 눈에서 적 겨눌 점까지
                Vector3 to = unit.AimPoint - ray.origin;
                // 시선 방향 거리
                float along = Vector3.Dot(to, ray.direction);
                if (along <= 0f || along > maxDistance) continue;
                // 시선에서 가로 거리
                float offset = (to - ray.direction * along).magnitude;
                if (offset >= bestOffset) continue;
                bestOffset = offset;
                best = unit;
            }
            return best;
        }
        #endregion

        #region FindInRadius - 그 편의 살아 있는 몸 중 중심에서 반지름(+몸 반지름) 안인 것을 result에 채움 (평면 거리)
        /// <param name="team">찾을 편</param>
        /// <param name="center">중심</param>
        /// <param name="radius">반지름 (m)</param>
        /// <param name="result">채울 목록 (먼저 비움)</param>
        public void FindInRadius(FieldTeam team, Vector3 center, float radius, List<FieldUnitController> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            foreach (FieldUnitController unit in _units)
            {
                if (unit == null || !unit.IsAlive || unit.Team != team) continue;
                if (Flat(unit.transform.position - center).magnitude <= radius + unit.Radius) result.Add(unit);
            }
        }
        #endregion

        #region FindNearSegment - 그 편의 살아 있는 몸 중 선분 a→b에서 거리 radius(+몸 반지름) 안인 것을 result에 채움 (평면, 화염 벽)
        /// <param name="team">찾을 편</param>
        /// <param name="a">선분 시작</param>
        /// <param name="b">선분 끝</param>
        /// <param name="radius">선분에서 거리 (m)</param>
        /// <param name="result">채울 목록 (먼저 비움)</param>
        public void FindNearSegment(FieldTeam team, Vector3 a, Vector3 b, float radius, List<FieldUnitController> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            foreach (FieldUnitController unit in _units)
            {
                if (unit == null || !unit.IsAlive || unit.Team != team) continue;
                if (SegmentDistance(unit.transform.position, a, b) <= radius + unit.Radius) result.Add(unit);
            }
        }
        #endregion

        #region WakePack - 같은 무리의 쉬는 적을 다 깨워 target을 쫓게 함
        /// <param name="pack">무리 번호</param>
        /// <param name="target">쫓을 몸</param>
        public void WakePack(int pack, FieldUnitController target)
        {
            foreach (EnemyController enemy in _enemies)
                if (enemy != null && enemy.Pack == pack) enemy.Wake(target);
        }
        #endregion

        #region SetEngaged - 적 하나가 쫓기 시작하거나 그만둠. 전투 상황이 바뀌면 알림
        /// <param name="engaged">쫓기 시작했는지</param>
        public void SetEngaged(bool engaged)
        {
            // 바뀌기 전 전투 상황
            bool was = InCombat;
            _engaged = Mathf.Max(0, _engaged + (engaged ? 1 : -1));
            if (was != InCombat) CombatChanged?.Invoke(InCombat);
        }
        #endregion

        #region IsTargeted - 쫓는 적 중 이 몸을 노리는 것이 있는지 (죽은 뒤 도망 시뮬)
        /// <param name="unit">확인할 몸</param>
        public bool IsTargeted(FieldUnitController unit)
        {
            if (unit == null) return false;
            foreach (EnemyController enemy in _enemies)
                if (enemy != null && enemy.Target == unit) return true;
            return false;
        }
        #endregion

        #region ReportHit - 마법사가 적을 때림 (위 가운데 대상 체력 막대)
        /// <param name="enemy">맞은 적 몸</param>
        public void ReportHit(FieldUnitController enemy)
        {
            LastHit = enemy;
            LastHitTime = Time.time;
        }
        #endregion

        #region Flat - 높이를 뺀 벡터
        /// <param name="v">벡터</param>
        static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
        #endregion

        #region SegmentDistance - 점에서 선분까지 평면 거리
        /// <param name="point">점</param>
        /// <param name="a">선분 시작</param>
        /// <param name="b">선분 끝</param>
        static float SegmentDistance(Vector3 point, Vector3 a, Vector3 b)
        {
            // 평면 좌표
            Vector2 p = new(point.x, point.z);
            Vector2 start = new(a.x, a.z);
            // 선분 방향
            Vector2 along = new Vector2(b.x, b.z) - start;
            // 선분 위 가장 가까운 곳 비율 (0~1)
            float t = along.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - start, along) / along.sqrMagnitude) : 0f;
            return (p - (start + along * t)).magnitude;
        }
        #endregion
    }
}
