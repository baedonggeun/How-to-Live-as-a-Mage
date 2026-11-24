using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mage
{
    /// <summary>용병 파티 캡슐의 숲 쪽 (MonoBehaviour 아님. FieldState가 만들고 이음): 파티 판 데이터·용병 프리팹·현장 규칙·따라갈 마법사를 받아, 파티 용병을 자리 뒤 양옆에 놓고 따라오게 함. 살아 있는 용병을 한 자리로 옮기고(던전 드나들기), 마법사가 죽으면 숲 입구로 도망치게 해 쫓는 적 없이 3초면 무사로 정함. 용병이 죽으면 파티에서 빼고 Lost로 알림</summary>
    public sealed class FieldPartyService
    {
        /// <summary>용병을 자리 둘레에 놓는 간격 (m)</summary>
        const float Spread = 2f;

        /// <summary>쫓는 적 없이 이만큼 지나면 무사 (게임 초, 임시)</summary>
        const float SafeCalmSeconds = 3f;

        /// <summary>파티 판 데이터</summary>
        readonly PartyEntry _data;

        /// <summary>용병 프리팹</summary>
        readonly MercenaryController _prefab;

        /// <summary>현장 규칙 (놓은 용병이 씀, 노림 확인)</summary>
        readonly FieldService _field;

        /// <summary>따라갈 마법사</summary>
        readonly Transform _leader;

        /// <summary>놓은 용병 (파티 순서. 죽어도 남음)</summary>
        readonly List<MercenaryController> _mercs = new();

        /// <summary>도망 중 용병마다 쫓는 적 없이 지난 시간 (게임 초)</summary>
        readonly Dictionary<MercenaryController, float> _calm = new();

        /// <summary>용병을 잃음 (잃은 용병 데이터. 파티에서는 이미 뺌)</summary>
        public event Action<MercenaryEntry> Lost;

        /// <summary>놓은 용병 (HUD 줄에 넘김. 복사본)</summary>
        public MercenaryController[] Mercenaries => _mercs.ToArray();

        #region FieldPartyService - 파티 판 데이터·용병 프리팹·현장 규칙·따라갈 마법사를 받음
        /// <param name="data">파티 판 데이터</param>
        /// <param name="prefab">용병 프리팹</param>
        /// <param name="field">현장 규칙</param>
        /// <param name="leader">따라갈 마법사</param>
        public FieldPartyService(PartyEntry data, MercenaryController prefab, FieldService field, Transform leader)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _leader = leader != null ? leader : throw new ArgumentNullException(nameof(leader));
        }
        #endregion

        #region Spawn - 파티 용병을 자리 뒤 양옆에 놓고 마법사를 따라오게 함
        /// <param name="point">놓을 자리 (숲 입구 안쪽)</param>
        public void Spawn(Transform point)
        {
            for (int i = 0; i < _data.Members.Count; i++)
            {
                // 놓은 용병
                MercenaryController merc = Object.Instantiate(_prefab, BehindSide(point, i), point.rotation);
                merc.Init(_data.Members[i], _field, _leader, i);
                merc.Died += OnMercenaryDied;
                _mercs.Add(merc);
            }
        }
        #endregion

        #region WarpTo - 살아 있는 용병을 자리 뒤 양옆에 순간이동
        /// <param name="point">옮길 자리</param>
        public void WarpTo(Transform point)
        {
            for (int i = 0; i < _mercs.Count; i++)
            {
                // 이 용병
                MercenaryController merc = _mercs[i];
                if (merc == null || !merc.Unit.IsAlive) continue;
                merc.WarpTo(BehindSide(point, i));
            }
        }
        #endregion

        #region StartFlee - 마법사가 죽음: 살아 있는 용병을 그곳(숲 입구)으로 도망치게 함
        /// <param name="exit">도망칠 곳</param>
        public void StartFlee(Transform exit)
        {
            _calm.Clear();
            foreach (MercenaryController merc in _mercs)
                if (merc != null && merc.Unit.IsAlive) merc.Flee(exit);
        }
        #endregion

        #region UpdateFlee - 도망 한 프레임: 쫓는 적 없이 3초 지난 용병은 무사로 정함 (입구에 닿은 용병은 스스로 무사, 도중에 죽으면 Lost). 아직 안 정해진 용병이 있으면 true
        /// <param name="dt">이번 프레임 게임 시간 (초)</param>
        public bool UpdateFlee(float dt)
        {
            // 아직 정해지지 않은 용병이 있는지
            bool pending = false;
            foreach (MercenaryController merc in _mercs)
            {
                if (merc == null || !merc.Unit.IsAlive || merc.IsSafe) continue;
                _calm.TryGetValue(merc, out float quiet);
                quiet = _field.IsTargeted(merc.Unit) ? 0f : quiet + dt;
                _calm[merc] = quiet;
                if (quiet >= SafeCalmSeconds) merc.MarkSafe();
                else pending = true;
            }
            return pending;
        }
        #endregion

        #region OnMercenaryDied - 파티에서 빼고 알림
        /// <param name="merc">죽은 용병</param>
        void OnMercenaryDied(MercenaryController merc)
        {
            merc.Died -= OnMercenaryDied;
            _data.Members.Remove(merc.Entry);
            Lost?.Invoke(merc.Entry);
        }
        #endregion

        #region BehindSide - 자리 기준 index번째 용병 자리 (뒤 양옆, NavMesh 위)
        /// <param name="point">기준 자리</param>
        /// <param name="index">파티 순서</param>
        static Vector3 BehindSide(Transform point, int index)
        {
            // 자리 기준 뒤 양옆
            Vector3 offset = new((index % 2 == 0 ? -1f : 1f) * Spread, 0f, -Spread);
            return FieldService.Sample(point.position + point.rotation * offset);
        }
        #endregion
    }
}
