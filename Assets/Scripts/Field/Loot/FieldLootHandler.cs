using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mage
{
    /// <summary>떨어진 금화 캡슐 (MonoBehaviour 아님. FieldState가 만들고 이음): 금화 판 데이터·현장 규칙·금화 지점 프리팹을 받아, 떨어진 금화가 있으면 그 자리에 지점을 놓고 현장 규칙에 알림(줍는 몬스터가 찾음). 누가 주우면(마법사·몬스터) 지점과 데이터를 지우고, 들고 가던 몬스터가 쓰러지면 그 자리에 다시 놓음. 마법사가 죽으면 지금 지점을 지우고 다음 진입 때 놓을 금화를 적어 둠</summary>
    public sealed partial class FieldLootHandler
    {
        /// <summary>떨어진 금화 판 데이터</summary>
        readonly LootEntry _data;

        /// <summary>현장 규칙 (떨어진 금화 지점·줍기 알림)</summary>
        readonly FieldService _field;

        /// <summary>떨어진 금화 지점 프리팹 (GoldPile.prefab, 지점 id gold)</summary>
        readonly InteractPointView _pilePrefab;

        /// <summary>놓인 금화 지점 (없으면 null)</summary>
        InteractPointView _pile;

        #region FieldLootHandler - 금화 판 데이터·현장 규칙·지점 프리팹을 받고 줍기·떨굼 알림을 구독
        /// <param name="data">떨어진 금화 판 데이터</param>
        /// <param name="field">현장 규칙</param>
        /// <param name="pilePrefab">금화 지점 프리팹</param>
        public FieldLootHandler(LootEntry data, FieldService field, InteractPointView pilePrefab)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _pilePrefab = pilePrefab != null ? pilePrefab : throw new ArgumentNullException(nameof(pilePrefab));
            _field.LootTaken += OnTaken;
            _field.LootDropped += OnDropped;
        }
        #endregion

        #region Spawn - 떨어진 금화가 있으면 그 자리에 지점을 놓음
        public void Spawn()
        {
            if (_data.Gold > 0) Place(_data.Position, _data.Gold);
        }
        #endregion

        #region Pick - 마법사가 떨어진 금화를 다 주움 (지점은 줍기 알림이 지움). 주운 금화 (없으면 0)
        public int Pick() => _field.TakeLoot();
        #endregion

        #region DropOnDeath - 마법사가 죽어 금화를 남김: 지금 지점을 지우고(죽은 뒤 도망 시뮬 중 몬스터가 안 줍게) 다음 진입 때 놓을 자리·금화를 적어 둠 (0이면 떨어진 것 없음)
        /// <param name="position">죽은 자리</param>
        /// <param name="gold">들고 있던 금화</param>
        public void DropOnDeath(Vector3 position, int gold)
        {
            RemovePile();
            _field.SetLoot(null, 0);
            _data.Gold = Mathf.Max(0, gold);
            _data.Position = position;
        }
        #endregion

        #region OnTaken - 누가 주움 (마법사·몬스터): 지점과 데이터를 지움
        void OnTaken()
        {
            RemovePile();
            _data.Gold = 0;
        }
        #endregion

        #region OnDropped - 들고 가던 몬스터가 쓰러져 금화를 떨굼: 그 자리(NavMesh 위)에 지점을 놓음
        /// <param name="position">떨군 곳</param>
        /// <param name="gold">금화</param>
        void OnDropped(Vector3 position, int gold) => Place(FieldService.Sample(position), gold);
        #endregion

        #region Place - 앞 지점을 지우고 그 자리에 지점을 놓아 데이터·현장 규칙에 남김
        /// <param name="at">놓을 곳</param>
        /// <param name="gold">금화</param>
        void Place(Vector3 at, int gold)
        {
            RemovePile();
            _data.Gold = gold;
            _data.Position = at;
            _pile = Object.Instantiate(_pilePrefab, at, Quaternion.identity);
            _field.SetLoot(_pile.transform, gold);
        }
        #endregion

        #region RemovePile - 놓인 지점을 지움
        void RemovePile()
        {
            if (_pile != null) Object.Destroy(_pile.gameObject);
            _pile = null;
        }
        #endregion
    }
}
