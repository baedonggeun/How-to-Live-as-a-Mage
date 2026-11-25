using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mage
{
    /// <summary>현장 시전 효과 (MonoBehaviour 아님. FieldCastController가 만들어 씀): 탄·폭발 탄·물탄·부채꼴 세 발·화염 벽을 냄 (방호막은 시전자 몫이라 안 함). 탄 방향은 조준 보정(시선 60m 안·가로 3.5m 안 적), 벽은 시선이 닿은 곳. 효과 프리팹이 있으면 입힘</summary>
    public sealed class SpellEffectService
    {
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

        /// <summary>불 탄 색</summary>
        static readonly Color FireColor = new(1f, 0.55f, 0.15f);

        /// <summary>물 탄 색</summary>
        static readonly Color WaterColor = new(0.35f, 0.65f, 1f);

        /// <summary>주문 탄 프리팹</summary>
        readonly SpellProjectileController _projectilePrefab;

        /// <summary>화염 벽 프리팹</summary>
        readonly FlameWallController _wallPrefab;

        /// <summary>겨누는 눈 (플레이어 Head)</summary>
        readonly Transform _eye;

        /// <summary>시전자 몸 (피해 출처·벽 기본 높이)</summary>
        readonly FieldUnitController _caster;

        /// <summary>현장 규칙 (조준 보정·탄·벽이 씀)</summary>
        readonly FieldService _field;

        #region SpellEffectService - 탄·벽 프리팹·눈·시전자 몸·현장 규칙을 받음
        /// <param name="projectilePrefab">주문 탄 프리팹</param>
        /// <param name="wallPrefab">화염 벽 프리팹</param>
        /// <param name="eye">겨누는 눈</param>
        /// <param name="caster">시전자 몸</param>
        /// <param name="field">현장 규칙</param>
        public SpellEffectService(SpellProjectileController projectilePrefab, FlameWallController wallPrefab, Transform eye, FieldUnitController caster, FieldService field)
        {
            _projectilePrefab = projectilePrefab != null ? projectilePrefab : throw new ArgumentNullException(nameof(projectilePrefab));
            _wallPrefab = wallPrefab != null ? wallPrefab : throw new ArgumentNullException(nameof(wallPrefab));
            _eye = eye != null ? eye : throw new ArgumentNullException(nameof(eye));
            _caster = caster != null ? caster : throw new ArgumentNullException(nameof(caster));
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }
        #endregion

        #region Apply - 효과를 냄: 탄·폭발 탄·물탄·부채꼴 세 발·화염 벽 (방호막·효과 없음은 무시)
        /// <param name="cast">쓸 주문</param>
        /// <param name="power">정확 배율을 곱한 위력</param>
        public void Apply(FieldCastSpec cast, float power)
        {
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
            }
        }
        #endregion

        #region AimDirection - 조준 보정 대상(시선 60m 안·가로 3.5m 안 적)이 있으면 그 겨눌 점으로, 없으면 시선 방향 (v1)
        Vector3 AimDirection()
        {
            // 시선
            var ray = new Ray(_eye.position, _eye.forward);
            // 보정 대상 (없으면 null)
            FieldUnitController target = _field.AimTarget(ray, AimMaxDistance, AimMaxOffset);
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
            SpellProjectileController shot = Object.Instantiate(_projectilePrefab, from, Quaternion.LookRotation(direction));
            shot.Launch(direction, ProjectileSpeed, ProjectileLife, damage, splash, push, FieldTeam.Enemy, _caster, _field, color, visualPrefab);
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
            Vector3 center = _caster.transform.position + flatForward * WallMaxDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, WallMaxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                // 닿은 몸 (몸이 아니면 null)
                FieldUnitController unit = hit.collider.GetComponentInParent<FieldUnitController>();
                center = unit != null ? unit.transform.position : hit.point;
            }
            // 세운 벽
            FlameWallController wall = Object.Instantiate(_wallPrefab, center, Quaternion.identity);
            wall.Launch(center, _eye.right, cast.Area, cast.Duration, damagePerSecond, _caster, _field, cast.EffectPrefab);
        }
        #endregion
    }
}
