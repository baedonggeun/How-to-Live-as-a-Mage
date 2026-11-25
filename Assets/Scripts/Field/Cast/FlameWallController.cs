using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>화염 벽 하나 (v1 화염 벽): 시선에 수직인 막대. 0.5초마다 막대에서 1.3m(+몸 반지름) 안 적에게 피해, 지속 시간이 끝나면 사라짐</summary>
    public sealed class FlameWallController : MonoBehaviour
    {
        /// <summary>피해 간격 (초, v1)</summary>
        const float TickSeconds = 0.5f;

        /// <summary>막대에서 피해가 닿는 거리 (m, v1)</summary>
        const float HitRadius = 1.3f;

        /// <summary>벽 도형 두께 (m)</summary>
        const float VisualWidth = 0.4f;

        /// <summary>벽 도형 높이 (m)</summary>
        const float VisualHeight = 1.6f;

        /// <summary>벽 도형 (Cube. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _visual;

        /// <summary>피해 줄 몸 (틱마다 다시 씀)</summary>
        readonly List<FieldUnitController> _found = new();

        /// <summary>현장 규칙</summary>
        FieldService _service;

        /// <summary>벽을 만든 몸 (피해 출처)</summary>
        FieldUnitController _source;

        /// <summary>막대 시작 끝</summary>
        Vector3 _start;

        /// <summary>막대 다른 끝</summary>
        Vector3 _end;

        /// <summary>틱마다 피해</summary>
        float _damagePerTick;

        /// <summary>남은 시간 (초)</summary>
        float _left;

        /// <summary>다음 틱까지 (초)</summary>
        float _tick;

        #region Launch - 그 자리에 막대를 세움: 가운데·방향·길이·지속·초당 피해·만든 몸·현장 규칙
        /// <param name="center">가운데 (땅)</param>
        /// <param name="along">막대 방향 (평면, 0이면 오른쪽)</param>
        /// <param name="length">길이 (m)</param>
        /// <param name="seconds">지속 (초)</param>
        /// <param name="damagePerSecond">초당 피해</param>
        /// <param name="source">만든 몸</param>
        /// <param name="service">현장 규칙</param>
        /// <param name="visualPrefab">벽 모습으로 입힐 효과 프리팹 (없으면 임시 도형)</param>
        public void Launch(Vector3 center, Vector3 along, float length, float seconds, float damagePerSecond, FieldUnitController source, FieldService service,
            GameObject visualPrefab = null)
        {
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            _source = source;
            // 평면 막대 방향
            Vector3 flat = Vector3.ProjectOnPlane(along, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.right;
            flat.Normalize();
            _start = center - flat * (length * 0.5f);
            _end = center + flat * (length * 0.5f);
            transform.SetPositionAndRotation(center, Quaternion.LookRotation(flat));
            if (_visual != null)
            {
                _visual.localScale = new Vector3(VisualWidth, VisualHeight, length);
                _visual.localPosition = Vector3.up * (VisualHeight * 0.5f);
            }
            if (visualPrefab != null)
            {
                // 입힌 효과 (벽 가운데에 벽 방향으로. 콜라이더는 꺼서 탄이 벽에 안 맞게)
                GameObject custom = Instantiate(visualPrefab, transform);
                custom.transform.localPosition = Vector3.zero;
                custom.transform.localRotation = Quaternion.identity;
                foreach (Collider collider in custom.GetComponentsInChildren<Collider>()) collider.enabled = false;
                if (_visual != null) _visual.gameObject.SetActive(false);
            }
            _damagePerTick = damagePerSecond * TickSeconds;
            _left = seconds;
            _tick = 0f;
        }
        #endregion

        #region Update - 멈췄으면 안 함. 틱마다 막대 둘레 적 피해, 시간이 다 되면 지움
        void Update()
        {
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f || _service == null) return;
            _left -= dt;
            _tick -= dt;
            if (_tick <= 0f)
            {
                _tick += TickSeconds;
                _service.FindNearSegment(FieldTeam.Enemy, _start, _end, HitRadius, _found);
                foreach (FieldUnitController unit in _found) unit.TakeDamage(_damagePerTick, _source);
            }
            if (_left <= 0f) Destroy(gameObject);
        }
        #endregion
    }
}
