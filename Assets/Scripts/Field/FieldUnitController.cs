using System;
using UnityEngine;

namespace Mage
{
    /// <summary>현장 편</summary>
    public enum FieldTeam
    {
        /// <summary>마법사와 용병</summary>
        Party,

        /// <summary>적</summary>
        Enemy,
    }

    /// <summary>현장에서 맞고 죽는 몸 하나 (마법사·용병·적 공통): 편, 체력, 받는 피해 배율, 맞음·죽음 알림. 움직임은 같은 오브젝트의 다른 Controller가 함</summary>
    public sealed class FieldUnitController : MonoBehaviour
    {
        /// <summary>몸 반지름 최솟값 (m)</summary>
        const float MinRadius = 0.05f;

        /// <summary>편 (인스펙터에서만 정함)</summary>
        [SerializeField] FieldTeam _team;

        /// <summary>몸 반지름 (m). 공격 거리·범위 피해에 더함. 적은 크기에 맞춰 코드가 바꿈</summary>
        [SerializeField, Min(MinRadius)] float _radius = 0.4f;

        /// <summary>겨눌 높이 (m, 발밑 기준)</summary>
        [SerializeField, Min(0f)] float _aimHeight = 1f;

        /// <summary>편</summary>
        public FieldTeam Team => _team;

        /// <summary>몸 반지름 (m)</summary>
        public float Radius => _radius;

        /// <summary>최대 체력</summary>
        public float MaxHp { get; private set; }

        /// <summary>지금 체력</summary>
        public float Hp { get; private set; }

        /// <summary>살아 있는지</summary>
        public bool IsAlive => Hp > 0f;

        /// <summary>받는 피해 배율 (방호막이면 1보다 작음)</summary>
        public float DamageScale { get; set; } = 1f;

        /// <summary>겨눌 점 (월드)</summary>
        public Vector3 AimPoint => transform.position + Vector3.up * _aimHeight;

        /// <summary>맞음 (맞은 몸, 실제 피해, 때린 몸. 때린 몸이 없으면 null)</summary>
        public event Action<FieldUnitController, float, FieldUnitController> Damaged;

        /// <summary>죽음 (죽은 몸)</summary>
        public event Action<FieldUnitController> Died;

        #region Init - 체력을 다 채우고 피해 배율을 되돌림
        /// <param name="maxHp">최대 체력 (0보다 큼)</param>
        public void Init(float maxHp)
        {
            if (maxHp <= 0f) throw new ArgumentOutOfRangeException(nameof(maxHp), maxHp, "최대 체력은 0보다 커야 함");
            MaxHp = maxHp;
            Hp = maxHp;
            DamageScale = 1f;
        }
        #endregion

        #region InitDead - 이미 죽은 몸으로 둠 (다시 들어갈 때 시체. 알림 없음)
        /// <param name="maxHp">최대 체력 (0보다 큼)</param>
        public void InitDead(float maxHp)
        {
            Init(maxHp);
            Hp = 0f;
        }
        #endregion

        #region SetRadius - 몸 반지름을 바꿈 (적 크기)
        /// <param name="radius">새 반지름 (m)</param>
        public void SetRadius(float radius) => _radius = Mathf.Max(MinRadius, radius);
        #endregion

        #region TakeDamage - 살아 있으면 피해 배율을 곱해 체력을 깎고 맞음 알림. 0이 되면 죽음 알림
        /// <param name="amount">피해 (0 이하면 무시)</param>
        /// <param name="source">때린 몸 (없으면 null)</param>
        public void TakeDamage(float amount, FieldUnitController source)
        {
            if (!IsAlive || amount <= 0f) return;
            // 실제로 깎는 피해 (배율 적용, 남은 체력까지만)
            float dealt = Mathf.Min(Hp, amount * DamageScale);
            Hp -= dealt;
            Damaged?.Invoke(this, dealt, source);
            if (Hp > 0f) return;
            Hp = 0f;
            Died?.Invoke(this);
        }
        #endregion
    }
}
