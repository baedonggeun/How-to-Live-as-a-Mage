using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Mage
{
    /// <summary>숲 리셋·차원문 캡슐 (MonoBehaviour 아님. FieldState가 만들고 이음): 판 데이터·연출·차원문·차원문 놓을 곳·현장 규칙·숲 구역·숲 입구를 받아, 이 판에서 처음이면 차원문 자리를 정해 놓음. 숲이 70% 넘게 빈 채 나갔으면(판 데이터 Pending) 이번 진입에 시체를 받을 때 무리마다 원래 적 자리 구역 안 아무 곳(숲 층 길 위, 입구 30m 밖)을 골라 그 둘레로 옮겨 눕히고, 연출을 틀어 끝나면 그 자리를 집으로 살린 뒤 Finished로 살린 적을 알림. 숲을 나갈 때 쓰러뜨린 비율을 보고 다음 리셋을 정함</summary>
    public sealed partial class ForestResetService
    {
        /// <summary>숲 적 자리 대비 쓰러뜨린 적이 이 비율 이상이면 리셋 (기획 70%)</summary>
        const float ResetRatio = 0.7f;

        /// <summary>숲 입구에서 이 거리 안엔 일으키지 않음 (m. 들어오자마자 코앞에서 일어나지 않게)</summary>
        const float EntryClearance = 30f;

        /// <summary>무리 가운데 둘레에 흩어 눕히는 반지름 (m, 적 자리 기본 흩음과 같게)</summary>
        const float PackScatter = 5f;

        /// <summary>무리 가운데를 고를 때 뽑아 보는 횟수 (다 안 맞으면 원래 적 자리)</summary>
        const int PickTries = 20;

        /// <summary>뽑은 곳에서 길(NavMesh)을 찾는 거리 (m)</summary>
        const float PickSampleDistance = 4f;

        /// <summary>숲 리셋·차원문 판 데이터</summary>
        readonly ForestResetEntry _data;

        /// <summary>리셋 연출</summary>
        readonly ForestResetSequence _sequence;

        /// <summary>차원문</summary>
        readonly Transform _portal;

        /// <summary>차원문을 놓을 수 있는 곳 (3구역 중 전초기지에서 먼 쪽 절반, 월드)</summary>
        readonly Bounds _portalArea;

        /// <summary>현장 규칙 (살린 적이 씀)</summary>
        readonly FieldService _field;

        /// <summary>숲 구역 (시체를 원래 적 자리 구역 안에 옮길 때)</summary>
        readonly ForestZoneSpec _zones;

        /// <summary>숲 입구 자리 (이 둘레엔 안 옮김)</summary>
        readonly Vector3 _entry;

        /// <summary>일어날 시체 (받은 순서)</summary>
        readonly List<EnemyController> _corpses = new();

        /// <summary>시체마다 돌아갈 집 (옮긴 무리 가운데)</summary>
        readonly Dictionary<EnemyController, Vector3> _homes = new();

        /// <summary>무리 번호마다 고른 가운데 (무리에서 처음 받은 시체가 정함)</summary>
        readonly Dictionary<int, Vector3> _packCenters = new();

        /// <summary>이번 진입에 리셋하는지 (만들 때 판 데이터로 정하고, 다 살리면 꺼짐)</summary>
        public bool IsResetting { get; private set; }

        /// <summary>연출이 끝나 시체가 다 일어남 (살린 적. 받는 쪽이 숲 적·소탕 대상으로 다시 받음)</summary>
        public event Action<IReadOnlyList<EnemyController>> Finished;

        #region ForestResetService - 판 데이터·연출·차원문·놓을 곳·현장 규칙·숲 구역·숲 입구를 받고 이번 진입에 리셋하는지 정함
        /// <param name="data">숲 리셋·차원문 판 데이터</param>
        /// <param name="sequence">리셋 연출</param>
        /// <param name="portal">차원문</param>
        /// <param name="portalArea">차원문을 놓을 수 있는 곳</param>
        /// <param name="field">현장 규칙</param>
        /// <param name="zones">숲 구역</param>
        /// <param name="entry">숲 입구 자리</param>
        public ForestResetService(ForestResetEntry data, ForestResetSequence sequence, Transform portal, Bounds portalArea, FieldService field, ForestZoneSpec zones, Vector3 entry)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _sequence = sequence != null ? sequence : throw new ArgumentNullException(nameof(sequence));
            _portal = portal != null ? portal : throw new ArgumentNullException(nameof(portal));
            _portalArea = portalArea;
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _zones = zones;
            _entry = entry;
            IsResetting = _data.Pending;
        }
        #endregion

        #region PlacePortal - 이 판에서 처음이면 놓을 수 있는 곳에서 차원문 자리를 정해 판 데이터에 남기고, 차원문을 그 자리에
        public void PlacePortal()
        {
            if (_data.PortalPosition == null)
            {
                // 고른 곳
                var picked = new Vector3(Random.Range(_portalArea.min.x, _portalArea.max.x), _portalArea.center.y, Random.Range(_portalArea.min.z, _portalArea.max.z));
                _data.PortalPosition = FieldService.Sample(picked);
            }
            _portal.position = _data.PortalPosition.Value;
        }
        #endregion

        #region AddCorpse - 리셋하는 진입이면 시체를 받아, 무리 가운데(무리에서 처음이면 원래 적 자리 구역 안에서 고름) 둘레로 옮겨 눕히고 그 가운데를 집으로 받아 둠 (아니면 무시. 로딩창 아래라 옮기는 게 안 보임)
        /// <param name="corpse">누운 적</param>
        /// <param name="home">원래 적 자리 (이 자리의 구역 안에서 고름)</param>
        public void AddCorpse(EnemyController corpse, Vector3 home)
        {
            if (!IsResetting || corpse == null) return;
            // 이 무리가 일어날 가운데
            if (!_packCenters.TryGetValue(corpse.Pack, out Vector3 center))
            {
                center = PickCenter(home);
                _packCenters[corpse.Pack] = center;
            }
            corpse.transform.position = Scatter(center);
            _corpses.Add(corpse);
            _homes[corpse] = center;
        }
        #endregion

        #region PickCenter - 원래 적 자리 구역 안 아무 곳: 지역 너비 × 구역 깊이에서 뽑아 숲 층 길 위·같은 구역·입구 30m 밖이면 그곳 (20번 다 안 맞으면 원래 적 자리)
        /// <param name="home">원래 적 자리</param>
        Vector3 PickCenter(Vector3 home)
        {
            // 원래 적 자리 구역
            int zone = _zones.ZoneOf(home.z);
            for (int i = 0; i < PickTries; i++)
            {
                // 구역 안에서 뽑은 곳 (높이는 원래 적 자리)
                var picked = new Vector3(Random.Range(_zones.Area.xMin, _zones.Area.xMax), home.y, Random.Range(_zones.MinZ(zone), _zones.MaxZ(zone)));
                if (!NavMesh.SamplePosition(picked, out NavMeshHit hit, PickSampleDistance, NavMesh.AllAreas)) continue;
                if (!FieldService.SameLevel(hit.position, home) || _zones.ZoneOf(hit.position.z) != zone) continue;
                // 숲 입구까지 평면 거리
                Vector3 fromEntry = hit.position - _entry;
                fromEntry.y = 0f;
                if (fromEntry.sqrMagnitude < EntryClearance * EntryClearance) continue;
                return hit.position;
            }
            return home;
        }
        #endregion

        #region Scatter - 무리 가운데 둘레 반지름 5m 안 아무 곳 (길 위로)
        /// <param name="center">무리 가운데</param>
        static Vector3 Scatter(Vector3 center)
        {
            // 흩는 거리
            Vector2 offset = Random.insideUnitCircle * PackScatter;
            return FieldService.Sample(center + new Vector3(offset.x, 0f, offset.y));
        }
        #endregion

        #region Play - 리셋하는 진입이면 연출 시작 (끝나면 시체를 살리고 Finished). 아니면 무시
        /// <param name="focus">카메라가 볼 기준 (플레이어. 가장 가까운 시체를 봄)</param>
        public void Play(Transform focus)
        {
            if (!IsResetting) return;
            _sequence.Play(_portal, _corpses, focus, OnSequenceDone);
        }
        #endregion

        #region MarkLeaving - 숲을 나갈 때: 숲 적 자리 대비 쓰러뜨린 적이 70% 이상이면 다음 진입에 리셋
        /// <param name="deadCount">쓰러뜨린 숲 적 수</param>
        /// <param name="total">숲 적 자리 수 합</param>
        public void MarkLeaving(int deadCount, int total)
        {
            if (total > 0 && (float)deadCount / total >= ResetRatio) _data.Pending = true;
        }
        #endregion

        #region OnSequenceDone - 연출이 끝남: 시체를 집(옮긴 무리 가운데)으로 살리고 리셋 표시를 지운 뒤 살린 적을 알림
        void OnSequenceDone()
        {
            // 살린 적
            var revived = new List<EnemyController>(_corpses.Count);
            foreach (EnemyController corpse in _corpses)
            {
                if (corpse == null) continue;
                corpse.Revive(_field, _homes.TryGetValue(corpse, out Vector3 home) ? home : corpse.transform.position);
                revived.Add(corpse);
            }
            _corpses.Clear();
            _homes.Clear();
            _packCenters.Clear();
            _data.Pending = false;
            IsResetting = false;
            Finished?.Invoke(revived);
        }
        #endregion
    }
}
