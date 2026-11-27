# Plan: 숲 지역 구조 — 전초기지 둘레 원형 3지역, 지역마다 차원문(1·2·2, 발견하면 고정), 숲 가운데 정문 귀환, 전초기지 목책

## 현행 분석 (`62c2301`)

- 숲 (`Forest.unity`): 숲 입구 표지판(z 0)에서 북쪽으로 띠 3개(깊이 240m, 너비 180m). `ForestZoneSpec`이 z로 구역을 셈 (HUD 구역 줄, 리셋 시체를 옮길 구역). 바닥 Cube 180 × 720, 경계 Cube 4개, 나무 810·바위 30 (시드 4201 빌더, 스크립트는 안 남음), 적 자리 19개, 던전 입구 (−70, 0, 130), 던전은 지하 y −60 (NavMesh 따로)
- 차원문: 씬에 `Portal` 하나. `ForestResetService.PlacePortal`이 판에서 처음 들어올 때 `FieldView._portalArea`에서 뽑아 `ForestResetEntry.PortalPosition`에 남김. 리셋 연출(`ForestResetSequence.Play`)이 그 차원문을 맥동
- 안개 지도: `FogMapService`가 `FieldView._mapArea` (−90, 0, 180, 720)를 3m 칸(60 × 240)으로 나눔. 진행도 분모는 60m 구획 전부. 숲 HUD 미니맵과 메뉴 지도 탭(Boot, 170 × 680)이 같은 `MapUIBinder`
- 귀환: 표지판의 `InteractPointView` (`forest_exit`) → `HubState(FromField)` → 전초기지 정문 밖 `ReturnPoint`. 숲에 들어오면 `Entrance/EntryPoint` (0, 0, 10)
- 전초기지 (`Outpost.unity`): 성벽 Cube 5개(40 × 40, 높이 3), 정문 기둥·상인방, 망루 4개(`Tower.prefab`), 바깥 나무 40그루가 사방(−120~120), 정문 밖 길(z −8 ~ −140), 역마차는 길로 와서 z −27에 섬. 바깥에 경계 없음

---

## C# 변경

### `Assets/Scripts/Field/Forest/ForestRegionSpec.cs` (새 파일)

`ForestZoneSpec.cs`·`.meta`는 지움 (직렬화 참조 없는 struct).

```csharp
using UnityEngine;

namespace Mage
{
    /// <summary>숲 지역 (숲 상태가 숲 화면 값으로 만들어 넘김): 전초기지 가운데를 둘러싼 원형 고리 지역. 가운데에서 잰 평면 거리로 몇 지역인지, 지역의 안쪽·바깥 반지름, 두 반지름 사이 아무 자리를 알려 줌. 1지역은 안쪽 반지름부터 폭만큼, 다음 지역은 바깥으로 폭만큼씩</summary>
    public readonly struct ForestRegionSpec
    {
        /// <summary>지역 가운데 (전초기지 가운데, 월드. y는 안 씀)</summary>
        public Vector3 Center { get; }

        /// <summary>1지역이 시작하는 반지름 (m. 안쪽은 전초기지)</summary>
        public float InnerRadius { get; }

        /// <summary>지역 하나 폭 (m)</summary>
        public float Width { get; }

        /// <summary>지역 수</summary>
        public int Count { get; }

        /// <summary>마지막 지역이 끝나는 반지름 (m, 숲 바깥 경계)</summary>
        public float OuterRadius => InnerRadius + Width * Count;

        #region ForestRegionSpec - 가운데·안쪽 반지름·폭·지역 수를 받음 (반지름 0·폭 1m·지역 1개 아래로는 자름)
        /// <param name="center">지역 가운데 (전초기지 가운데)</param>
        /// <param name="innerRadius">1지역이 시작하는 반지름 (m)</param>
        /// <param name="width">지역 하나 폭 (m)</param>
        /// <param name="count">지역 수</param>
        public ForestRegionSpec(Vector3 center, float innerRadius, float width, int count)
        {
            Center = center;
            InnerRadius = Mathf.Max(0f, innerRadius);
            Width = Mathf.Max(1f, width);
            Count = Mathf.Max(1, count);
        }
        #endregion

        #region Distance - 가운데에서 평면 거리 (m, 높이 무시)
        /// <param name="position">월드 위치</param>
        public float Distance(Vector3 position) => new Vector2(position.x - Center.x, position.z - Center.z).magnitude;
        #endregion

        #region RegionOf - 위치가 있는 지역 번호 (1~지역 수. 전초기지 쪽은 1, 바깥 경계 밖은 마지막 지역)
        /// <param name="position">월드 위치</param>
        public int RegionOf(Vector3 position) => Mathf.Clamp(Mathf.FloorToInt((Distance(position) - InnerRadius) / Width) + 1, 1, Count);
        #endregion

        #region MinRadius - 지역이 시작하는 반지름 (m)
        /// <param name="region">지역 번호 (1~지역 수, 밖이면 가까운 끝 지역)</param>
        public float MinRadius(int region) => InnerRadius + (Mathf.Clamp(region, 1, Count) - 1) * Width;
        #endregion

        #region MaxRadius - 지역이 끝나는 반지름 (m)
        /// <param name="region">지역 번호 (1~지역 수, 밖이면 가까운 끝 지역)</param>
        public float MaxRadius(int region) => MinRadius(region) + Width;
        #endregion

        #region RandomPoint - 두 반지름 사이 고리 안 아무 자리 (넓이 고르게: 각도는 고르게, 반지름은 제곱 기준으로 고르게)
        /// <param name="minRadius">안쪽 반지름 (m)</param>
        /// <param name="maxRadius">바깥 반지름 (m)</param>
        /// <param name="y">높이 (월드)</param>
        public Vector3 RandomPoint(float minRadius, float maxRadius, float y)
        {
            // 각도 (라디안, +z에서 +x 쪽으로)
            float angle = Random.Range(0f, Mathf.PI * 2f);
            // 반지름 (넓이 고르게)
            float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
            return new Vector3(Center.x + Mathf.Sin(angle) * radius, y, Center.z + Mathf.Cos(angle) * radius);
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- `FieldState.Regions()`가 FieldView 값으로 만들어 안개 지도(바깥 반지름)·숲 리셋·차원문 캡슐에 넘김, `RegionAt`이 HUD 지역 줄에 씀

---

### `Assets/Scripts/Field/Portal/PortalEntry.cs` (새 파일, 새 폴더)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>차원문 판 데이터 (RunEntry가 씬을 넘어 들고 다님). PortalService만 만짐</summary>
    public sealed class PortalEntry
    {
        /// <summary>발견한 차원문: 자리 id → 자리. 한 번 남기면 그 판 동안 안 바뀜 (안 남긴 차원문은 숲에 들어올 때마다 다시 뽑음)</summary>
        public Dictionary<string, Vector3> Found { get; } = new(StringComparer.Ordinal);
    }
}
```

---

### `Assets/Scripts/Field/Portal/PortalView.cs` (새 파일)

```csharp
using UnityEngine;

namespace Mage
{
    /// <summary>숲 차원문 표지 (Portal.prefab 루트, 로직 없음). 차원문 캡슐이 이 지역 고리 안에 자리를 정해 옮김. 발견 기록 키는 id</summary>
    public sealed class PortalView : MonoBehaviour
    {
        /// <summary>자리 id (씬 안에서 겹치지 않게. 판 데이터 발견 기록 키. 인스펙터에서만 정함)</summary>
        [SerializeField] string _id;

        /// <summary>놓을 지역 (1~3. 인스펙터에서만 정함)</summary>
        [SerializeField, Min(1)] int _region = 1;

        /// <summary>자리 id</summary>
        public string Id => _id;

        /// <summary>놓을 지역</summary>
        public int Region => _region;
    }
}
```

---

### `Assets/Scripts/Field/Portal/PortalService.cs` (새 파일)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mage
{
    /// <summary>차원문 캡슐 (MonoBehaviour 아님. FieldState가 만들고 이음): 판 데이터·차원문 표지·숲 지역·숲 입구를 받아, 숲에 들어올 때마다 차원문을 놓음 (발견한 것은 남긴 자리, 아직이면 자기 지역 고리 안 무작위. 입구 40m·차원문끼리 60m 띄우고 전초기지 가운데를 보게). 마법사가 24m 안(같은 층)에 들어오면 발견: 자리를 판 데이터에 남기고 Found로 지역 번호를 알림. 리셋 연출에 쓸 가장 가까운 차원문, 지도에 보일 발견한 자리를 줌</summary>
    public sealed partial class PortalService
    {
        /// <summary>이 거리 안에 들어오면 발견 (m, 안개를 밝히는 반지름과 같게. 임시)</summary>
        const float FindRadius = FogMapService.RevealRadius;

        /// <summary>지역 경계에서 띄우는 거리 (m, 차원문이 두 지역에 걸치지 않게)</summary>
        const float EdgeMargin = 8f;

        /// <summary>숲 입구에서 띄우는 거리 (m, 임시)</summary>
        const float EntryClearance = 40f;

        /// <summary>차원문끼리 띄우는 거리 (m, 임시)</summary>
        const float Spacing = 60f;

        /// <summary>자리를 뽑아 보는 횟수 (다 안 맞으면 마지막으로 뽑은 곳)</summary>
        const int PickTries = 30;

        /// <summary>뽑은 곳에서 길(NavMesh)을 찾는 거리 (m)</summary>
        const float PickSampleDistance = 4f;

        /// <summary>차원문 판 데이터</summary>
        readonly PortalEntry _data;

        /// <summary>차원문 표지</summary>
        readonly PortalView[] _portals;

        /// <summary>숲 지역 (자기 지역 고리에서 자리를 뽑음)</summary>
        readonly ForestRegionSpec _regions;

        /// <summary>숲 입구 자리 (이 둘레엔 안 놓음)</summary>
        readonly Vector3 _entry;

        /// <summary>이번 진입에 놓은 자리 (뽑을 때 간격 비교)</summary>
        readonly List<Vector3> _placed = new();

        /// <summary>차원문 수 (빈 칸 뺌)</summary>
        public int Total { get; }

        /// <summary>발견한 차원문 수</summary>
        public int FoundCount => _data.Found.Count;

        /// <summary>차원문을 발견함 (그 차원문 지역 번호)</summary>
        public event Action<int> Found;

        #region PortalService - 판 데이터·차원문 표지·숲 지역·숲 입구를 받고 차원문 수를 셈
        /// <param name="data">차원문 판 데이터</param>
        /// <param name="portals">차원문 표지</param>
        /// <param name="regions">숲 지역</param>
        /// <param name="entry">숲 입구 자리</param>
        public PortalService(PortalEntry data, PortalView[] portals, ForestRegionSpec regions, Vector3 entry)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _portals = portals ?? throw new ArgumentNullException(nameof(portals));
            _regions = regions;
            _entry = entry;
            // 빈 칸 뺀 차원문 수
            int total = 0;
            foreach (PortalView portal in _portals)
                if (portal != null) total++;
            Total = total;
        }
        #endregion

        #region Place - 차원문을 놓음: 발견한 것은 남긴 자리에 먼저, 아직인 것은 자기 지역 고리 안에서 뽑은 자리에. 다 전초기지 가운데를 봄 (빈 칸·id 없는 표지는 경고 후 건너뜀)
        public void Place()
        {
            _placed.Clear();
            foreach (PortalView portal in _portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.Id) || !_data.Found.TryGetValue(portal.Id, out Vector3 at)) continue;
                portal.transform.position = at;
                Face(portal.transform);
                _placed.Add(at);
            }
            foreach (PortalView portal in _portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.Id))
                {
                    Debug.LogWarning("[Portal] 비었거나 id가 없는 차원문 표지", portal);
                    continue;
                }
                if (_data.Found.ContainsKey(portal.Id)) continue;
                // 뽑은 자리
                Vector3 at = Pick(portal.Region);
                portal.transform.position = at;
                Face(portal.transform);
                _placed.Add(at);
            }
        }
        #endregion

        #region Pick - 지역 고리(경계 8m 안쪽)에서 뽑아 숲 층 길 위·같은 지역·입구 40m 밖·이번에 놓은 차원문과 60m 밖이면 그곳 (30번 다 안 맞으면 마지막으로 뽑은 곳을 길 위로)
        /// <param name="region">지역 번호</param>
        Vector3 Pick(int region)
        {
            // 뽑을 고리 반지름
            float min = _regions.MinRadius(region) + EdgeMargin;
            float max = _regions.MaxRadius(region) - EdgeMargin;
            // 마지막으로 뽑은 곳
            Vector3 picked = _entry;
            for (int i = 0; i < PickTries; i++)
            {
                picked = _regions.RandomPoint(min, max, _entry.y);
                if (!NavMesh.SamplePosition(picked, out NavMeshHit hit, PickSampleDistance, NavMesh.AllAreas)) continue;
                if (!FieldService.SameLevel(hit.position, _entry) || _regions.RegionOf(hit.position) != region) continue;
                if (FlatSqr(hit.position, _entry) < EntryClearance * EntryClearance || IsCrowded(hit.position)) continue;
                return hit.position;
            }
            return FieldService.Sample(picked);
        }
        #endregion

        #region IsCrowded - 이번에 놓은 차원문과 60m 안인지
        /// <param name="position">볼 자리</param>
        bool IsCrowded(Vector3 position)
        {
            foreach (Vector3 placed in _placed)
                if (FlatSqr(position, placed) < Spacing * Spacing) return true;
            return false;
        }
        #endregion

        #region Face - 차원문이 전초기지 가운데를 보게 (평면 방향만)
        /// <param name="portal">차원문</param>
        void Face(Transform portal)
        {
            // 차원문 → 지역 가운데 평면 방향
            Vector3 toCenter = _regions.Center - portal.position;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0f) portal.rotation = Quaternion.LookRotation(toCenter, Vector3.up);
        }
        #endregion

        #region Check - 아직 발견 안 한 차원문 중 마법사 자리에서 24m 안(같은 층)이면 발견: 자리를 판 데이터에 남기고 지역 번호를 알림
        /// <param name="position">마법사 자리</param>
        public void Check(Vector3 position)
        {
            foreach (PortalView portal in _portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.Id) || _data.Found.ContainsKey(portal.Id)) continue;
                // 차원문 자리
                Vector3 at = portal.transform.position;
                if (!FieldService.SameLevel(at, position) || FlatSqr(at, position) > FindRadius * FindRadius) continue;
                _data.Found[portal.Id] = at;
                Found?.Invoke(portal.Region);
            }
        }
        #endregion

        #region Nearest - 그 자리에서 가장 가까운 차원문 (발견 여부 무관, 없으면 null). 리셋 연출에 씀
        /// <param name="from">기준 자리</param>
        public Transform Nearest(Vector3 from)
        {
            // 찾은 차원문과 거리 제곱
            Transform best = null;
            float bestSq = float.MaxValue;
            foreach (PortalView portal in _portals)
            {
                if (portal == null) continue;
                // 거리 제곱
                float sq = FlatSqr(portal.transform.position, from);
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = portal.transform;
            }
            return best;
        }
        #endregion

        #region CopyFound - 발견한 차원문 자리를 result에 채움 (먼저 비움. 지도 표시)
        /// <param name="result">채울 목록</param>
        public void CopyFound(List<Vector3> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            foreach (Vector3 at in _data.Found.Values) result.Add(at);
        }
        #endregion

        #region FlatSqr - 두 자리 평면 거리 제곱
        /// <param name="a">한 자리</param>
        /// <param name="b">다른 자리</param>
        static float FlatSqr(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).sqrMagnitude;
        #endregion
    }
}
```

**호출/구독 관계:**
- `FieldState.OnSceneReady`가 만들고 `Place` (숲 NavMesh가 있는 씬 준비 뒤), `Found` → `FieldState.OnPortalFound` (토스트)
- `FieldState.PortalCheckRoutine`이 0.5초마다 `Check(마법사 자리)`
- `FieldState.Reveal`이 리셋하는 진입이면 `Nearest(마법사 자리)`를 `ForestResetService.Play`에 넘김
- `CopyFound`는 미니맵(`FieldHudView.Minimap`)·메뉴 지도(`UIManager.SetMapSource`)가 매 프레임 부름

---

### `Assets/Scripts/Field/Portal/PortalService.Debug.cs` (새 파일)

```csharp
#if UNITY_EDITOR
using System.Globalization;
using UnityEngine;

namespace Mage
{
    /// <summary>차원문 캡슐 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 발견 수 값 줄, 발견 기록 지우기, 가장 가까운 안 발견 차원문 자리 (숲 상태 디버그 진입점이 그 근처로 옮김)</summary>
    public sealed partial class PortalService
    {
        #region DebugStatus - 발견한 차원문 / 차원문 수
        [DebugControl("숲", "차원문")]
        string DebugStatus => string.Format(CultureInfo.InvariantCulture, "발견 {0} / {1}", FoundCount, Total);
        #endregion

        #region DebugForget - 발견 기록을 지움 (지금 자리는 그대로, 다음 진입에 다시 뽑음)
        [DebugControl("숲", "차원문 발견 지우기")]
        string DebugForget()
        {
            _data.Found.Clear();
            return "발견 기록을 지움 → 다음 진입에 자리를 다시 뽑음 (지금 자리에서 24m 안에 가면 다시 발견)";
        }
        #endregion

        #region DebugNearestHidden - 그 자리에서 가장 가까운 안 발견 차원문 자리 (다 발견했으면 null)
        /// <param name="from">기준 자리</param>
        public Vector3? DebugNearestHidden(Vector3 from)
        {
            // 찾은 자리와 거리 제곱
            Vector3? best = null;
            float bestSq = float.MaxValue;
            foreach (PortalView portal in _portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.Id) || _data.Found.ContainsKey(portal.Id)) continue;
                // 거리 제곱
                float sq = FlatSqr(portal.transform.position, from);
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = portal.transform.position;
            }
            return best;
        }
        #endregion
    }
}
#endif
```

---

### `Assets/Scripts/Field/Reset/ForestResetEntry.cs` (전체 교체)

```csharp
namespace Mage
{
    /// <summary>숲 리셋 판 데이터 (RunEntry가 씬을 넘어 들고 다님). ForestResetService만 만짐</summary>
    public sealed class ForestResetEntry
    {
        /// <summary>다음에 숲에 들어갈 때 리셋할지 (숲을 나갈 때 쓰러뜨린 적이 70% 이상이면 켬)</summary>
        public bool Pending { get; set; }
    }
}
```

---

### `Assets/Scripts/Field/Reset/ForestResetService.cs` (전체 교체)

바뀐 것: 차원문(`_portal`·`_portalArea`·`PlacePortal`)을 빼고, 시체를 옮길 자리를 지역 고리에서 뽑고, `Play`가 맥동할 차원문을 받음 (없으면 연출 없이 바로 살림).

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Mage
{
    /// <summary>숲 리셋 캡슐 (MonoBehaviour 아님. FieldState가 만들고 이음): 판 데이터·연출·현장 규칙·숲 지역·숲 입구를 받아, 숲이 70% 넘게 빈 채 나갔으면(판 데이터 Pending) 이번 진입에 시체를 받을 때 무리마다 원래 적 자리 지역 고리 안 아무 곳(숲 층 길 위, 입구 30m 밖)을 골라 그 둘레로 옮겨 눕히고, 상태가 넘긴 차원문으로 연출을 틀어 끝나면 그 자리를 집으로 살린 뒤 Finished로 살린 적을 알림. 숲을 나갈 때 쓰러뜨린 비율을 보고 다음 리셋을 정함</summary>
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

        /// <summary>숲 리셋 판 데이터</summary>
        readonly ForestResetEntry _data;

        /// <summary>리셋 연출</summary>
        readonly ForestResetSequence _sequence;

        /// <summary>현장 규칙 (살린 적이 씀)</summary>
        readonly FieldService _field;

        /// <summary>숲 지역 (시체를 원래 적 자리 지역 고리 안에 옮길 때)</summary>
        readonly ForestRegionSpec _regions;

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

        #region ForestResetService - 판 데이터·연출·현장 규칙·숲 지역·숲 입구를 받고 이번 진입에 리셋하는지 정함
        /// <param name="data">숲 리셋 판 데이터</param>
        /// <param name="sequence">리셋 연출</param>
        /// <param name="field">현장 규칙</param>
        /// <param name="regions">숲 지역</param>
        /// <param name="entry">숲 입구 자리</param>
        public ForestResetService(ForestResetEntry data, ForestResetSequence sequence, FieldService field, ForestRegionSpec regions, Vector3 entry)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _sequence = sequence != null ? sequence : throw new ArgumentNullException(nameof(sequence));
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _regions = regions;
            _entry = entry;
            IsResetting = _data.Pending;
        }
        #endregion

        #region AddCorpse - 리셋하는 진입이면 시체를 받아, 무리 가운데(무리에서 처음이면 원래 적 자리 지역 고리 안에서 고름) 둘레로 옮겨 눕히고 그 가운데를 집으로 받아 둠 (아니면 무시. 로딩창 아래라 옮기는 게 안 보임)
        /// <param name="corpse">누운 적</param>
        /// <param name="home">원래 적 자리 (이 자리의 지역 고리 안에서 고름)</param>
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

        #region PickCenter - 원래 적 자리 지역 안 아무 곳: 그 지역 고리에서 뽑아 숲 층 길 위·같은 지역·입구 30m 밖이면 그곳 (20번 다 안 맞으면 원래 적 자리)
        /// <param name="home">원래 적 자리</param>
        Vector3 PickCenter(Vector3 home)
        {
            // 원래 적 자리 지역
            int region = _regions.RegionOf(home);
            for (int i = 0; i < PickTries; i++)
            {
                // 지역 고리 안에서 뽑은 곳 (높이는 원래 적 자리)
                Vector3 picked = _regions.RandomPoint(_regions.MinRadius(region), _regions.MaxRadius(region), home.y);
                if (!NavMesh.SamplePosition(picked, out NavMeshHit hit, PickSampleDistance, NavMesh.AllAreas)) continue;
                if (!FieldService.SameLevel(hit.position, home) || _regions.RegionOf(hit.position) != region) continue;
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

        #region Play - 리셋하는 진입이면 그 차원문으로 연출 시작 (끝나면 시체를 살리고 Finished). 차원문이 없으면 경고 후 연출 없이 바로 살림. 리셋 안 하면 무시
        /// <param name="portal">맥동할 차원문 (상태가 차원문 캡슐에서 고름. 없으면 null)</param>
        /// <param name="focus">카메라가 볼 기준 (플레이어. 가장 가까운 시체를 봄)</param>
        public void Play(Transform portal, Transform focus)
        {
            if (!IsResetting) return;
            if (portal == null)
            {
                Debug.LogWarning("[Reset] 차원문 없음. 연출 없이 시체를 살림");
                OnSequenceDone();
                return;
            }
            _sequence.Play(portal, _corpses, focus, OnSequenceDone);
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
```

**호출/구독 관계:**
- `ForestResetService.Debug.cs`는 그대로 (`ResetRatio`·`IsResetting`·`_data.Pending`만 씀)
- `ForestEnemyService.CorpseSpawned` → `AddCorpse` 그대로

---

### `Assets/Scripts/Field/Fog/FogMapService.cs` (전체 교체)

바뀐 것: 생성자가 셀 반지름을 받아 지역 가운데에서 그 밖 칸(둥근 숲의 네모 모서리)은 진행도에 안 셈. 진행도 분모는 셀 칸이 있는 구획 수.

```csharp
using System;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 안개 지도 (v1, MonoBehaviour 아님): 지역을 3m 칸으로 나눠 걸으면 반경 24m를 밝힘. 진행도는 60m 구획 단위로, 구획의 셀 칸 중 45%가 밝혀지면 그 구획을 셈. 지역 가운데에서 셀 반지름 밖 칸(둥근 숲의 네모 모서리)은 밝혀도 진행도에 안 셈. 밝힌 칸 배열은 판 데이터에 그대로 남음</summary>
    public sealed class FogMapService
    {
        /// <summary>안개 칸 한 변 (m, v1)</summary>
        public const float CellSize = 3f;

        /// <summary>걸을 때 밝히는 반지름 (m, v1)</summary>
        public const float RevealRadius = 24f;

        /// <summary>진행도 구획 한 변 (m, v1)</summary>
        public const float BlockSize = 60f;

        /// <summary>구획을 셀 밝힌 칸 비율 (v1)</summary>
        public const float BlockThreshold = 0.45f;

        /// <summary>칸마다 밝혔는지 (가로 Width × 세로 Height, 아래 줄부터)</summary>
        readonly bool[] _revealed;

        /// <summary>칸마다 진행도에 세는지 (지역 가운데에서 셀 반지름 안)</summary>
        readonly bool[] _counts;

        /// <summary>구획마다 밝힌 셀 칸 수</summary>
        readonly int[] _blockRevealed;

        /// <summary>구획마다 셀 칸 수</summary>
        readonly int[] _blockCells;

        /// <summary>구획마다 셌는지</summary>
        readonly bool[] _blockCounted;

        /// <summary>셀 칸이 하나라도 있는 구획 수 (진행도 분모)</summary>
        readonly int _blockTotal;

        /// <summary>센 구획 수</summary>
        int _counted;

        /// <summary>지역 (x = 월드 x 최소, y = 월드 z 최소, 너비·높이 m)</summary>
        public Rect Area { get; }

        /// <summary>가로 칸 수</summary>
        public int Width { get; }

        /// <summary>세로 칸 수</summary>
        public int Height { get; }

        /// <summary>진행도 (센 구획 / 셀 칸이 있는 구획, 0~1)</summary>
        public float Progress => _blockTotal > 0 ? (float)_counted / _blockTotal : 0f;

        /// <summary>밝힐 때마다 오름 (지도 그림을 다시 그릴지)</summary>
        public int Version { get; private set; }

        #region FogMapService - 지역·안개 판 데이터·셀 반지름을 받아 앞서 밝힌 칸(없거나 크기가 다르면 새로 만들어 판 데이터에 넣음)으로 구획 수를 셈. 셀 반지름이 0 이하면 칸을 다 셈
        /// <param name="area">지역 (x·y = 월드 x·z 최소)</param>
        /// <param name="data">안개 판 데이터</param>
        /// <param name="countRadius">진행도에 셀 반지름 (m, 지역 가운데에서. 0 이하면 다 셈)</param>
        public FogMapService(Rect area, ForestFogEntry data, float countRadius)
        {
            if (area.width <= 0f || area.height <= 0f) throw new ArgumentException("지역 크기가 0", nameof(area));
            if (data == null) throw new ArgumentNullException(nameof(data));
            Area = area;
            Width = Mathf.CeilToInt(area.width / CellSize);
            Height = Mathf.CeilToInt(area.height / CellSize);
            // 앞서 밝힌 칸 (판 데이터)
            bool[] revealed = data.Revealed;
            _revealed = revealed != null && revealed.Length == Width * Height ? revealed : new bool[Width * Height];
            data.Revealed = _revealed;
            _counts = new bool[Width * Height];
            // 구획 가로·세로 수
            int blocksX = Mathf.CeilToInt(area.width / BlockSize);
            int blocksZ = Mathf.CeilToInt(area.height / BlockSize);
            _blockRevealed = new int[blocksX * blocksZ];
            _blockCells = new int[blocksX * blocksZ];
            _blockCounted = new bool[blocksX * blocksZ];
            // 셀 반지름 제곱 (0 이하면 끝없음)
            float countSq = countRadius > 0f ? countRadius * countRadius : float.MaxValue;
            for (int z = 0; z < Height; z++)
                for (int x = 0; x < Width; x++)
                {
                    // 칸 번호
                    int index = z * Width + x;
                    // 지역 가운데에서 칸 가운데까지 (월드 x·z)
                    Vector2 fromCenter = new Vector2(area.xMin + (x + 0.5f) * CellSize, area.yMin + (z + 0.5f) * CellSize) - area.center;
                    if (fromCenter.sqrMagnitude > countSq) continue;
                    _counts[index] = true;
                    // 이 칸의 구획
                    int block = BlockOf(x, z, blocksX);
                    _blockCells[block]++;
                    if (_revealed[index]) _blockRevealed[block]++;
                }
            for (int b = 0; b < _blockCounted.Length; b++)
            {
                if (_blockCells[b] > 0) _blockTotal++;
                Recount(b);
            }
        }
        #endregion

        #region Reveal - 위치에서 반지름 24m 안 칸을 밝힘 (숲 층이 아니면 안 밝힘: 던전 안). 새로 밝힌 칸이 있으면 Version을 올리고, 셀 칸이면 구획을 다시 셈
        /// <param name="position">월드 위치</param>
        public void Reveal(Vector3 position)
        {
            if (Mathf.Abs(position.y) > FieldService.LevelGap) return;
            // 위치 칸 좌표
            int cx = Mathf.FloorToInt((position.x - Area.xMin) / CellSize);
            int cz = Mathf.FloorToInt((position.z - Area.yMin) / CellSize);
            // 반지름 칸 수
            int reach = Mathf.CeilToInt(RevealRadius / CellSize);
            // 구획 가로 수
            int blocksX = Mathf.CeilToInt(Area.width / BlockSize);
            // 새로 밝힌 칸이 있는지
            bool changed = false;
            for (int z = Mathf.Max(0, cz - reach); z <= Mathf.Min(Height - 1, cz + reach); z++)
                for (int x = Mathf.Max(0, cx - reach); x <= Mathf.Min(Width - 1, cx + reach); x++)
                {
                    // 칸 번호
                    int index = z * Width + x;
                    if (_revealed[index]) continue;
                    // 칸 가운데 월드 x·z
                    float wx = Area.xMin + (x + 0.5f) * CellSize;
                    float wz = Area.yMin + (z + 0.5f) * CellSize;
                    if ((new Vector2(wx - position.x, wz - position.z)).sqrMagnitude > RevealRadius * RevealRadius) continue;
                    _revealed[index] = true;
                    changed = true;
                    if (!_counts[index]) continue;
                    // 이 칸의 구획
                    int block = BlockOf(x, z, blocksX);
                    _blockRevealed[block]++;
                    Recount(block);
                }
            if (changed) Version++;
        }
        #endregion

        #region IsRevealed - 칸을 밝혔는지 (범위 밖은 false)
        /// <param name="x">가로 칸</param>
        /// <param name="z">세로 칸</param>
        public bool IsRevealed(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Height && _revealed[z * Width + x];
        #endregion

        #region ToNormalized - 월드 위치를 지역 안 비율 (0~1, x·z)로
        /// <param name="world">월드 위치</param>
        public Vector2 ToNormalized(Vector3 world) => new((world.x - Area.xMin) / Area.width, (world.z - Area.yMin) / Area.height);
        #endregion

        #region BlockOf - 칸의 구획 번호
        /// <param name="x">가로 칸</param>
        /// <param name="z">세로 칸</param>
        /// <param name="blocksX">구획 가로 수</param>
        static int BlockOf(int x, int z, int blocksX) =>
            Mathf.FloorToInt(z * CellSize / BlockSize) * blocksX + Mathf.FloorToInt(x * CellSize / BlockSize);
        #endregion

        #region Recount - 구획이 45%를 넘었으면 셈 (한 번 세면 안 풀림. 셀 칸이 없는 구획은 안 셈)
        /// <param name="block">구획 번호</param>
        void Recount(int block)
        {
            if (_blockCounted[block] || _blockCells[block] == 0) return;
            if (_blockRevealed[block] < _blockCells[block] * BlockThreshold) return;
            _blockCounted[block] = true;
            _counted++;
        }
        #endregion
    }
}
```

**호출/구독 관계:**
- 부르는 곳은 `FieldState.OnSceneReady` 하나 (`regions.OuterRadius`를 넘김). `FogRevealController`·`MapUIBinder`·`FieldHudUIBinder`는 그대로

---

### `Assets/Scripts/UI/Views/MapView.cs`

**추가/변경 내용:**
- 클래스 summary: `표시 셋은 그림의 자식, 앵커 가운데` → `표시(플레이어·목표·입구·차원문 틀)는 그림의 자식, 앵커 가운데`
- `_exit` 필드 아래, `Exit` 프로퍼티 아래에 추가:

```csharp
        /// <summary>발견한 차원문 표시 틀 (그림 자식, 꺼 둠. 바인더가 복제해서 씀. 비우면 표시 안 함. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _portal;
```

```csharp
        /// <summary>발견한 차원문 표시 틀 (없으면 null)</summary>
        public RectTransform Portal => _portal;
```

---

### `Assets/Scripts/UI/Binders/MapUIBinder.cs`

**추가/변경 내용:**

1. using (맨 위):

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
```

2. 클래스 summary: `지도 칸 ← 안개 지도·플레이어·현재 목표·숲 입구 (출처는` → `지도 칸 ← 안개 지도·플레이어·현재 목표·숲 입구·발견한 차원문 (출처는`

3. `Vector3 _exit;` 필드 아래에 추가:

```csharp
        /// <summary>발견한 차원문 자리를 채우는 함수 (없으면 차원문 표시 안 함)</summary>
        Action<List<Vector3>> _portals;

        /// <summary>차원문 자리 (매 프레임 채워 씀)</summary>
        readonly List<Vector3> _portalPositions = new();

        /// <summary>만든 차원문 표시 (틀을 복제, 늘기만 함)</summary>
        readonly List<RectTransform> _portalMarkers = new();
```

4. `Bind` region 전체 교체:

```csharp
        #region Bind - 안개 지도·플레이어·목표·입구·발견한 차원문을 받고 그림을 (크기가 다르면 새로) 만들어 지도를 보임
        /// <param name="fog">안개 지도</param>
        /// <param name="player">플레이어</param>
        /// <param name="objective">현재 목표 위치 (없으면 null을 돌려주는 함수, 함수가 null이면 표시 안 함)</param>
        /// <param name="exit">숲 입구 위치</param>
        /// <param name="portals">발견한 차원문 자리를 채우는 함수 (null이면 표시 안 함)</param>
        public void Bind(FogMapService fog, Transform player, Func<Vector3?> objective, Vector3 exit, Action<List<Vector3>> portals)
        {
            _fog = fog ?? throw new ArgumentNullException(nameof(fog));
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _objective = objective;
            _exit = exit;
            _portals = portals;
            if (_texture == null || _texture.width != fog.Width || _texture.height != fog.Height)
            {
                if (_texture != null) Destroy(_texture);
                _texture = new Texture2D(fog.Width, fog.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _pixels = new Color32[fog.Width * fog.Height];
            }
            _drawnVersion = -1;
            _shownPercent = -1;
            if (_view == null) return;
            _view.Image.texture = _texture;
            ShowBound(true);
        }
        #endregion
```

5. `Unbind`: `_objective = null;` 아래에 `_portals = null;`

6. `PlaceMarkers` region: 설명을 `... 플레이어(방향 포함)·목표·입구·발견한 차원문 표시를 옮김. 범위 밖 표시는 끔`으로, 메서드 끝(`PlaceOptional(_view.Exit, _exit, origin, window);` 아래)에 추가:

```csharp
            PlacePortals(origin, window);
```

7. `PlaceOptional` region 아래에 새 region:

```csharp
        #region PlacePortals - 발견한 차원문 자리마다 표시를 옮김 (모자라면 틀을 복제해 늘리고, 남는 표시는 끔. 틀이 없으면 안 함)
        /// <param name="origin">보는 범위 왼쪽 아래</param>
        /// <param name="window">보는 범위 크기</param>
        void PlacePortals(Vector2 origin, Vector2 window)
        {
            // 표시 틀 (꺼 둔 채로 둠)
            RectTransform template = _view.Portal;
            if (template == null) return;
            _portalPositions.Clear();
            _portals?.Invoke(_portalPositions);
            while (_portalMarkers.Count < _portalPositions.Count) _portalMarkers.Add(Instantiate(template, template.parent));
            for (int i = 0; i < _portalMarkers.Count; i++)
            {
                if (i < _portalPositions.Count) Place(_portalMarkers[i], _fog.ToNormalized(_portalPositions[i]), origin, window);
                else _portalMarkers[i].gameObject.SetActive(false);
            }
        }
        #endregion
```

**호출/구독 관계:**
- `Bind`를 부르는 곳: `FieldState.OnSceneReady` (미니맵), `UIManager.SetMapSource` (메뉴 지도). 둘 다 `PortalService.CopyFound`를 넘김
- 복제한 표시는 꺼진 틀을 복제해서 꺼진 채 생기고, `Place`가 범위 안일 때만 켬

---

### `Assets/Scripts/Managers/UIManager.cs`

**추가/변경 내용:** `SetMapSource` region 전체 교체

```csharp
        #region SetMapSource - 메뉴 창 지도 탭에 안개 지도·플레이어·목표·입구·발견한 차원문을 넘김 (숲이 부름)
        /// <param name="fog">안개 지도</param>
        /// <param name="player">플레이어</param>
        /// <param name="objective">현재 목표 위치 (없으면 null을 돌려줌)</param>
        /// <param name="exit">숲 입구 위치</param>
        /// <param name="portals">발견한 차원문 자리를 채우는 함수</param>
        public void SetMapSource(FogMapService fog, Transform player, Func<Vector3?> objective, Vector3 exit, Action<List<Vector3>> portals)
        {
            if (_menuMap != null) _menuMap.Bind(fog, player, objective, exit, portals);
        }
        #endregion
```

(`using System;`·`using System.Collections.Generic;`은 이미 있음)

---

### `Assets/Scripts/UI/Views/FieldView.cs`

**추가/변경 내용:**

1. 필드: `_entry`부터 `_zoneCount`까지 교체

변경 전:

```csharp
        /// <summary>들어오면 서는 곳 (숲 입구 안쪽, 숲을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _entry;

        /// <summary>죽은 뒤 용병이 도망치는 곳 (숲 입구 표지판 앞. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _exitPoint;

        /// <summary>차원문 (자리는 판 데이터. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _portal;

        /// <summary>차원문을 놓을 수 있는 곳 (3구역 중 전초기지에서 먼 쪽 절반, 월드)</summary>
        [SerializeField] Bounds _portalArea = new(new Vector3(0f, 0f, 660f), new Vector3(160f, 0f, 110f));

        /// <summary>안개 지도 지역 (x·y = 월드 x·z 최소, m)</summary>
        [SerializeField] Rect _mapArea = new(-90f, 0f, 180f, 720f);

        /// <summary>구역 하나 깊이 (m. 입구에서 z로 잼)</summary>
        [SerializeField, Min(1f)] float _zoneDepth = 240f;

        /// <summary>구역 수</summary>
        [SerializeField, Min(1)] int _zoneCount = 3;
```

변경 후:

```csharp
        /// <summary>들어오면 서는 곳 (숲 가운데 전초기지 정문 밖, 숲을 봄. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _entry;

        /// <summary>죽은 뒤 용병이 도망치는 곳 (닫힌 정문 앞. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _exitPoint;

        /// <summary>차원문 표지 (Portals 자식. 자리는 차원문 캡슐이 정함. 인스펙터에서만 연결)</summary>
        [SerializeField] PortalView[] _portals;

        /// <summary>안개 지도 지역 (x·y = 월드 x·z 최소, m. 지역 가운데에 맞춘 정사각형)</summary>
        [SerializeField] Rect _mapArea = new(-210f, -210f, 420f, 420f);

        /// <summary>지역 가운데 (전초기지 가운데, 월드)</summary>
        [SerializeField] Vector3 _regionCenter = Vector3.zero;

        /// <summary>1지역이 시작하는 반지름 (m, 전초기지 성벽 바깥)</summary>
        [SerializeField, Min(0f)] float _regionInner = 30f;

        /// <summary>지역 하나 폭 (m)</summary>
        [SerializeField, Min(1f)] float _regionWidth = 60f;

        /// <summary>지역 수</summary>
        [SerializeField, Min(1)] int _regionCount = 3;
```

2. 프로퍼티: `Portal`·`PortalArea`·`ZoneDepth`·`ZoneCount` 교체 (`MapArea`는 그대로 두고 그 아래에 지역 넷)

변경 전:

```csharp
        /// <summary>차원문</summary>
        public Transform Portal => _portal;

        /// <summary>차원문을 놓을 수 있는 곳</summary>
        public Bounds PortalArea => _portalArea;

        /// <summary>안개 지도 지역</summary>
        public Rect MapArea => _mapArea;

        /// <summary>구역 하나 깊이 (m)</summary>
        public float ZoneDepth => _zoneDepth;

        /// <summary>구역 수</summary>
        public int ZoneCount => _zoneCount;
```

변경 후:

```csharp
        /// <summary>차원문 표지</summary>
        public PortalView[] Portals => _portals;

        /// <summary>안개 지도 지역</summary>
        public Rect MapArea => _mapArea;

        /// <summary>지역 가운데</summary>
        public Vector3 RegionCenter => _regionCenter;

        /// <summary>1지역이 시작하는 반지름 (m)</summary>
        public float RegionInner => _regionInner;

        /// <summary>지역 하나 폭 (m)</summary>
        public float RegionWidth => _regionWidth;

        /// <summary>지역 수</summary>
        public int RegionCount => _regionCount;
```

---

### `Assets/Scripts/Core/RunEntry.cs`

**추가/변경 내용:** `ForestReset` 프로퍼티 아래에 추가

```csharp

        /// <summary>발견한 차원문 (PortalService만 만짐)</summary>
        public PortalEntry Portal { get; } = new();
```

---

### `Assets/Scripts/Flow/FieldState.cs`

**추가/변경 내용:**

1. 클래스 summary 교체:

```csharp
    /// <summary>숲. 전초기지를 둘러싼 원형 3지역 회색 숲에 마법사·용병·적(쓰러뜨린 적은 쓰러진 자리에 시체), 지역마다 차원문(24m 안에 가면 발견), 1지역 고블린 던전(지하, 보스)을 두고 현장 시전·안개 지도·시계·12시 안내(전투 중엔 숨김)를 켬. 숲이 70% 넘게 빈 채 나갔으면 들어올 때 가장 가까운 차원문의 리셋 연출 뒤 시체가 일어남. 숲 가운데 전초기지 정문에서 F로 귀환, 소탕 대상을 다 쓰러뜨리면 의뢰 목표 달성, 기간이 지나면 의뢰 실패. 죽으면 금화를 떨구고 덮개 밑에서 용병 도망을 빠르게 돌린 뒤 여관 방 침대(방이 없으면 정문)에서 부활. 숲 적·의뢰·용병·던전·숲 리셋·차원문·떨어진 금화·임무 일지·코덱스는 캡슐이 맡고, 여기선 캡슐을 만들어 이음 (토스트·금화·목표·튜토리얼·화면 전환·시전 소리·벌칙·조작 잠금)</summary>
```

2. 상수: `ExitId` 주석만 `/// <summary>정문 지점 id (귀환. 숲 가운데 닫힌 정문)</summary>`. `DeathMaxSeconds` 아래와 `StashEmptyKey` 아래에 추가:

```csharp
        /// <summary>차원문 발견을 확인하는 간격 (초, 게임 시간이라 일시정지·로딩창엔 멈춤)</summary>
        const float PortalCheckSeconds = 0.5f;
```

```csharp
        /// <summary>차원문을 발견했을 때 토스트 키 ({0} 지역 번호)</summary>
        const string PortalFoundKey = "field.portal_found";
```

3. 필드: `_reset` 주석을 `/// <summary>숲 리셋 캡슐</summary>`로, 그 아래와 `_death` 아래에 추가:

```csharp
        /// <summary>차원문 캡슐</summary>
        PortalService _portals;
```

```csharp
        /// <summary>차원문 발견 확인 코루틴 (GameManager가 돌림. 없으면 null)</summary>
        Coroutine _portalCheck;
```

4. `OnSceneReady` region 전체 교체:

```csharp
        #region OnSceneReady - 현장 규칙(보관 장소 = 던전 입구 밖)·숲 지역·안개 지도(둥근 숲 안만 진행도)와 캡슐(의뢰·임무 일지·숲 리셋·차원문·숲 적·용병·던전·떨어진 금화·코덱스)을 만들어 잇고(숲 적 → 의뢰 소탕 대상·리셋 시체, 차원문 발견 → 토스트, 몬스터가 보관한 금화 → 던전, 코덱스 등록 → 토스트, 시전 성공·실패 → 소리·벌칙, 시전 조작 잠금·방호막 → 플레이어·몸·감지기, 메뉴 임무 일지·주문 도감·지도에 출처), 차원문을 놓고, 마법사를 입구에, 용병·숲 적·던전·떨어진 금화를 놓고, 시전·안개·시계·HUD·지도(발견한 차원문 포함)를 연결, 목표를 정함 (덮개 밑)
        protected override void OnSceneReady()
        {
            _view = FindView<FieldView>();
            if (_view == null) return;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            // 숲 지역 (전초기지 둘레 원형)
            ForestRegionSpec regions = Regions();
            _service = new FieldService(_view.PlayerUnit) { StashPoint = _view.Dungeon.Outside };
            _fog = new FogMapService(_view.MapArea, run.Fog, regions.OuterRadius);
            _quest = new FieldQuestService(run.Quest);
            _quest.Cleared += OnQuestCleared;
            _quest.Expired += OnQuestExpired;
            _journal = new JournalService(run.Journal);
            _reset = new ForestResetService(run.ForestReset, _view.ResetSequence, _service, regions, _view.Entry.position);
            _reset.Finished += OnResetFinished;
            _portals = new PortalService(run.Portal, _view.Portals, regions, _view.Entry.position);
            _portals.Found += OnPortalFound;
            _forest = new ForestEnemyService(run.Forest, _view.SpawnPoints, _view.EnemyPrefab, _service);
            _forest.EnemySpawned += _quest.Track;
            _forest.EnemyDied += _quest.Untrack;
            _forest.CorpseSpawned += _reset.AddCorpse;
            _party = new FieldPartyService(run.Party, _view.MercenaryPrefab, _service, _view.Player.transform);
            _party.Lost += OnMercenaryLost;
            _dungeon = new DungeonService(run.Dungeon, _view.Dungeon, _service);
            _dungeon.Cleared += OnDungeonCleared;
            _loot = new FieldLootHandler(run.Loot, _service, _view.GoldPilePrefab);
            _service.LootStored += _dungeon.AddStash;
            _portals.Place();
            _view.Player.PlaceAt(_view.Entry);
            _view.PlayerUnit.Init(PlayerMaxHp);
            _party.Spawn(_view.Entry);
            _forest.Spawn();
            _dungeon.Spawn();
            _loot.Spawn();
            _codex = new CodexService(run.Codex, DataManager.Instance.Spells, DataManager.Instance.Runes);
            _codex.Registered += OnSpellRegistered;
            _view.Cast.Init(_codex, _service, _view.PlayerUnit);
            _view.Cast.CastAccepted += OnCastAccepted;
            _view.Cast.CastFailed += OnCastFailed;
            _view.Cast.ControlLockChanged += OnCastControlLock;
            _view.Cast.InteractLockChanged += OnCastInteractLock;
            _view.Cast.ShieldChanged += OnCastShieldChanged;
            _view.FogReveal.Init(_fog);
            _view.Clock.Init(run.Clock);
            _view.HudBinder.Bind(_view.PlayerUnit, _view.Player, _view.Cast, _party.Mercenaries, _service, _fog, RegionAt, () => _view.Clock.Day);
            _view.Hud.Minimap.Bind(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position, _portals.CopyFound);
            UIManager.Instance.SetMapSource(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position, _portals.CopyFound);
            UIManager.Instance.SetJournalSource(JournalNow);
            UIManager.Instance.SetCodexSource(_codex, run.PlayerName);
            _view.Hud.InteractPrompt.gameObject.SetActive(false);
            if (!_reset.IsResetting) _quest.SettleIfEmpty();
            _objectiveChanged = UpdateObjective(false);
        }
        #endregion
```

5. `Reveal` region 전체 교체:

```csharp
        #region Reveal - 커서 잠금, 지점·죽음·전투 상황 구독, 시계 → 의뢰 기한, M 구독, 차원문 발견 확인 시작. 리셋이면 조작·HUD를 끄고 마법사에서 가장 가까운 차원문으로 연출 → 끝나면 OnResetFinished, 아니면 바로 조작 시작 (로딩창을 넘길 때 누른 키는 구독 전이라 안 들어감)
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.PlayerUnit.Died += OnPlayerDied;
            _service.CombatChanged += OnCombatChanged;
            _view.Clock.Ticked += _quest.Tick;
            _map = InputSystem.actions.FindAction(MapActionPath);
            if (_map != null) _map.performed += OnMapPressed;
            else Debug.LogError($"[Field] {MapActionPath} 액션 없음. 지도 튜토리얼을 못 배움");
            _portalCheck = Game.StartCoroutine(PortalCheckRoutine());
            if (_reset.IsResetting)
            {
                SetControl(false);
                _view.HudCanvas.enabled = false;
                _reset.Play(_portals.Nearest(_view.Player.transform.position), _view.Player.transform);
                return;
            }
            StartControl();
        }
        #endregion
```

6. `Exit`: region 설명 앞부분을 `도망 시뮬·차원문 확인을 멈추고 게임 속도를 되돌림, ...`으로, 도망 코루틴을 멈추는 `if` 블록 아래에 추가:

```csharp
            if (_portalCheck != null)
            {
                Game.StopCoroutine(_portalCheck);
                _portalCheck = null;
            }
```

7. `Unwire`: `if (_reset != null) _reset.Finished -= OnResetFinished;` 아래에 추가:

```csharp
            if (_portals != null) _portals.Found -= OnPortalFound;
```

8. `OnInteracted` region 설명: `숲 입구면 귀환` → `정문이면 귀환`. `Return` region 설명: `숲 입구로 귀환해 전초기지로 (정문 밖에서 시작)` → `정문에서 귀환해 전초기지로 (정문 밖에서 시작)`

9. `ZoneAt`·`Zones` region 교체:

```csharp
        #region RegionAt - 플레이어가 있는 지역 번호 (전초기지 가운데에서 잰 거리, 1~지역 수)
        int RegionAt() => Regions().RegionOf(_view.Player.transform.position);
        #endregion

        #region Regions - 숲 화면 값(지역 가운데·1지역 반지름·지역 폭·지역 수)으로 만든 숲 지역
        ForestRegionSpec Regions() => new(_view.RegionCenter, _view.RegionInner, _view.RegionWidth, _view.RegionCount);
        #endregion
```

10. `OnResetFinished` region 아래에 새 region 둘:

```csharp
        #region PortalCheckRoutine - 0.5초(게임 시간)마다 마법사 자리로 차원문 발견을 확인 (Exit에서 멈춤, 씬이 내려가면 끝)
        IEnumerator PortalCheckRoutine()
        {
            // 확인 간격
            var wait = new WaitForSeconds(PortalCheckSeconds);
            while (_view != null)
            {
                yield return wait;
                if (_view == null) yield break;
                _portals.Check(_view.Player.transform.position);
            }
        }
        #endregion

        #region OnPortalFound - 차원문을 발견함 (차원문 캡슐이 자리를 남김): 토스트
        /// <param name="region">발견한 차원문 지역 번호</param>
        void OnPortalFound(int region) => UIManager.Instance.ShowToast(Table, PortalFoundKey, region);
        #endregion
```

**호출/구독 관계:**
- `_portals.Found` → `OnPortalFound` (Unwire에서 해제), `PortalCheckRoutine`은 `Game.StartCoroutine` (Exit에서 `StopCoroutine`)
- HUD `FieldHudUIBinder.Bind`의 `zoneAt` 칸에 `RegionAt`을 넘김 (HUD 쪽은 안 바뀜)

---

### `Assets/Scripts/Flow/FieldState.Debug.cs`

**추가/변경 내용:**

1. 클래스 summary의 `파티 옮기기` → `파티 옮기기(차원문 근처 포함)`

2. 상수 추가 (`DebugKeepTargets` 아래):

```csharp
        /// <summary>안 찾은 차원문에서 띄워 세울 거리 (m, 발견 24m 밖)</summary>
        const float DebugPortalGap = 30f;
```

3. `DebugTargets`: 설명의 `숲 리셋·떨어진 금화` → `숲 리셋·차원문·떨어진 금화`, `yield return _reset;` 아래에 `yield return _portals;`

4. `DebugWhere` region 교체:

```csharp
        #region DebugWhere - 마법사가 있는 곳 ("숲 N지역" 또는 "던전 안")
        [DebugControl("숲", "위치")]
        string DebugWhere => FieldService.SameLevel(_view.Player.transform.position, _view.Entry.position)
            ? string.Format(CultureInfo.InvariantCulture, "숲 {0}지역", RegionAt())
            : "던전 안";
        #endregion
```

5. `DebugMoveAboveRoomA` region 아래에 새 region:

```csharp
        #region DebugMoveNearPortal - 마법사·용병을 가장 가까운 안 찾은 차원문의 전초기지 쪽 30m 앞으로 (덮개 페이드, 차원문을 봄. 걸어가면 발견)
        [DebugControl("숲", "안 찾은 차원문 근처로")]
        string DebugMoveNearPortal()
        {
            if (DebugBusy) return "지금은 안 됨";
            // 가장 가까운 안 찾은 차원문 자리
            Vector3? hidden = _portals.DebugNearestHidden(_view.Player.transform.position);
            if (hidden == null) return "다 발견함";
            // 차원문 → 지역 가운데 평면 방향
            Vector3 toCenter = Vector3.ProjectOnPlane(_view.RegionCenter - hidden.Value, Vector3.up).normalized;
            return DebugMove(DebugPointAt(hidden.Value + toCenter * DebugPortalGap, -toCenter), "안 찾은 차원문 30m 앞으로 (걸어가면 발견)");
        }
        #endregion
```

---

## Prefab 변경

### `Assets/Prefabs/World/Portal.prefab`

1. 루트 `Portal`: Add Component → `PortalView` (`_id` 비움, `_region` 1. 씬 인스턴스에서 정함)

### `Assets/Prefabs/World/OutpostWalls.prefab` (새로)

Outpost 씬 1~3번에서 만듦 (아래 씬 변경). 루트 `OutpostWalls` (원점) 아래 `Walls`(Cube 5개, 높이 5, 콜라이더 둠), `Gate`(PostLeft·PostRight·Lintel), `Towers`(`Tower.prefab` 중첩 4개). Forest 씬도 이 프리팹을 씀

---

## 씬 변경

### `Assets/Scenes/Outpost.unity`

1. 빈 오브젝트 `OutpostWalls` (원점) 새로 → 루트의 `Walls`·`Gate`·`Towers`를 자식으로 (월드 자리 그대로)
2. `OutpostWalls/Walls` 자식 높이 3 → 5:
   - North: 위치 (0, 2.5, 20), 스케일 (40, 5, 0.5)
   - East: (20, 2.5, 0), (0.5, 5, 40)
   - West: (-20, 2.5, 0), (0.5, 5, 40)
   - SouthLeft: (-11.5, 2.5, -20), (17, 5, 0.5)
   - SouthRight: (11.5, 2.5, -20), (17, 5, 0.5)
3. `PrefabUtility.SaveAsPrefabAssetAndConnect(OutpostWalls, "Assets/Prefabs/World/OutpostWalls.prefab", InteractionMode.AutomatedAction)` (Towers의 `Tower.prefab`은 중첩 프리팹으로 남음)
4. `Trees` 자식 중 위치 z ≥ -20 인 `Tree.prefab` 인스턴스 지움 (정문 쪽 z < -20만 남김)
5. `GateYard` (원점) 새로, 아래 Cube 3개 (MeshRenderer 끔, BoxCollider 둠): `West` (-20.5, 3, -40) 스케일 (1, 6, 40), `East` (20.5, 3, -40) 스케일 (1, 6, 40), `South` (0, 3, -60) 스케일 (42, 6, 1)
6. 저장 → 다시 열어 `OutpostWalls` 프리팹 연결·성벽 높이 5·남은 나무가 다 z < -20·`GateYard` 콜라이더 3개, HubView 칸이 그대로인지 확인

### `Assets/Scenes/Forest.unity`

1. `Forest/Ground`: 위치 (0, -0.5, 0), 스케일 (440, 1, 440)
2. `Forest/Bounds`: 자식 North·South·East·West 지움 → Cube 64개 `Edge00`~`Edge63` 새로. i번째 각도 θ = i × 5.625°, 위치 (212 sin θ, 3, 212 cos θ), 회전 (0, θ, 0), 스케일 (22, 6, 2), MeshRenderer 끔 (BoxCollider 둠)
3. `Forest/OutpostWalls`: `OutpostWalls.prefab` 인스턴스 (원점)
4. `Forest/GateDoor` 새로: Cube (0, 2.5, -20) 스케일 (6, 5, 0.6) `Wood.mat`, BoxCollider 둠. Add Component → `InteractPointView` (`_id` = `forest_exit`, `_label` = Field/`field.exit`)
5. `Forest/Road` 새로: Cube (0, 0.025, -115) 스케일 (4, 0.05, 190) `Road.mat`
6. `Forest/DungeonEntrance`: 위치 (-70, 0, 130) → (-61, 0, 35), 회전 (0, 300, 0) (입구가 전초기지 가운데 쪽). 자식은 그대로
7. `Spawns`: 자식 순서·id·몬스터·수·흩음은 그대로, 위치만 바꿈

| id | 지역 | 위치 |
|---|---|---|
| goblin_a | 1 | (-52, 0, -30) |
| goblin_b | 1 | (56, 0, 33) |
| goblin_c | 1 | (-28, 0, 48) |
| wolf_a | 1 | (61, 0, -35) |
| wolf_b | 1 | (0, 0, 75) |
| boar_a | 1 | (-35, 0, -61) |
| boar_b | 1 | (26, 0, -70) |
| spider_a | 2 | (58, 0, 100) |
| spider_b | 2 | (60, 0, -104) |
| spider_c | 2 | (-110, 0, 0) |
| spider_d | 2 | (-43, 0, -117) |
| bear_a | 2 | (125, 0, 0) |
| bear_b | 2 | (-60, 0, 104) |
| mutant_a | 3 | (0, 0, 180) |
| mutant_b | 3 | (152, 0, -88) |
| mutant_c | 3 | (-160, 0, -93) |
| mutant_d | 3 | (-147, 0, 85) |
| wolf_c | 3 | (160, 0, 93) |
| wolf_d | 3 | (-49, 0, -184) |

8. `Forest/Trees`: 자식을 다 지우고 `Tree.prefab` 인스턴스를 새로 놓음. `System.Random(4201)`로 지역마다 그루 수만큼: 1지역(반지름 30~90) 120, 2지역(90~150) 280, 3지역(150~210) 490. 한 그루마다 최대 30번 뽑음: 각도 0~360° 고르게, 반지름 √(안쪽² ~ 바깥²) 고르게, 크기 0.8~1.3배, yaw 0~360°. 안 놓는 곳: 가운데 32m 안, 길 통로(|x| < 8 이고 z < -20), 입구 자리 (0, -28)에서 12m 안, 적 자리 8m 안, 던전 입구 (-61, 35)에서 12m 안, 가운데에서 208m 밖, 다른 나무 3m 안 (30번 다 안 되면 그 그루는 건너뜀)
9. `Forest/Rocks`: 자식을 다 지우고 Cube 30개 (지역마다 10개, 같은 `System.Random`을 이어서), 스케일 x·z 2~5·y 1.5~3, 위치 y = 높이 / 2, yaw 0~360°, `Road.mat`. 안 놓는 곳은 나무와 같고 나무·다른 바위 4m 안도 뺌
10. `Entrance/Signpost` 지움. `Entrance/EntryPoint` (0, 0, 10) yaw 0 → (0, 0, -28) yaw 180 (숲을 봄), `Entrance/ExitPoint` (0, 0, 6) → (0, 0, -24)
11. 루트 `Portal` 인스턴스 지움 → `Portals` (원점) 새로, 아래 `Portal.prefab` 인스턴스 5개. 자리·방향은 들어올 때 차원문 캡슐이 정함 (씬 자리는 편집용):

| 이름 = `_id` | `_region` | 씬 자리 |
|---|---|---|
| portal_1 | 1 | (30, 0, -52) |
| portal_2a | 2 | (0, 0, 120) |
| portal_2b | 2 | (-104, 0, -60) |
| portal_3a | 3 | (180, 0, 0) |
| portal_3b | 3 | (-180, 0, 0) |

12. `Player`: 위치 (0, 0, 10) → (0, 0, -28), yaw 180
13. `Canvas/Hud/Minimap/Map/Portal` 새로: Image (0.75, 0.45, 1) 14 × 14, 앵커·피벗 가운데, Raycast Target 끔, 꺼 둠. Minimap의 MapView `_portal` → Map/Portal
14. `Field` FieldView: `_entry` → Entrance/EntryPoint, `_exitPoint` → Entrance/ExitPoint, `_portals` = Portals 자식 5개 (위 순서), `_mapArea` (-210, -210, 420, 420), `_regionCenter` (0, 0, 0), `_regionInner` 30, `_regionWidth` 60, `_regionCount` 3
15. `Forest` NavMeshSurface Bake → `Assets/Scenes/Forest/NavMesh-Forest.asset` (덮어씀). `Dungeon` NavMesh는 그대로
16. 저장 → 다시 열어 FieldView 새 칸, PortalView 5개 id·지역, GateDoor 지점 id·이름 키, Minimap MapView `_portal`, NavMesh 데이터 연결 확인. 콘솔 에러 0

### `Assets/Scenes/Boot.unity`

1. `Managers/UIManager/UICanvas/GameMenu/Panel/Pages/MapPage/Map`: 크기 (170, 680) → (640, 640) (위치 (0, 20) 그대로)
2. `Map/Image/Portal` 새로: Image (0.75, 0.45, 1) 14 × 14, 앵커·피벗 가운데, Raycast Target 끔, 꺼 둠. Map의 MapView `_portal` → Map/Image/Portal
3. 저장 → 다시 열어 MapView `_portal`·Map 크기 확인

---

## 다국어 변경

1. 시트 `Field`·`Objective`·`Quest`·`Tip` 탭에서 고침 (키는 그대로, ko·en만. Comment만 바꾸는 줄은 표시)

**`Field`**

| Key | Comment | ko | en |
|---|---|---|---|
| `field.zone.1` | 1지역 이름 | 1지역 | Region 1 |
| `field.zone.2` | 2지역 이름 | 2지역 | Region 2 |
| `field.zone.3` | 3지역 이름 | 3지역 | Region 3 |
| `field.zone_line` (Comment만) | 숲 HUD 왼쪽 위. {0} 지역 이름, {1} 날짜, {2} 지도 % | (그대로) | (그대로) |
| `field.exit` (Comment만) | 정문 지점 이름 (숲 가운데 닫힌 정문, 귀환) | (그대로) | (그대로) |
| `field.quest_cleared` | 토스트: 소탕 목표 달성. {0} 의뢰 이름 | 목표 달성: {0}. 정문으로 돌아가자 | Goal complete: {0}. Head back to the gate |
| `field.portal_found` (새) | 토스트: 차원문 발견. {0} 지역 번호 | 차원문을 발견했다 ({0}지역) | Found a portal (Region {0}) |

**`Objective`**

| Key | Comment | ko | en |
|---|---|---|---|
| `objective.forest_clear.title` | 목표 이름 | 1지역의 고블린 소탕 | Clear the Goblins in Region 1 |
| `objective.forest_clear.desc` | 목표 설명 | 1지역의 고블린을 모두 쓰러뜨린다. 의뢰를 못 끝내도 정문에서 돌아올 수 있다. | Defeat every goblin in Region 1. You can return at the gate even if you haven't finished. |
| `objective.forest_return.title` | 목표 이름 | 정문으로 돌아가기 | Return to the Gate |
| `objective.forest_return.desc` | 목표 설명 | 고블린을 모두 쓰러뜨렸다. 정문에서 F로 돌아가면 의뢰가 끝난다. | All the goblins are down. Return at the gate with F to finish the quest. |

**`Quest`**

| Key | Comment | ko | en |
|---|---|---|---|
| `quest.forest_01.title` | 첫 의뢰 이름 (UI 탭에서 옮김) | 1지역 정리 (임시) | Clear Region 1 (temp) |
| `quest.forest_01.desc` | 첫 의뢰 설명 (임시) | 1지역에 나타난 고블린을 몰아내 줘. | Drive off the goblins that appeared in Region 1. |

**`Tip`**

| Key | Comment | ko | en |
|---|---|---|---|
| `tip.return.body` | 로딩창 팁: 귀환 본문 | 정문에서 F를 누르면 전초기지로 돌아가 | Press F at the gate to return to the outpost |

2. CSV: Field·Objective·Quest·Tip → `Assets/Localization/CSV/` 덮어씀 → `Tools > PaulowniaSky > Localization Kit > Import Tables from CSV` → `Check Missing Translations`·`Check Missing Glyphs`

---

## 주의사항

- 키 이름(`field.zone.*`·`field.zone_line`)은 그대로 둠: CSV에 없는 키는 테이블에서 안 지워져서 이름을 바꾸면 옛 키가 남음. HUD 코드(`FieldHudUIBinder`·`FieldHudView`)의 zone 이름도 그대로 (구역 번호 자리에 지역 번호가 들어감)
- 구현 임시: 지역 30m부터 폭 60m × 3 (바깥 210m), 발견 24m, 입구 40m·차원문끼리 60m·경계 8m 띄움, 안 찾은 차원문은 숲에 들어올 때마다 다시 뽑음 (찾은 것만 `PortalEntry.Found`에 남음), 리셋 연출은 마법사에서 가장 가까운 차원문 하나 (발견 여부 무관)
- `PortalService.Place`는 숲 NavMesh가 있어야 자리를 길 위로 맞춤 (`OnSceneReady` = 씬 로드 뒤라 있음). `Portal.prefab`의 NavMeshObstacle(Carve)이 옮긴 자리에서 길을 깎음
- 발견 확인 코루틴은 `WaitForSeconds`(게임 시간)라 메뉴·로딩창·대화 중엔 멈춤. `Exit`에서 꼭 멈춤
- 리셋인데 차원문이 하나도 없으면 연출 없이 바로 살림 (`OnSequenceDone` → `Finished`가 `Reveal` 안에서 바로 불림)
- 안개 판 데이터 칸 수가 60 × 240 → 140 × 140으로 바뀌어 앞서 밝힌 안개는 새로 시작함 (`FogMapService`가 크기가 다르면 새로 만듦)
- 숲 적 자리 id는 그대로 두고 자리만 옮김. 앞 판의 쓰러진 기록(`ForestEntry.Dead`)은 옛 좌표라 새 숲에선 엉뚱한 곳에 시체 → 새 게임으로 확인
- Forest NavMeshSurface는 Current Object Hierarchy라 `OutpostWalls`·`GateDoor`·`Road`·`Bounds`는 `Forest` 아래에 둬야 구울 때 들어감
- `OutpostWalls` 프리팹을 고치면 두 씬에 같이 들어감. 숲 쪽 닫힌 정문(`GateDoor`)은 프리팹 밖 (숲 씬에만)
- 전초기지 역마차는 앞마당 남쪽 경계(z -60)를 지나 들어오지만 애니메이션이라 안 막힘. 하차 자리 (2.2, 0, -27)·귀환 자리 (2.2, 0, -23.5)는 앞마당 안
- 미니맵 표시 틀은 `Map`(RawImage) 자식이어야 `Place`가 그림 가운데 기준으로 맞춤. 메뉴 지도는 `Map/Image` 자식

---

## 진행 추적

- [x] 묶음 Logic — ForestRegionSpec(ForestZoneSpec 지움), PortalEntry·PortalView·PortalService·PortalService.Debug, ForestResetEntry·ForestResetService, FogMapService, MapView·MapUIBinder·UIManager, FieldView, RunEntry, FieldState·FieldState.Debug. MSBuild(Mage.csproj 복사본에 새 파일 넣어서) 에러·C# 경고 0. Unity 콘솔 확인은 Compile 묶음
- [x] 묶음 Prefab — Portal.prefab에 PortalView (`_id` 빔, `_region` 1), OutpostWalls.prefab (Walls·Gate·Towers, Tower 중첩 프리팹)
- [x] 묶음 Editor-Scene — Outpost(성벽 5m, OutpostWalls 연결·오버라이드 없음, 나무 22그루 지움·18그루 남음, GateYard 콜라이더 3), Forest(Ground 440, 경계 64개, OutpostWalls, GateDoor `forest_exit`·Field/`field.exit`, Road, 던전 입구, 적 자리 19, 나무 890·바위 30, 입구·도망 자리, 차원문 5개, Player, 미니맵 틀, FieldView 칸, NavMesh-Forest 다시 굽기·GUID 그대로), Boot(지도 640 × 640, Image/Portal 틀), 저장 뒤 다시 열어 확인. NavMesh: 입구에서 숲 바깥까지 길 이어짐, 성 안은 안 이어짐, 경계 밖 길 없음
- [x] 묶음 Localization — 시트 Field·Objective·Quest·Tip 고침(읽어서 대조), CSV 4개(시트 값으로, 기존 형식 그대로), Import(17개, 키 수 Unity=CSV), 빠진 번역 ko·en 0, 빠진 글자 없음
- [x] 묶음 Compile — 콘솔 에러·경고 0. Play로 숲 들어가 봄: 차원문 5개가 자기 지역·전초기지 가운데를 봄·입구 40m·서로 60m 밖, portal_1 10m 앞으로 옮기면 발견 1·미니맵 표시·토스트, HUD "1지역 · 1일째 · 지도 0%"
- [x] 묶음 Play 확인 (Claude가 Play 모드로, 2026-11-27) — 확인 목록 1-1~8-1 통과. 2-1은 경비병 대화 대신 상태 전환으로 출발, 8-1은 방 있는 판만. 디버그 패널에 '의뢰/소탕 끝냄'이 켜진 채 기억돼 있으면 숲에 들어올 때 의뢰가 끝난 상태라 6-1 전에 꺼야 함

구현 중 plan과 다르게 한 것 (2026-11-27):
- `Dungeon/ExitDoor` z -5.9 → -5.8: 앞면이 WallSouth 앞면(z -5.8)과 같은 면이라 레이가 벽에 먼저 맞아 [F] 안내가 안 떴음 (이번 변경 전부터 있던 던전 배치). 벽 앞으로 0.1m 내서 "[F] 동굴 밖으로" → F → 동굴 앞 확인. 던전 NavMesh는 벽에서 이미 깎인 범위라 다시 안 구움

---

## 확인 목록 (사용자, 에디터 Play)

상황 만들기는 디버그 패널 (`Tools > Mage > 디버그 패널`, Play 중). [ ] 안은 패널 조작, '…'은 패널 값 줄.

1. 전초기지
   1-1. 새 게임 → 광장에서 사방을 둘러봄 → 성벽 너머로 아무것도 안 보이고, 정문 쪽만 길·나무가 보임
   1-2. 정문 밖으로 나가 좌우·남쪽으로 걸음 → 앞마당(40 × 40) 밖으로 못 나감
   1-3. 새 게임 첫 도착 → 역마차 연출이 그대로 나오고 하차 뒤 조작됨
2. 숲 드나들기
   2-1. 경비병과 대화해 출발 → 숲 정문 밖에서 남쪽을 보고 시작, 돌아보면 성벽·망루·닫힌 정문
   2-2. 닫힌 정문을 봄 → "[F] 돌아가기", F → 전초기지 정문 밖에서 시작
3. 지역
   3-1. 숲 왼쪽 위 → "1지역 · N일째 · 지도 N%", 패널 '위치' "숲 1지역"
   3-2. 가운데에서 바깥으로 걸음 → 90m쯤에서 2지역, 150m쯤에서 3지역으로 바뀜
   3-3. 가장 바깥(210m쯤) → 둥근 경계에 막힘
   3-4. M → 지도 탭이 정사각형, 가운데가 전초기지, 밝힌 곳이 둥글게 퍼짐
4. 차원문
   4-1. 들어오자마자 → 패널 '차원문' "발견 0 / 5"
   4-2. [안 찾은 차원문 근처로] → 차원문을 보고 30m 앞에 섬, 걸어가면 "차원문을 발견했다 (N지역)" → '차원문' +1
   4-3. 발견한 뒤 → 미니맵·M 지도에 차원문 표시
   4-4. [나갔다 다시 들어오기] → 발견한 차원문은 같은 자리, 안 찾은 차원문은 자기 지역 안 다른 자리
   4-5. [차원문 발견 지우기] → [나갔다 다시 들어오기] → 지도 표시가 없어지고 '차원문' "발견 0 / 5"
   4-6. 차원문들이 다 전초기지 쪽을 봄, 같은 지역 둘(2a·2b, 3a·3b)은 서로 떨어져 있음
5. 리셋
   5-1. [숲 적 70% 쓰러뜨리기] → [나갔다 다시 들어오기] → 정문 밖에서 가장 가까운 차원문이 맥동, 시체들이 원래 지역 안 다른 자리에서 무리째 일어남
6. 의뢰
   6-1. 1지역 고블린 3무리(goblin_a·b·c)를 다 쓰러뜨림 → "목표 달성: …. 정문으로 돌아가자", 임무 일지 목표 '정문으로 돌아가기', 지도 목표 표시가 정문
7. 던전
   7-1. 1지역 북서쪽 고블린 동굴 → 입구가 전초기지 쪽, F로 들어갔다 나오면 동굴 앞에서 전초기지 쪽을 봄
8. 죽음
   8-1. [죽기] → 여관 침대(방이 없으면 정문)에서 깸

---

## 커밋 메시지

```
Add — 숲 지역 구조: 전초기지 둘레 원형 3지역, 지역마다 차원문(1·2·2, 발견하면 고정), 숲 가운데 정문 귀환, 전초기지 목책

Why: 전초기지가 숲 가운데 안전지대이고 멀어질수록 위험하고 차원문이 많은 방사형 구조로 바꿈. 숲 지역은 전초기지 가운데에서 잰 거리로 셈(ForestRegionSpec), 차원문은 새 캡슐(PortalService)이 지역마다 놓고 24m 안에 가면 발견해 그 판 동안 고정. 전초기지와 숲은 따로 씬이고 정문·성벽은 같은 프리팹(OutpostWalls), 전초기지는 목책으로 가리고 정문 쪽만 바깥을 둠.
Alternatives: 전초기지와 숲을 한 씬으로(리셋·던전 만들기·죽은 뒤 도망을 덮는 정문 로딩창이 없어지고 안전지대를 코드로 갈라야 해서 뺌), 숲 바깥을 네모로(안개 진행도는 그대로지만 원형 구조와 달라 뺌), 안 찾은 차원문도 판마다 한 번만 뽑기(발견해도 달라지는 게 없어 뺌)
```
