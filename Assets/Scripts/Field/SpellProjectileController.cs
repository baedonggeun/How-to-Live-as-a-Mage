using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>날아가는 주문·화살 하나: 매 프레임 구 모양으로 앞을 훑어 처음 닿은 것에서 터짐. 노리는 편의 살아 있는 몸이면 피해(+물이면 밀쳐냄), 몸이 아닌 것(땅·나무·바위)이면 그냥 터짐. 같은 편·죽은 몸·쏜 몸은 지나감. 폭발 범위가 있으면 둘레 몸에 절반 피해. 시간이 다 되면 터지지 않고 사라짐 (v1)</summary>
    public sealed class SpellProjectileController : MonoBehaviour
    {
        /// <summary>터짐이 보이는 시간 (초, v1)</summary>
        const float BurstSeconds = 0.22f;

        /// <summary>폭발 범위 피해 배율 (v1 절반)</summary>
        const float SplashScale = 0.5f;

        /// <summary>폭발 범위가 없을 때 터짐 구 반지름 (m)</summary>
        const float MinBurstRadius = 0.9f;

        /// <summary>한 번에 훑는 콜라이더 최대 수</summary>
        const int MaxHits = 8;

        /// <summary>색 셰이더 속성 (URP)</summary>
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>맞음 판정 반지름 (m)</summary>
        [SerializeField, Min(0.01f)] float _radius = 0.25f;

        /// <summary>날아가는 모습 (인스펙터에서만 연결)</summary>
        [SerializeField] Renderer _visual;

        /// <summary>터질 때 켜는 구 (꺼 둔 자식. 인스펙터에서만 연결)</summary>
        [SerializeField] Renderer _burst;

        /// <summary>훑은 결과 (매 프레임 다시 씀)</summary>
        readonly RaycastHit[] _hits = new RaycastHit[MaxHits];

        /// <summary>폭발 범위 안 몸 (터질 때 다시 씀)</summary>
        readonly List<FieldUnitController> _splashUnits = new();

        /// <summary>날아가는 방향 (단위 벡터)</summary>
        Vector3 _direction;

        /// <summary>속도 (m/s)</summary>
        float _speed;

        /// <summary>남은 비행 시간 (초)</summary>
        float _life;

        /// <summary>맞은 몸 피해</summary>
        float _damage;

        /// <summary>폭발 반지름 (m, 0이면 없음)</summary>
        float _splashRadius;

        /// <summary>밀쳐내는 거리 (m, 0이면 없음)</summary>
        float _push;

        /// <summary>노리는 편</summary>
        FieldTeam _targetTeam;

        /// <summary>쏜 몸 (안 맞음, 피해 출처)</summary>
        FieldUnitController _source;

        /// <summary>현장 규칙 (폭발 범위 찾기)</summary>
        FieldService _service;

        /// <summary>터졌거나 사라지는 중인지</summary>
        bool _done;

        /// <summary>터짐이 보일 남은 시간 (초)</summary>
        float _burstLeft;

        /// <summary>날아가는 모습으로 입힌 효과 프리팹 (없으면 null)</summary>
        GameObject _custom;

        #region Launch - 쏨: 방향·속도·비행 시간·피해·폭발·밀쳐내기·노리는 편·쏜 몸·현장 규칙·색을 받음
        /// <param name="direction">방향 (0이면 지금 앞)</param>
        /// <param name="speed">속도 (m/s)</param>
        /// <param name="lifeSeconds">최대 비행 시간 (초)</param>
        /// <param name="damage">맞은 몸 피해</param>
        /// <param name="splashRadius">폭발 반지름 (m, 0이면 없음)</param>
        /// <param name="push">밀쳐내는 거리 (m, 0이면 없음)</param>
        /// <param name="targetTeam">노리는 편</param>
        /// <param name="source">쏜 몸</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="color">색</param>
        /// <param name="visualPrefab">날아가는 모습으로 입힐 효과 프리팹 (없으면 임시 도형)</param>
        public void Launch(Vector3 direction, float speed, float lifeSeconds, float damage, float splashRadius, float push,
            FieldTeam targetTeam, FieldUnitController source, FieldService service, Color color, GameObject visualPrefab = null)
        {
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            _speed = Mathf.Max(0f, speed);
            _life = Mathf.Max(0f, lifeSeconds);
            _damage = damage;
            _splashRadius = Mathf.Max(0f, splashRadius);
            _push = Mathf.Max(0f, push);
            _targetTeam = targetTeam;
            _source = source;
            _done = false;
            transform.rotation = Quaternion.LookRotation(_direction);
            // 색 칠하기
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            if (_burst != null)
            {
                _burst.SetPropertyBlock(block);
                _burst.gameObject.SetActive(false);
            }
            if (visualPrefab != null) AttachVisual(visualPrefab);
            else if (_visual != null) _visual.SetPropertyBlock(block);
        }
        #endregion

        #region AttachVisual - 날아가는 모습을 받은 효과 프리팹으로 바꿈 (임시 도형은 끔, 콜라이더는 꺼서 스스로 안 맞게)
        /// <param name="prefab">효과 프리팹</param>
        void AttachVisual(GameObject prefab)
        {
            _custom = Instantiate(prefab, transform);
            _custom.transform.localPosition = Vector3.zero;
            _custom.transform.localRotation = Quaternion.identity;
            foreach (Collider collider in _custom.GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (_visual != null) _visual.enabled = false;
        }
        #endregion

        #region Update - 멈췄으면 안 함. 터진 뒤엔 터짐을 잠깐 보이고 지움. 날아가는 중엔 이번 거리만큼 훑어 닿으면 터지고, 아니면 나아가다 시간이 다 되면 사라짐
        void Update()
        {
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (_done)
            {
                _burstLeft -= dt;
                if (_burstLeft <= 0f) Destroy(gameObject);
                return;
            }
            if (_service == null) return;
            // 이번 프레임 날아갈 거리
            float step = _speed * dt;
            if (TryHit(step, out Vector3 point, out FieldUnitController unit))
            {
                Explode(point, unit);
                return;
            }
            transform.position += _direction * step;
            _life -= dt;
            if (_life <= 0f) Finish(false);
        }
        #endregion

        #region TryHit - 이번 거리만큼 구로 훑어 가장 가까운 '닿을 것'을 찾음: 노리는 편의 산 몸이거나 몸이 아닌 콜라이더. 트리거·같은 편·죽은 몸·쏜 몸은 건너뜀
        /// <param name="step">훑을 거리 (m)</param>
        /// <param name="point">닿은 곳</param>
        /// <param name="unit">닿은 몸 (몸이 아니면 null)</param>
        bool TryHit(float step, out Vector3 point, out FieldUnitController unit)
        {
            point = transform.position;
            unit = null;
            // 훑은 콜라이더 수
            int count = Physics.SphereCastNonAlloc(transform.position, _radius, _direction, _hits, step, ~0, QueryTriggerInteraction.Ignore);
            // 가장 가까운 닿은 거리
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                // 이번 콜라이더
                RaycastHit hit = _hits[i];
                if (hit.collider == null || hit.distance >= best) continue;
                // 콜라이더의 몸 (몸이 아니면 null)
                FieldUnitController other = hit.collider.GetComponentInParent<FieldUnitController>();
                if (other != null && (other == _source || other.Team != _targetTeam || !other.IsAlive)) continue;
                best = hit.distance;
                unit = other;
                // 처음부터 겹쳐 있으면 hit.point가 0이라 지금 자리
                point = hit.distance > 0f ? hit.point : transform.position;
            }
            return best < float.MaxValue;
        }
        #endregion

        #region Explode - 닿은 몸 피해(+밀쳐내기), 폭발 범위 둘레 몸 절반 피해, 터짐을 보임
        /// <param name="point">터진 곳</param>
        /// <param name="unit">닿은 몸 (없으면 null)</param>
        void Explode(Vector3 point, FieldUnitController unit)
        {
            transform.position = point;
            if (unit != null)
            {
                unit.TakeDamage(_damage, _source);
                if (_push > 0f && unit.TryGetComponent(out EnemyController enemy))
                    enemy.Push(Vector3.ProjectOnPlane(_direction, Vector3.up).normalized * _push);
            }
            if (_splashRadius > 0f)
            {
                _service.FindInRadius(_targetTeam, point, _splashRadius, _splashUnits);
                foreach (FieldUnitController other in _splashUnits)
                    if (other != unit) other.TakeDamage(_damage * SplashScale, _source);
            }
            Finish(true);
        }
        #endregion

        #region Finish - 날아가는 모습(입힌 효과 포함)을 끄고, 터졌으면 터짐 구를 폭발 크기로 잠깐 켬
        /// <param name="burst">터짐을 보일지</param>
        void Finish(bool burst)
        {
            _done = true;
            if (_visual != null) _visual.enabled = false;
            if (_custom != null)
            {
                Destroy(_custom);
                _custom = null;
            }
            _burstLeft = 0f;
            if (!burst || _burst == null) return;
            _burst.transform.localScale = Vector3.one * (Mathf.Max(MinBurstRadius, _splashRadius) * 2f);
            _burst.gameObject.SetActive(true);
            _burstLeft = BurstSeconds;
        }
        #endregion
    }
}
