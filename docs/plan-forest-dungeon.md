# Plan: 6-2 숲 — 고블린 던전·물건 줍기, 숲 70% 리셋 연출, 날짜·의뢰 기간

6-1 plan(`docs/plan-forest-field.md`)을 구현한 코드 위에 더함. 아래 diff·위치는 6-1 plan 코드 기준

## 결정 (사용자, 2026-11-23. 기획 12 결정 68~86 중 6-2 몫)

1. 고블린처럼 물건을 가져가는 몬스터는 떨어진 물건을 주워 감. 고블린은 고블린 던전에 보관. 줍던 고블린을 쓰러뜨리면 그 자리에 떨어뜨림. 보관된 물건은 플레이어가 또 죽어도 남고, 던전이 리셋될 때 같이 리셋됨. 던전을 클리어한 뒤 안 챙기면 사라짐
2. 고블린 동굴 = 숲 지역 안의 인스턴트 던전. 숲에 들어갈 때 던전을 만듦 (무작위 형태는 보류). 고블린 챔피언, 고블린 대주술사 같은 보스를 쓰러뜨리면 클리어. 클리어하고 전초기지로 돌아갔다가 며칠 뒤(보류) 숲에 들어오면 새 던전 (숲 리셋과 따로). 6단계는 고정 던전 하나 + 보스(챔피언·대주술사 중 하나)
3. 숲은 고정. 몬스터·동물·자원을 합쳐 70% 이상 없어지면 리셋. 리셋은 숲 밖에 있을 때만, 다음에 들어올 때 연출: 차원문 맥동 → 마력 분출 → 마력이 죽은 몬스터 시체에 스며들어 부활 → 몬스터가 일어남. 시체는 다 없애고 동물·자원도 원래대로 (6단계엔 몬스터뿐)
4. 의뢰는 전부 기간 한정, 기간이 안 지났으면 남음. 튜토리얼 의뢰만 의뢰 기간 = 원정 기간. 날짜는 하루 실제 10분
5. 전투 설명창: 6-1 시전 방식 변경 때 6-1에 먼저 구현함 (2026-11-24 사용자 요청: 어떤 적이든 플레이어를 알아챈 첫 전투에서 게임을 멈추고 설명창, 6장, 이 PC에서 한 번). 이 plan에선 안 함
6. 소탕 의뢰는 여러 방식 (숲 고블린 소탕, 동굴 소탕 등). 동굴 소탕 의뢰는 이번에 안 만듦 (게시판엔 첫 의뢰 하나)

구현 임시 (사용자 확인 전, 기획에 '구현 임시'로 적음):
- 시간은 숲에서만 흐름 (게임 시간, 일시정지·로딩창엔 멈춤). 의뢰 기간은 처음 출발할 때부터 셈. 기간이 지나면 의뢰 실패: 받은 의뢰가 비고 게시판에 다시 붙음 (받기 가능)
- 던전은 숲 씬 지하(y -60)의 고정 모양, 입구는 1구역. 새 던전은 클리어하고 숲을 나갔다가 다음에 들어올 때 (며칠 조건 보류). 보스는 던전을 만들 때 챔피언·대주술사 중 무작위
- 보스 수치 (기획 미정, v1 족장을 줄임): 챔피언 체력 600·피해 24·6~8초마다 마법사 자리에 붉은 원(반경 5m) 예고 1.1초 뒤 강타 38, 대주술사 체력 400·피해 10·2.5~3.5초마다 마법사에게 불탄 12. 둘 다 체력 절반에서 고블린 3마리를 부름 (부른 고블린은 판 데이터에 안 남음)
- 고블린은 쉴 때 알아채는 거리 안에 떨어진 금화가 있으면 주우러 가고, 주우면 던전 입구로 가져가 보관. 6단계엔 떨어진 물건이 금화뿐
- 70%는 숲 적 자리 수 대비 쓰러뜨린 수 (던전·부른 적은 안 셈). 숲을 나갈 때(입구 귀환·죽음) 판정
- 리셋 연출은 약 6초, 넘기기 없음. 일어난 적은 원래 적 자리를 집으로 돌아다님. 소탕 중 리셋되면 일어난 고블린도 다시 대상
- 클리어한 던전의 보관 금화는 숲을 나갈 때 사라짐

---

## 현행 분석 (6-1 plan 구현 뒤)

- `FieldState`: 숲 적 자리 20곳(무리 번호 = 순서, 키 `{id}.{번호}`), 쓰러진 적은 `ForestDead` 자리에 시체(`InitCorpse`). 떨어진 금화는 `run.DroppedGold`·자리, `GoldPile`(지점 `gold`) 하나. 죽으면 덮개 밑 도망 시뮬 → `RespawningFromDeath` → `HubState`
- `FieldService`: 몸 목록·가까운 상대·조준 보정·범위·무리 깨우기·전투 상황(`CombatChanged`)
- `EnemyController`: Idle(돌아다님·알아채기) → Chase → Dead. 시체는 다시 못 일어남
- `MercenaryController`: 따라오기·싸우기·도망. 순간이동 없음
- 시간·날짜 없음. 의뢰 기간은 식량 계산(`QuestSO.Days`)에만 씀
- `TutorialManager`: 12시 안내(`Notify`), 배움(`MarkLearned`·`IsLearned`), 전투 중 숨김(`SetSuppressed`). 창으로 보이는 튜토리얼은 전투 설명창 하나 (`UIManager.ShowTutorialWindow`)
- `UIManager`: 대화·영입·게시판·설명창이 열리면 게임 멈춤(`RefreshMenuPause`), Esc 순서 확인창 → 설정 → 설명창 → 대화 → 영입 → 게시판 → 메뉴 → 일시정지
- `HubState`: 출발 때 식량만 뺌. 의뢰 기간 없음

---

## C# 변경

### `Assets/Scripts/Core/RunEntry.cs`

**추가 내용:** 날짜·의뢰 기간, 숲 리셋, 던전. 6-1에서 넣은 `RespawningFromDeath` 아래

```csharp
        /// <summary>하루 길이 (게임 초, 기획: 하루 10분)</summary>
        public const float DaySeconds = 600f;

        /// <summary>판에서 흐른 게임 시간 (초). 구현 임시: 숲에서만 흐름</summary>
        public float PlaySeconds { get; set; }

        /// <summary>지금 날짜 (1일부터)</summary>
        public int Day => 1 + Mathf.FloorToInt(PlaySeconds / DaySeconds);

        /// <summary>받은 의뢰가 끝나는 시각 (PlaySeconds 기준). 0이면 아직 안 셈 (처음 출발할 때 정함)</summary>
        public float QuestDeadline { get; set; }

        /// <summary>다음에 숲에 들어갈 때 리셋할지 (숲을 나갈 때 쓰러뜨린 적이 70% 이상이면 켬)</summary>
        public bool ForestResetPending { get; set; }

        /// <summary>고블린 던전 세대 (새로 만들 때마다 오름. 던전 적 키에 들어감)</summary>
        public int DungeonGeneration { get; set; }

        /// <summary>이번 던전 보스 몬스터 id (던전을 만들 때 고름. 아직 없으면 null)</summary>
        public string DungeonBossId { get; set; }

        /// <summary>던전에서 쓰러뜨린 적: 키 → 쓰러진 곳 (던전을 새로 만들면 비움)</summary>
        public Dictionary<string, Vector3> DungeonDead { get; } = new(StringComparer.Ordinal);

        /// <summary>이번 던전 보스를 쓰러뜨렸는지 (클리어)</summary>
        public bool DungeonCleared { get; set; }

        /// <summary>던전을 새로 만들어야 하는지 (클리어하고 숲을 나가면 켬. 며칠 뒤 조건은 보류)</summary>
        public bool DungeonNeedsReset { get; set; }

        /// <summary>고블린이 던전에 보관한 금화</summary>
        public int DungeonStashGold { get; set; }
```

---

### `Assets/Scripts/Quest/QuestSO.cs`

**추가 내용:** 의뢰 기간. 6-1의 `_target` 아래, `Target` 아래

```csharp
        /// <summary>의뢰 기간 (일). 0이면 원정 기간(_days)과 같음 (튜토리얼 의뢰)</summary>
        [SerializeField, Min(0)] int _periodDays;
```

```csharp
        /// <summary>의뢰 기간 (일, 0이면 원정 기간)</summary>
        public int PeriodDays => _periodDays > 0 ? _periodDays : _days;
```

---

### `Assets/Scripts/Field/MonsterSO.cs`

**추가 내용:** 물건 줍기, 보스 패턴. 파일 맨 위(클래스 앞)에 enum, `_color` 아래 필드, `Color` 아래 프로퍼티

```csharp
    /// <summary>보스 패턴</summary>
    public enum BossPattern
    {
        /// <summary>없음 (보통 적)</summary>
        None,

        /// <summary>마법사 자리에 붉은 원을 예고하고 지연 뒤 강타 (v1 족장)</summary>
        Slam,

        /// <summary>마법사에게 불탄을 쏨</summary>
        FireBolt,
    }
```

```csharp
        /// <summary>떨어진 물건을 주워 던전에 보관하는지 (고블린)</summary>
        [SerializeField] bool _picksUpItems;

        /// <summary>보스 패턴 (보통 적은 None)</summary>
        [SerializeField] BossPattern _bossPattern;

        /// <summary>패턴 사이 최소·최대 시간 (초)</summary>
        [SerializeField, Min(0.1f)] float _patternIntervalMin = 6f;
        [SerializeField, Min(0.1f)] float _patternIntervalMax = 8f;

        /// <summary>강타 예고 시간 (초)</summary>
        [SerializeField, Min(0f)] float _patternDelay = 1.1f;

        /// <summary>강타 반지름 (m)</summary>
        [SerializeField, Min(0f)] float _patternRadius = 5f;

        /// <summary>패턴 피해 (강타·불탄)</summary>
        [SerializeField, Min(0f)] float _patternDamage = 38f;

        /// <summary>체력 절반에서 부르는 수 (0이면 안 부름)</summary>
        [SerializeField, Min(0)] int _summonCount;

        /// <summary>부르는 몬스터</summary>
        [SerializeField] MonsterSO _summon;
```

```csharp
        /// <summary>떨어진 물건을 주워 던전에 보관하는지</summary>
        public bool PicksUpItems => _picksUpItems;

        /// <summary>보스 패턴</summary>
        public BossPattern Pattern => _bossPattern;

        /// <summary>패턴 사이 최소 시간 (초)</summary>
        public float PatternIntervalMin => _patternIntervalMin;

        /// <summary>패턴 사이 최대 시간 (초)</summary>
        public float PatternIntervalMax => Mathf.Max(_patternIntervalMin, _patternIntervalMax);

        /// <summary>강타 예고 시간 (초)</summary>
        public float PatternDelay => _patternDelay;

        /// <summary>강타 반지름 (m)</summary>
        public float PatternRadius => _patternRadius;

        /// <summary>패턴 피해</summary>
        public float PatternDamage => _patternDamage;

        /// <summary>체력 절반에서 부르는 수</summary>
        public int SummonCount => _summonCount;

        /// <summary>부르는 몬스터 (없으면 null)</summary>
        public MonsterSO Summon => _summon;
```

---

### `Assets/Scripts/Field/FieldService.cs`

**추가 내용:** 떨어진 물건(금화)과 보관 장소, 줍기·떨굼·보관 알림. `using System;`은 이미 있음. `LastHitTime` 아래 프로퍼티·이벤트, `ReportHit` region 아래 메서드

```csharp
        /// <summary>떨어진 금화 지점 (없으면 null)</summary>
        public Transform Loot { get; private set; }

        /// <summary>떨어진 금화</summary>
        public int LootGold { get; private set; }

        /// <summary>줍는 몬스터가 주운 물건을 가져가는 곳 (던전 입구 밖. 없으면 null)</summary>
        public Transform StashPoint { get; set; }

        /// <summary>떨어진 금화를 누가 주움 (지점을 지울 때)</summary>
        public event Action LootTaken;

        /// <summary>금화를 떨굼 (자리, 금화. 들고 가던 몬스터가 쓰러짐)</summary>
        public event Action<Vector3, int> LootDropped;

        /// <summary>몬스터가 금화를 던전에 보관함 (금화)</summary>
        public event Action<int> LootStored;
```

```csharp
        #region SetLoot - 떨어진 금화 지점과 금화를 정함 (없으면 null·0)
        /// <param name="pile">떨어진 금화 지점</param>
        /// <param name="gold">금화</param>
        public void SetLoot(Transform pile, int gold)
        {
            Loot = gold > 0 ? pile : null;
            LootGold = Loot != null ? gold : 0;
        }
        #endregion

        #region TakeLoot - 떨어진 금화를 다 주움 (지점이 없어짐 알림). 주운 금화
        public int TakeLoot()
        {
            if (Loot == null) return 0;
            // 주운 금화
            int gold = LootGold;
            Loot = null;
            LootGold = 0;
            LootTaken?.Invoke();
            return gold;
        }
        #endregion

        #region DropLoot - 금화를 그 자리에 떨굼 알림 (FieldState가 지점을 만들고 SetLoot)
        /// <param name="position">떨군 곳</param>
        /// <param name="gold">금화</param>
        public void DropLoot(Vector3 position, int gold)
        {
            if (gold > 0) LootDropped?.Invoke(position, gold);
        }
        #endregion

        #region StoreLoot - 금화를 던전에 보관함 알림
        /// <param name="gold">금화</param>
        public void StoreLoot(int gold)
        {
            if (gold > 0) LootStored?.Invoke(gold);
        }
        #endregion
```

---

### `Assets/Scripts/Field/EnemyController.cs`

**변경 내용:** 줍는 몬스터는 떨어진 금화를 주워 던전 입구로 가져감(들고 쓰러지면 떨굼), 시체에서 다시 일어남(리셋), 일어나는 모습 비율

상수 추가 (`CorpseLift` 아래):

```csharp
        /// <summary>떨어진 금화를 주웠다고 칠 거리 (m)</summary>
        const float PickupDistance = 1.5f;

        /// <summary>보관 장소에 닿았다고 칠 거리 (m)</summary>
        const float StoreDistance = 3f;
```

`EnemyState` 교체:

```csharp
        /// <summary>적 상태</summary>
        enum EnemyState
        {
            /// <summary>집 둘레를 돌며 쉼</summary>
            Idle,

            /// <summary>대상을 쫓거나 때림</summary>
            Chase,

            /// <summary>떨어진 금화로 감 (줍는 몬스터)</summary>
            Fetch,

            /// <summary>주운 금화를 보관 장소로 가져감</summary>
            Carry,

            /// <summary>죽음 (시체)</summary>
            Dead,
        }
```

`Target` 프로퍼티 아래:

```csharp
        /// <summary>들고 있는 금화 (줍는 몬스터)</summary>
        public int CarriedGold { get; private set; }
```

`Update` 교체:

```csharp
        #region Update - 멈췄으면(로딩창·메뉴) 안 함. 상태마다: 쉼(알아채기·줍기·돌아다니기), 줍기, 가져가기, 쫓기·때리기
        void Update()
        {
            if (_service == null || _state == EnemyState.Dead) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _cooldown -= dt;
            _repath -= dt;
            switch (_state)
            {
                case EnemyState.Idle: UpdateIdle(dt); break;
                case EnemyState.Fetch: UpdateFetch(); break;
                case EnemyState.Carry: UpdateCarry(); break;
                default: UpdateChase(dt); break;
            }
        }
        #endregion
```

`UpdateChase`에서 `_repath -= dt;` 줄을 지움 (`Update`에서 셈).

`UpdateIdle` 교체 + 새 메서드:

```csharp
        #region UpdateIdle - 알아채면 무리를 깨움. 들고 있는 금화가 있으면 가져가기, 줍는 몬스터면 알아채는 거리 안 떨어진 금화로. 아니면 가끔 집 둘레를 천천히 걸음
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateIdle(float dt)
        {
            if (TryNotice()) return;
            if (CarriedGold > 0 && _service.StashPoint != null)
            {
                _state = EnemyState.Carry;
                _repath = 0f;
                return;
            }
            if (Monster.PicksUpItems && _service.Loot != null && FlatDistance(_service.Loot.position) <= Monster.AggroRange)
            {
                _state = EnemyState.Fetch;
                _repath = 0f;
                return;
            }
            _wanderWait -= dt;
            if (_wanderWait > 0f) return;
            _wanderWait = Random.Range(WanderWaitMin, WanderWaitMax);
            // 집 둘레에서 고른 곳
            Vector2 offset = Random.insideUnitCircle * WanderRadius;
            if (NavMesh.SamplePosition(_home + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, SampleDistance, NavMesh.AllAreas))
                MoveTo(hit.position, Monster.Speed * WanderSpeedScale);
        }
        #endregion

        #region TryNotice - 알아채는 거리 안에 파티가 있으면 무리를 깨움 (깨웠으면 true)
        bool TryNotice()
        {
            // 알아챈 파티 몸 (알아채는 거리 안 가장 가까운 것)
            FieldUnitController seen = _service.Nearest(FieldTeam.Party, transform.position, Monster.AggroRange);
            if (seen == null) return false;
            _service.WakePack(Pack, seen);
            return true;
        }
        #endregion

        #region UpdateFetch - 떨어진 금화로 감. 닿으면 다 주워 가져가기 (보관 장소가 없으면 쉼). 금화가 없어졌으면 쉼
        void UpdateFetch()
        {
            if (TryNotice()) return;
            // 떨어진 금화 지점
            Transform loot = _service.Loot;
            if (loot == null)
            {
                _state = EnemyState.Idle;
                return;
            }
            if (FlatDistance(loot.position) <= PickupDistance)
            {
                CarriedGold += _service.TakeLoot();
                _state = CarriedGold > 0 && _service.StashPoint != null ? EnemyState.Carry : EnemyState.Idle;
                _repath = 0f;
                return;
            }
            if (_repath > 0f) return;
            _repath = RepathSeconds;
            MoveTo(loot.position, Monster.Speed);
        }
        #endregion

        #region UpdateCarry - 보관 장소로 감. 닿으면 보관하고 집으로 돌아가며 쉼
        void UpdateCarry()
        {
            if (TryNotice()) return;
            // 보관 장소
            Transform stash = _service.StashPoint;
            if (stash == null)
            {
                _state = EnemyState.Idle;
                return;
            }
            if (FlatDistance(stash.position) <= StoreDistance)
            {
                _service.StoreLoot(CarriedGold);
                CarriedGold = 0;
                _state = EnemyState.Idle;
                MoveTo(_home, Monster.Speed * WanderSpeedScale);
                return;
            }
            if (_repath > 0f) return;
            _repath = RepathSeconds;
            MoveTo(stash.position, Monster.Speed);
        }
        #endregion

        #region FlatDistance - 그 곳까지 평면 거리
        /// <param name="point">곳</param>
        float FlatDistance(Vector3 point) => Vector3.ProjectOnPlane(point - transform.position, Vector3.up).magnitude;
        #endregion
```

`Wake` 첫 줄:

```diff
-            if (_state != EnemyState.Idle || target == null || !target.IsAlive) return;
+            if (_state == EnemyState.Chase || _state == EnemyState.Dead || target == null || !target.IsAlive) return;
```

`OnUnitDied`에서 `LieDown();` 앞에:

```csharp
            if (CarriedGold > 0)
            {
                _service.DropLoot(transform.position, CarriedGold);
                CarriedGold = 0;
            }
```

`LieDown` 교체 + 새 메서드:

```csharp
        #region LieDown - 도형을 옆으로 눕힘 (시체)
        void LieDown() => SetRise(0f);
        #endregion

        #region SetRise - 누움(0) ~ 섬(1) 사이 모습 (리셋 연출이 일어나는 동안 부름)
        /// <param name="t">일어난 비율 (0~1)</param>
        public void SetRise(float t)
        {
            t = Mathf.Clamp01(t);
            _visual.localRotation = Quaternion.Slerp(Quaternion.Euler(0f, 0f, 90f), Quaternion.identity, t);
            _visual.localPosition = Vector3.up * (_unit.Radius * CorpseLift * (1f - t));
        }
        #endregion

        #region Revive - 시체면 다시 일어남 (숲 리셋): 체력을 채우고 움직임·콜라이더를 켜고 그 집에서 쉼, 현장 규칙에 등록
        /// <param name="service">현장 규칙</param>
        /// <param name="home">돌아다닐 집 (원래 적 자리)</param>
        public void Revive(FieldService service, Vector3 home)
        {
            if (_state != EnemyState.Dead || Monster == null) return;
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            _home = home;
            _unit.Init(Monster.MaxHp);
            _focusMage = Random.value < Monster.MageFocusChance;
            _wanderWait = Random.Range(0f, WanderWaitMax);
            _collider.enabled = true;
            _agent.enabled = true;
            SetRise(1f);
            _state = EnemyState.Idle;
            _unit.Damaged -= OnDamaged;
            _unit.Damaged += OnDamaged;
            _unit.Died -= OnUnitDied;
            _unit.Died += OnUnitDied;
            service.RegisterEnemy(this);
        }
        #endregion
```

**호출/구독 관계:**
- `FieldService.Loot`·`StashPoint`·`TakeLoot`·`DropLoot`·`StoreLoot`. 리셋은 `ForestResetSequence`가 `SetRise`, `FieldState.OnResetFinished`가 `Revive`

---

### `Assets/Scripts/Field/MercenaryController.cs`

**추가 내용:** 순간이동 (던전 드나들 때). `MarkSafe` region 아래

```csharp
        #region WarpTo - 살아 있으면 그 곳으로 순간이동 (던전 드나들 때 마법사를 따라옴)
        /// <param name="position">갈 곳 (NavMesh 위)</param>
        public void WarpTo(Vector3 position)
        {
            if (_dead || !_agent.enabled) return;
            _agent.Warp(position);
            _foe = null;
            _repath = 0f;
        }
        #endregion
```

---

### `Assets/Scripts/Field/BossPatternController.cs` (새 파일)

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>보스 패턴 (같은 오브젝트의 EnemyController와 같이 씀): 쫓는 중에 패턴 간격마다 강타(마법사 자리에 붉은 원 예고 → 지연 뒤 원 안 파티 피해) 또는 불탄(마법사에게). 체력 절반에서 한 번 부하를 부름. 수치는 MonsterSO</summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class BossPatternController : MonoBehaviour
    {
        /// <summary>불탄 속도 (m/s)</summary>
        const float BoltSpeed = 18f;

        /// <summary>불탄 최대 비행 시간 (초)</summary>
        const float BoltLifeSeconds = 3f;

        /// <summary>부하를 부르는 체력 비율 (v1 족장 절반)</summary>
        const float SummonHpRatio = 0.5f;

        /// <summary>불탄 색</summary>
        static readonly Color BoltColor = new(1f, 0.3f, 0.15f);

        /// <summary>같은 보스 (인스펙터에서만 연결)</summary>
        [SerializeField] EnemyController _enemy;

        /// <summary>강타 예고 원 프리팹 (Telegraph.prefab, 지름 1. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _telegraphPrefab;

        /// <summary>불탄 프리팹 (SpellProjectile.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] SpellProjectileController _boltPrefab;

        /// <summary>불탄이 나가는 곳 (인스펙터에서만 연결)</summary>
        [SerializeField] Transform _muzzle;

        /// <summary>강타 범위 안 몸 (강타 때 다시 씀)</summary>
        readonly List<FieldUnitController> _found = new();

        /// <summary>현장 규칙 (Init에서 받음)</summary>
        FieldService _service;

        /// <summary>부하 부르기 (몬스터, 근처 → 부른 적. FieldState가 넘김)</summary>
        Func<MonsterSO, Vector3, EnemyController> _summon;

        /// <summary>다음 패턴까지 (초)</summary>
        float _timer;

        /// <summary>강타 예고 남은 시간 (초, 0이면 예고 중 아님)</summary>
        float _slamLeft;

        /// <summary>강타 자리</summary>
        Vector3 _slamAt;

        /// <summary>예고 원 (처음 쓸 때 만듦)</summary>
        GameObject _telegraph;

        /// <summary>부하를 불렀는지 (한 번만)</summary>
        bool _summoned;

        #region Init - 현장 규칙·부하 부르기를 받고 첫 패턴 시간을 정함
        /// <param name="service">현장 규칙</param>
        /// <param name="summon">부하 부르기</param>
        public void Init(FieldService service, Func<MonsterSO, Vector3, EnemyController> summon)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _summon = summon;
            _summoned = false;
            _slamLeft = 0f;
            ResetTimer();
        }
        #endregion

        #region Update - 멈췄거나 죽었으면 예고를 숨기고 안 함. 쫓는 중이면 체력 절반 부하, 예고 중이면 지연 뒤 강타, 아니면 간격마다 패턴
        void Update()
        {
            if (_service == null || _enemy == null || _enemy.Monster == null) return;
            if (!_enemy.Unit.IsAlive)
            {
                HideTelegraph();
                return;
            }
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f || _enemy.Target == null) return;
            TrySummon();
            if (_slamLeft > 0f)
            {
                _slamLeft -= dt;
                if (_slamLeft <= 0f) Slam();
                return;
            }
            _timer -= dt;
            if (_timer > 0f) return;
            ResetTimer();
            if (_enemy.Monster.Pattern == BossPattern.Slam) StartSlam();
            else if (_enemy.Monster.Pattern == BossPattern.FireBolt) ShootBolt();
        }
        #endregion

        #region ResetTimer - 다음 패턴까지 시간을 최소~최대 사이에서 고름
        void ResetTimer()
        {
            if (_enemy == null || _enemy.Monster == null) return;
            _timer = UnityEngine.Random.Range(_enemy.Monster.PatternIntervalMin, _enemy.Monster.PatternIntervalMax);
        }
        #endregion

        #region TrySummon - 체력이 절반 아래로 처음 내려가면 부하를 그 수만큼 부름
        void TrySummon()
        {
            // 보스 데이터
            MonsterSO monster = _enemy.Monster;
            if (_summoned || _summon == null || monster.SummonCount <= 0 || monster.Summon == null) return;
            if (_enemy.Unit.Hp > _enemy.Unit.MaxHp * SummonHpRatio) return;
            _summoned = true;
            for (int i = 0; i < monster.SummonCount; i++) _summon(monster.Summon, transform.position);
        }
        #endregion

        #region StartSlam - 마법사 자리에 강타 반지름 붉은 원을 띄우고 예고 시간을 셈 (v1)
        void StartSlam()
        {
            _slamAt = _service.Player.transform.position;
            _slamLeft = _enemy.Monster.PatternDelay;
            if (_telegraph == null && _telegraphPrefab != null) _telegraph = Instantiate(_telegraphPrefab);
            if (_telegraph == null) return;
            _telegraph.transform.position = _slamAt;
            _telegraph.transform.localScale = new Vector3(_enemy.Monster.PatternRadius * 2f, 1f, _enemy.Monster.PatternRadius * 2f);
            _telegraph.SetActive(true);
        }
        #endregion

        #region Slam - 원 안 파티 몸에 피해 (동료도 맞음), 예고 숨김
        void Slam()
        {
            _service.FindInRadius(FieldTeam.Party, _slamAt, _enemy.Monster.PatternRadius, _found);
            foreach (FieldUnitController unit in _found) unit.TakeDamage(_enemy.Monster.PatternDamage, _enemy.Unit);
            HideTelegraph();
        }
        #endregion

        #region ShootBolt - 마법사 겨눌 점으로 불탄 (파티를 노림)
        void ShootBolt()
        {
            if (_boltPrefab == null || !_service.Player.IsAlive) return;
            // 불탄이 나가는 곳
            Vector3 from = _muzzle != null ? _muzzle.position : _enemy.Unit.AimPoint;
            // 방향
            Vector3 direction = (_service.Player.AimPoint - from).normalized;
            // 쏜 불탄
            SpellProjectileController bolt = Instantiate(_boltPrefab, from, Quaternion.LookRotation(direction));
            bolt.Launch(direction, BoltSpeed, BoltLifeSeconds, _enemy.Monster.PatternDamage, 0f, 0f, FieldTeam.Party, _enemy.Unit, _service, BoltColor);
        }
        #endregion

        #region HideTelegraph - 예고 원을 숨기고 예고를 멈춤
        void HideTelegraph()
        {
            _slamLeft = 0f;
            if (_telegraph != null) _telegraph.SetActive(false);
        }
        #endregion

        #region OnDestroy - 만든 예고 원을 지움
        void OnDestroy()
        {
            if (_telegraph != null) Destroy(_telegraph);
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/Field/FieldClockController.cs` (새 파일)

```csharp
using System;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 시계: 게임 시간만큼 판 데이터 시간(PlaySeconds)을 흐르게 하고 (구현 임시: 숲에서만), 받은 의뢰의 기간이 지나면 한 번 알림</summary>
    public sealed class FieldClockController : MonoBehaviour
    {
        /// <summary>이번 판 데이터 (없으면 안 함)</summary>
        RunEntry _run;

        /// <summary>이번 숲에서 기간 지남을 알렸는지</summary>
        bool _expired;

        /// <summary>받은 의뢰의 기간이 지남 (못 끝낸 의뢰가 있을 때 한 번)</summary>
        public event Action Expired;

        #region Init - 판 데이터를 받음
        /// <param name="run">이번 판 데이터</param>
        public void Init(RunEntry run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _expired = false;
        }
        #endregion

        #region Update - 멈췄으면 안 함. 시간을 더하고, 못 끝낸 의뢰의 기간이 지났으면 알림
        void Update()
        {
            if (_run == null) return;
            // 이번 프레임 게임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _run.PlaySeconds += dt;
            if (_expired || _run.Quest == null || _run.QuestCleared || _run.QuestDeadline <= 0f || _run.PlaySeconds < _run.QuestDeadline) return;
            _expired = true;
            Expired?.Invoke();
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Sequences/ForestResetSequence.cs` (새 파일)

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 리셋 연출 (약 6초, 넘기기 없음): 연출 카메라로 차원문을 봄 → 차원문 맥동 3번 → 마력 구가 퍼짐 → 차원문에서 시체마다 마력 줄기가 뻗어 스며듦 → 가장 가까운 시체 쪽 카메라에서 시체들이 일어남 → 카메라를 끄고 알림. 게임 규칙은 안 건드림 (일어난 적의 AI는 FieldState가 켬)</summary>
    public sealed class ForestResetSequence : MonoBehaviour
    {
        /// <summary>맥동 시간 (초)</summary>
        const float PulseSeconds = 1.5f;

        /// <summary>맥동 횟수</summary>
        const int PulseCount = 3;

        /// <summary>맥동 크기 (배율 더하기)</summary>
        const float PulseScale = 0.15f;

        /// <summary>마력 구가 퍼지는 시간 (초)</summary>
        const float BurstSeconds = 1f;

        /// <summary>마력 줄기가 뻗는 시간 (초)</summary>
        const float StreamSeconds = 1.5f;

        /// <summary>일어나는 시간 (초)</summary>
        const float RiseSeconds = 1f;

        /// <summary>줄기를 그릴 시체 최대 수 (많으면 앞에서부터)</summary>
        const int MaxStreams = 40;

        /// <summary>차원문 가운데 높이 (m)</summary>
        const float PortalCenterHeight = 4f;

        /// <summary>연출 카메라 (꺼 둠, 플레이어 카메라보다 Depth 높음, AudioListener 없음. 인스펙터에서만 연결)</summary>
        [SerializeField] Camera _camera;

        /// <summary>차원문을 볼 자리 (차원문 기준 월드 오프셋)</summary>
        [SerializeField] Vector3 _portalView = new(0f, 10f, -24f);

        /// <summary>시체를 볼 자리 (시체 기준 월드 오프셋)</summary>
        [SerializeField] Vector3 _corpseView = new(0f, 4f, -9f);

        /// <summary>퍼지는 마력 구 (꺼 둔 자식, 지름 1. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _burst;

        /// <summary>마력 구가 퍼지는 반지름 (m)</summary>
        [SerializeField, Min(1f)] float _burstRadius = 60f;

        /// <summary>마력 줄기 프리팹 (ManaStream.prefab, 점 2개. 인스펙터에서만 연결)</summary>
        [SerializeField] LineRenderer _streamPrefab;

        /// <summary>그리는 줄기 (끝나면 지움)</summary>
        readonly List<LineRenderer> _streams = new();

        /// <summary>도는 연출. 없으면 null</summary>
        Coroutine _routine;

        /// <summary>연출 중인지</summary>
        public bool IsPlaying => _routine != null;

        #region Awake - 연결 확인, 카메라·마력 구를 끔
        void Awake()
        {
            if (_camera == null || _burst == null || _streamPrefab == null) Debug.LogError("[Reset] 카메라·마력 구·줄기 연결 안 됨", this);
            if (_camera != null) _camera.gameObject.SetActive(false);
            if (_burst != null) _burst.gameObject.SetActive(false);
        }
        #endregion

        #region Play - 차원문·시체를 받아 연출 시작, 끝나면 onDone (도는 중이면 무시)
        /// <param name="portal">차원문</param>
        /// <param name="corpses">일어날 시체 (누운 적)</param>
        /// <param name="focus">카메라가 볼 기준 (플레이어. 가장 가까운 시체를 봄)</param>
        /// <param name="onDone">끝나면 할 일</param>
        public void Play(Transform portal, IReadOnlyList<EnemyController> corpses, Transform focus, Action onDone)
        {
            if (portal == null) throw new ArgumentNullException(nameof(portal));
            if (corpses == null) throw new ArgumentNullException(nameof(corpses));
            if (_routine != null) return;
            _routine = StartCoroutine(Run(portal, corpses, focus, onDone));
        }
        #endregion

        #region Run - 맥동 → 마력 구 → 줄기 → 일어남 → 정리
        /// <param name="portal">차원문</param>
        /// <param name="corpses">일어날 시체</param>
        /// <param name="focus">카메라가 볼 기준</param>
        /// <param name="onDone">끝나면 할 일</param>
        IEnumerator Run(Transform portal, IReadOnlyList<EnemyController> corpses, Transform focus, Action onDone)
        {
            // 차원문 가운데
            Vector3 center = portal.position + Vector3.up * PortalCenterHeight;
            _camera.gameObject.SetActive(true);
            _camera.transform.position = portal.position + _portalView;
            _camera.transform.LookAt(center);
            yield return Pulse(portal);
            yield return Burst(center);
            yield return Streams(center, corpses);
            // 카메라가 볼 시체 (기준에서 가장 가까운 것)
            EnemyController nearest = Nearest(corpses, focus != null ? focus.position : center);
            if (nearest != null)
            {
                _camera.transform.position = nearest.transform.position + _corpseView;
                _camera.transform.LookAt(nearest.transform.position);
            }
            yield return Rise(corpses);
            _camera.gameObject.SetActive(false);
            _routine = null;
            onDone?.Invoke();
        }
        #endregion

        #region Pulse - 차원문 크기를 맥동 횟수만큼 키웠다 줄임
        /// <param name="portal">차원문</param>
        IEnumerator Pulse(Transform portal)
        {
            // 원래 크기
            Vector3 baseScale = portal.localScale;
            for (float t = 0f; t < PulseSeconds; t += Time.deltaTime)
            {
                portal.localScale = baseScale * (1f + PulseScale * Mathf.Abs(Mathf.Sin(t / PulseSeconds * PulseCount * Mathf.PI)));
                yield return null;
            }
            portal.localScale = baseScale;
        }
        #endregion

        #region Burst - 차원문 가운데에서 마력 구가 퍼짐
        /// <param name="center">차원문 가운데</param>
        IEnumerator Burst(Vector3 center)
        {
            _burst.position = center;
            _burst.gameObject.SetActive(true);
            for (float t = 0f; t < BurstSeconds; t += Time.deltaTime)
            {
                _burst.localScale = Vector3.one * Mathf.Lerp(1f, _burstRadius * 2f, t / BurstSeconds);
                yield return null;
            }
            _burst.gameObject.SetActive(false);
        }
        #endregion

        #region Streams - 차원문에서 시체마다 줄기를 뻗음 (끝이 시체까지 감), 다 뻗으면 지움
        /// <param name="center">차원문 가운데</param>
        /// <param name="corpses">시체</param>
        IEnumerator Streams(Vector3 center, IReadOnlyList<EnemyController> corpses)
        {
            for (int i = 0; i < corpses.Count && _streams.Count < MaxStreams; i++)
            {
                if (corpses[i] == null) continue;
                // 줄기 하나
                LineRenderer stream = Instantiate(_streamPrefab, transform);
                stream.positionCount = 2;
                stream.SetPosition(0, center);
                stream.SetPosition(1, center);
                _streams.Add(stream);
            }
            for (float t = 0f; t < StreamSeconds; t += Time.deltaTime)
            {
                // 뻗은 비율
                float k = t / StreamSeconds;
                for (int i = 0, s = 0; i < corpses.Count && s < _streams.Count; i++)
                {
                    if (corpses[i] == null) continue;
                    _streams[s++].SetPosition(1, Vector3.Lerp(center, corpses[i].transform.position, k));
                }
                yield return null;
            }
            foreach (LineRenderer stream in _streams)
                if (stream != null) Destroy(stream.gameObject);
            _streams.Clear();
        }
        #endregion

        #region Rise - 시체들이 누운 모습에서 선 모습으로
        /// <param name="corpses">시체</param>
        IEnumerator Rise(IReadOnlyList<EnemyController> corpses)
        {
            for (float t = 0f; t < RiseSeconds; t += Time.deltaTime)
            {
                foreach (EnemyController corpse in corpses)
                    if (corpse != null) corpse.SetRise(t / RiseSeconds);
                yield return null;
            }
            foreach (EnemyController corpse in corpses)
                if (corpse != null) corpse.SetRise(1f);
        }
        #endregion

        #region Nearest - 기준점에서 가장 가까운 시체 (없으면 null)
        /// <param name="corpses">시체</param>
        /// <param name="from">기준점</param>
        static EnemyController Nearest(IReadOnlyList<EnemyController> corpses, Vector3 from)
        {
            // 찾은 시체와 거리 제곱
            EnemyController best = null;
            float bestSq = float.MaxValue;
            foreach (EnemyController corpse in corpses)
            {
                if (corpse == null) continue;
                // 거리 제곱
                float sq = (corpse.transform.position - from).sqrMagnitude;
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = corpse;
            }
            return best;
        }
        #endregion

        #region OnDisable - 상태를 나가면 연출을 멈추고 카메라·구·줄기를 정리
        void OnDisable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (_camera != null) _camera.gameObject.SetActive(false);
            if (_burst != null) _burst.gameObject.SetActive(false);
            foreach (LineRenderer stream in _streams)
                if (stream != null) Destroy(stream.gameObject);
            _streams.Clear();
        }
        #endregion
    }
}
```

---

### `Assets/Scripts/UI/Binders/FieldHudUIBinder.cs`

**변경 내용:** 구역 줄에 날짜

```diff
-        /// <summary>구역 줄 키 ({0} 구역 이름, {1} 지도 %)</summary>
+        /// <summary>구역 줄 키 ({0} 구역 이름, {1} 날짜, {2} 지도 %)</summary>
         const string ZoneLineKey = "field.zone_line";
```

`_zoneAt` 필드 아래:

```csharp
        /// <summary>지금 날짜 (1일부터)</summary>
        Func<int> _dayNow;

        /// <summary>마지막으로 보인 날짜</summary>
        int _shownDay = -1;
```

`Bind` 맨 뒤 매개변수 추가:

```diff
         /// <param name="zoneAt">지금 구역 번호</param>
+        /// <param name="dayNow">지금 날짜</param>
         public void Bind(FieldUnitController player, PlayerController controller, FieldCastController cast, IReadOnlyList<MercenaryController> mercs,
-            FieldService service, FogMapService fog, Func<int> zoneAt)
+            FieldService service, FogMapService fog, Func<int> zoneAt, Func<int> dayNow)
         {
             Unbind();
```

```diff
             _zoneAt = zoneAt ?? throw new ArgumentNullException(nameof(zoneAt));
+            _dayNow = dayNow ?? throw new ArgumentNullException(nameof(dayNow));
             _player.Damaged += OnPlayerDamaged;
-            _shownHp = _shownMana = _shownZone = _shownPercent = -1;
+            _shownHp = _shownMana = _shownZone = _shownPercent = _shownDay = -1;
```

`Unbind`: `_zoneAt = null;` 아래 `_dayNow = null;`

`RefreshZone` 교체:

```csharp
        #region RefreshZone - 구역·날짜·지도 %가 바뀌었을 때만 "구역 이름 · N일째 · 지도 N%"를 다시 씀
        void RefreshZone()
        {
            // 지금 구역·날짜·지도 %
            int zone = _zoneAt();
            int day = _dayNow();
            int percent = Mathf.RoundToInt(_fog.Progress * 100f);
            if (zone == _shownZone && day == _shownDay && percent == _shownPercent) return;
            _shownZone = zone;
            _shownDay = day;
            _shownPercent = percent;
            // 구역 이름 (지금 언어)
            string zoneName = new LocalizedString(Table, string.Format(CultureInfo.InvariantCulture, ZoneKeyFormat, zone)).GetLocalizedString();
            // 구역 줄의 String Reference
            var text = _view.ZoneText.StringReference;
            text.SetReference(Table, ZoneLineKey);
            text.Arguments = new object[] { zoneName, day, percent };
            _view.ZoneText.RefreshString();
        }
        #endregion
```

---

### `Assets/Scripts/UI/Binders/JournalUIBinder.cs`

**변경 내용:** 의뢰 줄에 기간 (출발 전엔 의뢰 기간, 출발 뒤엔 남은 날). 맨 위 `using UnityEngine.Localization.Tables;` 추가, 클래스 맨 위 상수

```csharp
        /// <summary>의뢰 줄 키 ({0} 의뢰 이름, {1} 의뢰 기간 일)</summary>
        const string QuestKey = "menu.journal.quest";

        /// <summary>의뢰 줄 키, 기간을 세는 중 ({0} 의뢰 이름, {1} 남은 날)</summary>
        const string QuestLeftKey = "menu.journal.quest_left";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference MenuTable = "Menu";
```

`OnEnable`의 의뢰 줄 부분 교체:

```diff
-            // 이번 판 받은 의뢰 (판이나 의뢰가 없으면 null)
-            QuestSO quest = GameManager.Instance.Run?.Quest;
+            // 이번 판 데이터 (없으면 null)
+            RunEntry run = GameManager.Instance.Run;
+            // 받은 의뢰 (판이나 의뢰가 없으면 null)
+            QuestSO quest = run?.Quest;
             _view.QuestText.gameObject.SetActive(quest != null);
             if (quest != null)
             {
-                _view.QuestText.StringReference.Arguments = new object[] { quest.Title.GetLocalizedString(), quest.Days };
+                // 기간을 세는 중인지 (처음 출발 뒤)
+                bool counting = run.QuestDeadline > 0f;
+                // 보일 날 수 (세는 중이면 남은 날 올림, 아니면 의뢰 기간)
+                int days = counting ? Mathf.Max(0, Mathf.CeilToInt((run.QuestDeadline - run.PlaySeconds) / RunEntry.DaySeconds)) : quest.PeriodDays;
+                _view.QuestText.StringReference.SetReference(MenuTable, counting ? QuestLeftKey : QuestKey);
+                _view.QuestText.StringReference.Arguments = new object[] { quest.Title.GetLocalizedString(), days };
                 _view.QuestText.RefreshString();
             }
```

---

### `Assets/Scripts/UI/Views/FieldView.cs`

**추가 내용:** 시계·리셋 연출·HUD 캔버스, 던전 자리. `_returnObjective` 아래 필드, `ReturnObjective` 아래 프로퍼티

```csharp
        /// <summary>숲 시계 (Field 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] FieldClockController _clock;

        /// <summary>숲 리셋 연출 (ResetSequence 오브젝트. 인스펙터에서만 연결)</summary>
        [SerializeField] ForestResetSequence _resetSequence;

        /// <summary>HUD 캔버스 (리셋 연출 동안 끔. 인스펙터에서만 연결)</summary>
        [SerializeField] Canvas _hudCanvas;

        /// <summary>던전 입구 밖 (나오면 서는 곳, 줍는 몬스터가 물건을 가져가는 곳. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _dungeonOutside;

        /// <summary>던전 안 시작 자리 (인스펙터에서만 연결)</summary>
        [SerializeField] Transform _dungeonStart;

        /// <summary>던전 적 무리 자리 (인스펙터에서만 연결)</summary>
        [SerializeField] SpawnPointView[] _dungeonSpawnPoints;

        /// <summary>보스 자리 (인스펙터에서만 연결)</summary>
        [SerializeField] Transform _bossPoint;

        /// <summary>보스 후보 (던전을 만들 때 하나를 고름. 인스펙터에서만 연결)</summary>
        [SerializeField] MonsterSO[] _bossChoices;

        /// <summary>보스 프리팹 (Boss.prefab. 인스펙터에서만 연결)</summary>
        [SerializeField] EnemyController _bossPrefab;
```

```csharp
        /// <summary>숲 시계</summary>
        public FieldClockController Clock => _clock;

        /// <summary>숲 리셋 연출</summary>
        public ForestResetSequence ResetSequence => _resetSequence;

        /// <summary>HUD 캔버스</summary>
        public Canvas HudCanvas => _hudCanvas;

        /// <summary>던전 입구 밖</summary>
        public Transform DungeonOutside => _dungeonOutside;

        /// <summary>던전 안 시작 자리</summary>
        public Transform DungeonStart => _dungeonStart;

        /// <summary>던전 적 무리 자리</summary>
        public SpawnPointView[] DungeonSpawnPoints => _dungeonSpawnPoints;

        /// <summary>보스 자리</summary>
        public Transform BossPoint => _bossPoint;

        /// <summary>보스 후보</summary>
        public MonsterSO[] BossChoices => _bossChoices;

        /// <summary>보스 프리팹</summary>
        public EnemyController BossPrefab => _bossPrefab;
```

---

### `Assets/Scripts/Flow/FieldState.cs`

**변경 내용:** 던전(드나들기·보관함·보스·새 던전), 떨어진 금화를 현장 규칙으로(고블린 줍기·떨굼·보관), 숲 70% 리셋(나갈 때 판정·들어올 때 연출), 날짜·의뢰 기간

클래스 요약 교체:

```csharp
    /// <summary>숲. 3구역 회색 숲에 마법사·용병·적(쓰러뜨린 적은 쓰러진 자리에 시체)과 1구역 고블린 던전(지하, 보스)을 두고 현장 시전·안개 지도·시계·12시 안내(전투 중엔 숨김)를 켬. 숲이 70% 넘게 빈 채 나갔으면 들어올 때 리셋 연출 뒤 시체가 일어남. 숲 입구에서 F로 귀환, 소탕 대상을 다 쓰러뜨리면 의뢰 목표 달성, 기간이 지나면 의뢰 실패. 죽으면 금화를 떨구고 덮개 밑에서 용병 도망을 빠르게 돌린 뒤 여관 방 침대(방이 없으면 정문)에서 부활</summary>
```

`GoldId` 아래 상수:

```csharp
        /// <summary>던전 입구 지점 id (들어가기)</summary>
        const string DungeonEnterId = "dungeon_enter";

        /// <summary>던전 안 나가는 문 지점 id</summary>
        const string DungeonExitId = "dungeon_exit";

        /// <summary>던전 보관함 지점 id</summary>
        const string StashId = "stash";

        /// <summary>숲 적 자리 대비 쓰러뜨린 적이 이 비율 이상이면 리셋 (기획 70%)</summary>
        const float ForestResetRatio = 0.7f;

        /// <summary>부하를 보스 둘레에 흩는 반지름 (m)</summary>
        const float SummonSpread = 6f;

        /// <summary>던전 무리 번호 시작 (숲 무리와 안 겹치게)</summary>
        const int DungeonPackBase = 1000;

        /// <summary>던전 적 키 형식 ({0} 던전 세대, {1} 자리 id, {2} 번호)</summary>
        const string DungeonKeyFormat = "dungeon.{0}.{1}.{2}";

        /// <summary>보스 키 형식 ({0} 던전 세대)</summary>
        const string BossKeyFormat = "dungeon.{0}.boss";
```

`MercenaryLostKey` 아래 키:

```csharp
        /// <summary>의뢰 기간이 지났을 때 토스트 키 ({0} 의뢰 이름)</summary>
        const string QuestExpiredKey = "field.quest_expired";

        /// <summary>보관함에서 금화를 챙겼을 때 토스트 키 ({0} 금화)</summary>
        const string StashTakenKey = "field.stash_taken";

        /// <summary>보관함이 비었을 때 토스트 키</summary>
        const string StashEmptyKey = "field.stash_empty";

        /// <summary>던전 보스를 쓰러뜨렸을 때 토스트 키</summary>
        const string DungeonClearedKey = "field.dungeon_cleared";
```

`_objectiveChanged` 아래 필드:

```csharp
        /// <summary>리셋 연출에서 일어날 숲 시체</summary>
        readonly List<EnemyController> _corpses = new();

        /// <summary>일어날 시체마다 돌아갈 집 (원래 적 자리)</summary>
        readonly Dictionary<EnemyController, Vector3> _corpseHomes = new();

        /// <summary>숲 적 자리 수 합 (70% 판정)</summary>
        int _forestTotal;

        /// <summary>이번 진입에 리셋 연출을 트는지</summary>
        bool _resetting;
```

`OnSceneReady` 교체:

```csharp
        #region OnSceneReady - 현장 규칙(보관 장소 = 던전 입구 밖)·안개 지도, 리셋 여부, 차원문 자리, 던전 준비, 마법사를 입구에, 용병·숲 적·던전 적·보스·떨어진 금화를 놓고, 금화 알림 구독, 시전·안개·시계·HUD·지도를 연결, 목표를 정함 (덮개 밑)
        protected override void OnSceneReady()
        {
            _view = FindView<FieldView>();
            if (_view == null) return;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            _service = new FieldService(_view.PlayerUnit) { StashPoint = _view.DungeonOutside };
            _fog = new FogMapService(_view.MapArea, run.ForestFog);
            run.ForestFog = _fog.Revealed;
            _resetting = run.ForestResetPending;
            PlacePortal(run);
            PrepareDungeon(run);
            _view.Player.PlaceAt(_view.Entry);
            _view.PlayerUnit.Init(PlayerMaxHp);
            SpawnParty(run);
            SpawnEnemies(run);
            SpawnDungeon(run);
            SpawnGold(run);
            _service.LootTaken += OnLootTaken;
            _service.LootDropped += OnLootDropped;
            _service.LootStored += OnLootStored;
            _view.Cast.Init(run, DataManager.Instance.Spells, DataManager.Instance.Runes, _service, _view.Player, _view.PlayerUnit);
            _view.FogReveal.Init(_fog);
            _view.Clock.Init(run);
            _view.HudBinder.Bind(_view.PlayerUnit, _view.Player, _view.Cast, _mercs, _service, _fog, ZoneAt, () => Game.Run.Day);
            _view.Hud.Minimap.Bind(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position);
            UIManager.Instance.SetMapSource(_fog, _view.Player.transform, ObjectivePosition, _view.Entry.position);
            _view.Hud.InteractPrompt.gameObject.SetActive(false);
            if (!_resetting && QuestActive(run) && _targets.Count == 0) run.QuestCleared = true;
            _objectiveChanged = UpdateObjective(false);
        }
        #endregion
```

`Reveal` 교체 + `StartControl` 새로:

```csharp
        #region Reveal - 커서 잠금, 지점·죽음·전투 상황·시계·M 구독. 리셋이면 HUD를 끄고 연출 → 끝나면 OnResetFinished, 아니면 바로 조작 시작 (로딩창을 넘길 때 누른 키는 구독 전이라 안 들어감)
        public override void Reveal()
        {
            if (_view == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            _view.Detector.TargetChanged += OnTargetChanged;
            _view.Detector.Interacted += OnInteracted;
            _view.PlayerUnit.Died += OnPlayerDied;
            _service.CombatChanged += OnCombatChanged;
            _view.Clock.Expired += OnQuestExpired;
            _map = InputSystem.actions.FindAction(MapActionPath);
            if (_map != null) _map.performed += OnMapPressed;
            else Debug.LogError($"[Field] {MapActionPath} 액션 없음. 지도 튜토리얼을 못 배움");
            if (_resetting)
            {
                _view.HudCanvas.enabled = false;
                _view.ResetSequence.Play(_view.Portal, _corpses, _view.Player.transform, OnResetFinished);
                return;
            }
            StartControl();
        }
        #endregion

        #region StartControl - 조작 켬, 안 배운 12시 안내 알림, 들어올 때 목표가 바뀌었으면 알림
        void StartControl()
        {
            SetControl(true);
            TutorialManager.Instance.Notify(MapTutorialId);
            if (_objectiveChanged) NotifyObjective();
        }
        #endregion
```

`Exit`의 `_view != null` 블록 안, `_view.PlayerUnit.Died -= OnPlayerDied;` 아래:

```csharp
                _view.Clock.Expired -= OnQuestExpired;
                _view.HudCanvas.enabled = true;
```

`OnInteracted` 교체:

```csharp
        #region OnInteracted - 처음 쓰면 상호작용 배움. 숲 입구면 귀환, 떨어진 금화면 줍기, 던전 입구·나가는 문이면 드나들기, 보관함이면 챙기기. 모르는 id는 경고
        /// <param name="target">상호작용 키를 누를 때 바라본 지점</param>
        void OnInteracted(InteractPointView target)
        {
            TutorialManager.Instance.MarkLearned(InteractTutorialId);
            switch (target.Id)
            {
                case ExitId: Return(); break;
                case GoldId: PickGold(); break;
                case DungeonEnterId: MoveParty(_view.DungeonStart); break;
                case DungeonExitId: MoveParty(_view.DungeonOutside); break;
                case StashId: TakeStash(); break;
                default: Debug.LogWarning($"[Field] 모르는 지점 id: {target.Id} ({target.name})"); break;
            }
        }
        #endregion
```

`Return`:

```diff
             if (_dying) return;
+            MarkLeaving(Game.Run);
             Game.Run.ReturningFromField = true;
```

`PickGold` 교체 (지점은 `OnLootTaken`이 지움):

```csharp
        #region PickGold - 떨어진 금화를 다 주워 금화에 더함 (지점은 금화 알림이 지움)
        void PickGold()
        {
            // 주운 금화
            int gold = _service.TakeLoot();
            if (gold <= 0) return;
            Game.Run.Gold += gold;
            UIManager.Instance.ShowToast(Table, GoldPickedKey, gold);
        }
        #endregion
```

`SpawnEnemies`: 숲 적 자리 수를 세고, 리셋이면 시체를 일어날 목록에

```diff
                 pack++;
+                _forestTotal += point.Count;
                 for (int i = 0; i < point.Count; i++)
```

```diff
                     if (dead)
                     {
                         enemy.InitCorpse(point.Monster, key, pack);
+                        if (_resetting)
+                        {
+                            _corpses.Add(enemy);
+                            _corpseHomes[enemy] = point.transform.position;
+                        }
                         continue;
                     }
```

`SpawnGold`:

```diff
             _goldPile = Object.Instantiate(_view.GoldPilePrefab, run.DroppedGoldPosition, Quaternion.identity);
+            _service.SetLoot(_goldPile.transform, run.DroppedGold);
```

`DropGold`:

```diff
             if (_goldPile != null) Object.Destroy(_goldPile.gameObject);
             _goldPile = null;
+            _service.SetLoot(null, 0);
```

`DeathRoutine` 끝:

```diff
             Game.SetGameSpeed(1f);
             _death = null;
+            MarkLeaving(Game.Run);
             Game.Run.RespawningFromDeath = true;
```

`DeathRoutine` region 아래 새 메서드:

```csharp
        #region OnResetFinished - 리셋 연출이 끝남: 시체를 원래 적 자리를 집으로 살리고(소탕 대상이면 다시 셈), 쓰러진 기록·리셋 표시를 지움, HUD를 켜고 조작 시작
        void OnResetFinished()
        {
            if (_view == null) return;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            foreach (EnemyController corpse in _corpses)
            {
                if (corpse == null) continue;
                corpse.Revive(_service, _corpseHomes.TryGetValue(corpse, out Vector3 home) ? home : corpse.transform.position);
                corpse.Died += OnEnemyDied;
                if (IsQuestTarget(run, corpse.Monster)) _targets.Add(corpse);
            }
            _corpses.Clear();
            _corpseHomes.Clear();
            run.ForestDead.Clear();
            run.ForestResetPending = false;
            _resetting = false;
            if (QuestActive(run) && _targets.Count == 0) run.QuestCleared = true;
            if (UpdateObjective(false)) _objectiveChanged = true;
            _view.HudCanvas.enabled = true;
            StartControl();
        }
        #endregion

        #region PrepareDungeon - 던전이 아직 없거나 새로 만들어야 하면: 세대를 올리고 쓰러진 기록·클리어·보관 금화를 비우고 보스를 후보 중 무작위로 고름 (모양은 고정, 무작위 형태는 보류)
        /// <param name="run">이번 판 데이터</param>
        void PrepareDungeon(RunEntry run)
        {
            if (run.DungeonBossId != null && !run.DungeonNeedsReset) return;
            run.DungeonGeneration++;
            run.DungeonDead.Clear();
            run.DungeonCleared = false;
            run.DungeonNeedsReset = false;
            run.DungeonStashGold = 0;
            // 보스 후보
            MonsterSO[] choices = _view.BossChoices;
            // 고른 보스
            MonsterSO pick = choices != null && choices.Length > 0 ? choices[Random.Range(0, choices.Length)] : null;
            run.DungeonBossId = pick != null ? pick.Id : string.Empty;
        }
        #endregion

        #region SpawnDungeon - 던전 적 자리마다 수만큼 놓음 (쓰러진 키는 시체, 키에 던전 세대가 들어감), 보스를 놓음. 무리 번호는 숲과 안 겹치게
        /// <param name="run">이번 판 데이터</param>
        void SpawnDungeon(RunEntry run)
        {
            // 무리 번호 (던전 자리마다 하나)
            int pack = DungeonPackBase;
            foreach (SpawnPointView point in _view.DungeonSpawnPoints)
            {
                if (point == null || point.Monster == null)
                {
                    Debug.LogWarning("[Field] 비었거나 몬스터가 없는 던전 적 자리", point);
                    continue;
                }
                pack++;
                for (int i = 0; i < point.Count; i++)
                {
                    // 이 적 키
                    string key = string.Format(CultureInfo.InvariantCulture, DungeonKeyFormat, run.DungeonGeneration, point.Id, i);
                    // 쓰러졌는지, 쓰러진 곳
                    bool dead = run.DungeonDead.TryGetValue(key, out Vector3 corpseAt);
                    // 놓은 적
                    EnemyController enemy = Object.Instantiate(_view.EnemyPrefab, Sample(dead ? corpseAt : SpawnPosition(point)), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    if (dead)
                    {
                        enemy.InitCorpse(point.Monster, key, pack);
                        continue;
                    }
                    enemy.Init(point.Monster, _service, key, pack);
                    enemy.Died += OnDungeonEnemyDied;
                }
            }
            SpawnBoss(run, pack + 1);
        }
        #endregion

        #region SpawnBoss - 이번 던전 보스를 보스 자리에 놓음 (쓰러졌으면 시체). 패턴에 현장 규칙·부하 부르기를 넘김
        /// <param name="run">이번 판 데이터</param>
        /// <param name="pack">보스 무리 번호 (부하도 같음)</param>
        void SpawnBoss(RunEntry run, int pack)
        {
            // 보스 데이터
            MonsterSO boss = FindBoss(run.DungeonBossId);
            if (boss == null)
            {
                Debug.LogWarning($"[Field] 던전 보스 없음: {run.DungeonBossId}");
                return;
            }
            // 보스 키
            string key = string.Format(CultureInfo.InvariantCulture, BossKeyFormat, run.DungeonGeneration);
            // 쓰러졌는지, 쓰러진 곳
            bool dead = run.DungeonDead.TryGetValue(key, out Vector3 corpseAt);
            // 놓은 보스
            EnemyController enemy = Object.Instantiate(_view.BossPrefab, Sample(dead ? corpseAt : _view.BossPoint.position), _view.BossPoint.rotation);
            if (dead)
            {
                enemy.InitCorpse(boss, key, pack);
                return;
            }
            enemy.Init(boss, _service, key, pack);
            enemy.Died += OnBossDied;
            if (enemy.TryGetComponent(out BossPatternController pattern))
                pattern.Init(_service, (monster, near) => Summon(monster, near, pack));
        }
        #endregion

        #region FindBoss - 보스 후보에서 id로 찾기 (대소문자 구분). 없으면 null
        /// <param name="id">몬스터 id</param>
        MonsterSO FindBoss(string id)
        {
            if (string.IsNullOrEmpty(id) || _view.BossChoices == null) return null;
            foreach (MonsterSO monster in _view.BossChoices)
                if (monster != null && string.Equals(monster.Id, id, System.StringComparison.Ordinal)) return monster;
            return null;
        }
        #endregion

        #region Summon - 보스 둘레에 부하 하나를 놓고 마법사를 쫓게 함 (키가 없어 판 데이터에 안 남음)
        /// <param name="monster">부하 데이터</param>
        /// <param name="near">보스 자리</param>
        /// <param name="pack">보스 무리 번호</param>
        EnemyController Summon(MonsterSO monster, Vector3 near, int pack)
        {
            // 흩는 거리
            Vector2 offset = Random.insideUnitCircle * SummonSpread;
            // 놓은 부하
            EnemyController enemy = Object.Instantiate(_view.EnemyPrefab, Sample(near + new Vector3(offset.x, 0f, offset.y)), Quaternion.identity);
            enemy.Init(monster, _service, string.Empty, pack);
            enemy.Wake(_service.Player);
            return enemy;
        }
        #endregion

        #region OnDungeonEnemyDied - 던전 적이 쓰러진 곳을 판 데이터에 남김
        /// <param name="enemy">쓰러진 적</param>
        void OnDungeonEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnDungeonEnemyDied;
            Game.Run.DungeonDead[enemy.SpawnKey] = enemy.transform.position;
        }
        #endregion

        #region OnBossDied - 보스가 쓰러짐: 판 데이터에 남기고 던전 클리어, 토스트
        /// <param name="enemy">쓰러진 보스</param>
        void OnBossDied(EnemyController enemy)
        {
            enemy.Died -= OnBossDied;
            // 이번 판 데이터
            RunEntry run = Game.Run;
            run.DungeonDead[enemy.SpawnKey] = enemy.transform.position;
            run.DungeonCleared = true;
            UIManager.Instance.ShowToast(Table, DungeonClearedKey);
        }
        #endregion

        #region MoveParty - 화면이 덮인 사이 마법사·살아 있는 용병을 그 자리로 옮기고, 다 걷히면 조작을 돌려줌 (던전 드나들기)
        /// <param name="point">옮길 자리</param>
        void MoveParty(Transform point)
        {
            if (_dying || point == null) return;
            if (!UIManager.Instance.FadeThrough(() => PlaceParty(point), () => { if (!_dying) SetControl(true); })) return;
            SetControl(false);
        }
        #endregion

        #region PlaceParty - 마법사를 그 자리에, 살아 있는 용병을 그 뒤 양옆에 순간이동
        /// <param name="point">옮길 자리</param>
        void PlaceParty(Transform point)
        {
            if (_view == null) return;
            _view.Player.PlaceAt(point);
            for (int i = 0; i < _mercs.Count; i++)
            {
                // 이 용병
                MercenaryController merc = _mercs[i];
                if (merc == null || !merc.Unit.IsAlive) continue;
                // 자리 기준 뒤 양옆
                Vector3 offset = new((i % 2 == 0 ? -1f : 1f) * PartySpread, 0f, -PartySpread);
                merc.WarpTo(Sample(point.position + point.rotation * offset));
            }
        }
        #endregion

        #region TakeStash - 던전 보관 금화가 있으면 다 챙겨 금화에 더함 (없으면 비었다)
        void TakeStash()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            // 보관 금화
            int gold = run.DungeonStashGold;
            if (gold <= 0)
            {
                UIManager.Instance.ShowToast(Table, StashEmptyKey);
                return;
            }
            run.DungeonStashGold = 0;
            run.Gold += gold;
            UIManager.Instance.ShowToast(Table, StashTakenKey, gold);
        }
        #endregion

        #region OnLootTaken - 떨어진 금화가 주워짐 (플레이어·고블린): 지점을 지우고 판 데이터 떨어진 금화를 비움
        void OnLootTaken()
        {
            if (_goldPile != null) Object.Destroy(_goldPile.gameObject);
            _goldPile = null;
            Game.Run.DroppedGold = 0;
        }
        #endregion

        #region OnLootDropped - 들고 가던 몬스터가 쓰러져 금화를 떨굼: 그 자리(NavMesh 위)에 금화 지점을 놓고 판 데이터·현장 규칙에 남김
        /// <param name="position">떨군 곳</param>
        /// <param name="gold">금화</param>
        void OnLootDropped(Vector3 position, int gold)
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (_goldPile != null) Object.Destroy(_goldPile.gameObject);
            // 놓을 곳
            Vector3 at = Sample(position);
            run.DroppedGold = gold;
            run.DroppedGoldPosition = at;
            _goldPile = Object.Instantiate(_view.GoldPilePrefab, at, Quaternion.identity);
            _service.SetLoot(_goldPile.transform, gold);
        }
        #endregion

        #region OnLootStored - 몬스터가 금화를 던전에 보관함: 판 데이터 보관 금화에 더함
        /// <param name="gold">금화</param>
        void OnLootStored(int gold) => Game.Run.DungeonStashGold += gold;
        #endregion

        #region MarkLeaving - 숲을 나갈 때(입구 귀환·죽음): 숲 적 자리 대비 쓰러뜨린 적이 70% 이상이면 다음에 리셋, 던전을 클리어했으면 새 던전 표시·보관 금화는 사라짐
        /// <param name="run">이번 판 데이터</param>
        void MarkLeaving(RunEntry run)
        {
            if (_forestTotal > 0 && (float)run.ForestDead.Count / _forestTotal >= ForestResetRatio) run.ForestResetPending = true;
            if (!run.DungeonCleared) return;
            run.DungeonNeedsReset = true;
            run.DungeonStashGold = 0;
        }
        #endregion

        #region OnQuestExpired - 받은 의뢰의 기간이 지남: 의뢰 실패로 비움 (게시판에 다시 붙음), 소탕 대상·목표 정리, 토스트
        void OnQuestExpired()
        {
            // 이번 판 데이터
            RunEntry run = Game.Run;
            if (run.Quest == null || run.QuestCleared) return;
            // 실패한 의뢰
            QuestSO quest = run.Quest;
            run.Quest = null;
            run.QuestDeadline = 0f;
            _targets.Clear();
            UpdateObjective(false);
            UIManager.Instance.ShowToast(Table, QuestExpiredKey, quest.Title.GetLocalizedString());
        }
        #endregion
```

**호출/구독 관계 (더해진 것):**
- 구독: `FieldService.LootTaken`·`LootDropped`·`LootStored`, `FieldClockController.Expired`, 던전 적·보스 `EnemyController.Died`
- 부름: `ForestResetSequence.Play`, `EnemyController.Revive`, `BossPatternController.Init`, `MercenaryController.WarpTo`, `UIManager.FadeThrough`

---

### `Assets/Scripts/Flow/HubState.cs`

**변경 내용:** 처음 출발할 때 의뢰 기간을 정함, 의뢰를 받거나 끝내면 기간을 비움

`Depart`:

```diff
             run.Rations -= Mathf.Min(run.Rations, NeededRations());
             run.HasDeparted = true;
+            if (run.Quest != null && run.QuestDeadline <= 0f) run.QuestDeadline = run.PlaySeconds + run.Quest.PeriodDays * RunEntry.DaySeconds;
```

`TakeQuest`:

```diff
             Game.Run.Quest = quest;
             Game.Run.QuestCleared = false;
+            Game.Run.QuestDeadline = 0f;
```

`FinishQuestIfCleared`:

```diff
             run.Quest = null;
             run.QuestCleared = false;
+            run.QuestDeadline = 0f;
```

---

## 입력 변경

없음

---

## 에셋 변경

### `Assets/Art/Placeholder/Materials/` (새로, 재질 2개)

| 파일 | 셰이더 | 색 | 그 밖 |
|---|---|---|---|
| `Warning.mat` | URP Unlit | (1, 0.15, 0.1, 0.45) | Surface Transparent |
| `ManaBurst.mat` | URP Unlit | (0.6, 0.35, 1, 0.3) | Surface Transparent |

### `Assets/Data/Monsters/` (보스 2개 새로, 고블린 고침. 보스 수치는 기획 미정이라 임시)

| 파일 | `_id` | 체력 | 속도 | 피해 | 간격 | 거리 | 알아챔 | 크기 | 색 | 패턴 | 패턴 간격 | 예고 | 반지름 | 패턴 피해 | 부하 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `GoblinChampion.asset` | goblin_champion | 600 | 4.8 | 24 | 1.6 | 2.4 | 30 | 1.8 | (0.18, 0.36, 0.15) | Slam | 6~8 | 1.1 | 5 | 38 | Goblin × 3 |
| `GoblinShaman.asset` | goblin_shaman | 400 | 4.5 | 10 | 1.5 | 1.6 | 30 | 1.3 | (0.45, 0.2, 0.55) | FireBolt | 2.5~3.5 | 0 | 0 | 12 | Goblin × 3 |

- 이름: Monster/`monster.goblin_champion.name`·`monster.goblin_shaman.name`. 마법사 노림 0, 줍기 끔
- `Goblin.asset`: `_picksUpItems` 켬

---

## Prefab 변경

### `Assets/Prefabs/World/Boss.prefab` (새로, Enemy.prefab 변형)

1. `Visual/Muzzle` 빈 오브젝트 (0, 1.6, 0.6)
2. 루트에 Add Component → `BossPatternController` (`_enemy` → 루트 EnemyController, `_telegraphPrefab` = Telegraph.prefab, `_boltPrefab` = SpellProjectile.prefab, `_muzzle` → Visual/Muzzle)

### `Assets/Prefabs/World/Telegraph.prefab` (새로)

루트 `Telegraph` (스케일은 코드가 지름으로) 아래 `Disc` Cylinder (위치 (0, 0.02, 0), 스케일 (1, 0.01, 1)) `Warning.mat`, 콜라이더 지움

### `Assets/Prefabs/World/ManaStream.prefab` (새로)

루트 `ManaStream` + LineRenderer (Positions 2, Width 0.3, Use World Space 켬, `ManaBurst.mat`, Cast Shadows 끔)

---

## 씬 변경

### `Assets/Scenes/Forest.unity`

1. `Forest/DungeonEntrance` (1구역, 원점 (-70, 0, 130), 입구가 남쪽 = 숲 입구 쪽):
   - `RockLeft` Cube (-72.5, 2, 130) (2, 4, 3), `RockRight` Cube (-67.5, 2, 130) (2, 4, 3), `Lintel` Cube (-70, 4.5, 130) (7, 1, 3) — `Road.mat`, BoxCollider
   - `Mouth` Cube (-70, 1.5, 128.6) (3, 3, 0.2) `Portal.mat`, BoxCollider + `InteractPointView` (`dungeon_enter`, Field/`field.dungeon_enter`)
   - `Outside` (-70, 0, 125), yaw 180 (던전에서 나오면 서는 곳, 고블린 보관 장소)
   - 이 둘레 10m 안 나무·바위를 지움
2. `Dungeon` (0, -60, 0) + NavMeshSurface (Humanoid, Collect Objects: Current Object Hierarchy, Physics Colliders). 아래 로컬 좌표, 바닥 `Ground.mat`(두께 0.2), 벽 `Road.mat`(높이 4, 두께 0.4), 천장 `Roof.mat`. 방과 복도는 맞닿는 벽에 폭 4m 문 구멍:
   - `Hall` 12 × 12, 가운데 (0, 0, 0) / `CorridorA` 4 × 18, (0, 0, 15) / `RoomA` 16 × 16, (0, 0, 32) / `CorridorB` 4 × 14, (0, 0, 47) / `RoomB` 16 × 16, (0, 0, 62) / `CorridorC` 4 × 14, (0, 0, 77) / `BossRoom` 24 × 24, (0, 0, 96)
   - 방마다 Point Light (천장 아래 3m, 세기 2, 범위 14, 색 (1, 0.75, 0.5))
   - `Start` (0, 0, -3), yaw 0 / `ExitDoor` Cube (0, 1.1, -5.9) (2, 2.2, 0.2) `Wood.mat` + `InteractPointView` (`dungeon_exit`, Field/`field.dungeon_exit`)
   - `Spawns` 아래 SpawnPointView: `dgoblin_a` (0, 0, 32) Goblin 3 흩음 4, `dgoblin_b` (0, 0, 62) Goblin 4 흩음 4
   - `BossPoint` (0, 0, 100), yaw 180
   - `Stash` Cube (9, 0.4, 104) (1.4, 0.8, 0.9) `Wood.mat`, BoxCollider + `InteractPointView` (`stash`, Field/`field.stash`)
3. `ResetSequence` (원점) + `ForestResetSequence`: 자식 `Camera` (Camera, Depth 10, AudioListener 없음, 꺼 둠), `Burst` Sphere (`ManaBurst.mat`, 콜라이더 지움, 꺼 둠). 칸: `_camera` → Camera, `_burst` → Burst, `_streamPrefab` = ManaStream.prefab, `_portalView` (0, 10, -24), `_corpseView` (0, 4, -9), `_burstRadius` 60
4. `Field`: Add Component → `FieldClockController`. FieldView 새 칸: `_clock` → Field, `_resetSequence` → ResetSequence, `_hudCanvas` → Canvas, `_dungeonOutside` → DungeonEntrance/Outside, `_dungeonStart` → Dungeon/Start, `_dungeonSpawnPoints` = dgoblin_a·dgoblin_b, `_bossPoint` → Dungeon/BossPoint, `_bossChoices` = GoblinChampion·GoblinShaman, `_bossPrefab` = Boss.prefab
5. NavMeshSurface 둘 다 Bake (Forest는 동굴 입구를 넣고 다시, Dungeon은 새로 `Assets/Scenes/Forest/NavMesh-Dungeon.asset`)
6. 저장 → 다시 열어 FieldView 새 칸 9개, 지점 id·이름 키, NavMesh 데이터 2개 확인

---

## 다국어 변경

1. `Field` 탭: 1줄 고침 + 7줄 추가

| Key | Comment | ko | en |
|---|---|---|---|
| `field.zone_line` (고침) | 숲 HUD 왼쪽 위. {0} 구역 이름, {1} 날짜, {2} 지도 % | {0} · {1}일째 · 지도 {2}% | {0} · Day {1} · Map {2}% |
| `field.quest_expired` | 토스트: 의뢰 기간이 지남. {0} 의뢰 이름 | 의뢰 기간이 지났다: {0} | The quest has expired: {0} |
| `field.dungeon_enter` | 던전 입구 지점 이름 | 고블린 동굴 | Goblin Cave |
| `field.dungeon_exit` | 던전 나가는 문 지점 이름 | 동굴 밖으로 | Leave the Cave |
| `field.stash` | 던전 보관함 지점 이름 | 고블린이 모은 물건 | Goblin Hoard |
| `field.stash_taken` | 토스트: 보관함에서 챙김. {0} 금화 | 되찾은 금화: {0} | Recovered gold: {0} |
| `field.stash_empty` | 토스트: 보관함이 빔 | 비어 있다 | It's empty |
| `field.dungeon_cleared` | 토스트: 던전 보스를 쓰러뜨림 | 고블린 동굴을 정리했다 | The goblin cave is cleared |

2. `Monster` 탭: `monster.goblin_champion.name` 고블린 챔피언 / Goblin Champion, `monster.goblin_shaman.name` 고블린 대주술사 / Goblin Grand Shaman
3. `Menu` 탭: `menu.journal.quest_left` (임무 일지 의뢰 줄, 기간을 세는 중. {0} 의뢰 이름, {1} 남은 날) 의뢰: {0} (남은 기간 {1}일) / Quest: {0} ({1} days left)
4. CSV: Field·Monster·Menu → `Assets/Localization/CSV/` → `Import Tables from CSV` → `Check Missing Translations`·`Check Missing Glyphs`

---

## 주의사항

- 숲 적 키와 던전 적 키를 따로 남김 (`ForestDead`·`DungeonDead`). 70%·소탕 대상은 숲 것만. 부른 부하는 키가 없어 판 데이터에 안 남음
- 리셋 연출 동안 HUD는 `Canvas.enabled`만 끔. Hud 오브젝트를 끄면 그리기 칸 `SpellDrawController`가 `End`돼서 판정 함수를 잃음
- 연출 카메라는 플레이어 카메라보다 Depth가 높고 AudioListener가 없음 (리스너 둘 경고 방지). 연출 동안 조작은 꺼져 있고 살아 있는 적은 그대로 움직임
- 던전 드나들기는 `UIManager.FadeThrough`(실제 시간), 덮는 동안 조작 끔. 용병은 `NavMeshAgent.Warp`. 위아래로 떨어져 쫓던 적은 46m 넘어서 포기
- 고블린은 쉴 때만 떨어진 금화를 봄 (쫓는 중엔 안 주움). 들고 싸우다 쓰러지면 그 자리에 떨굼. 보관 장소는 던전 입구 밖 자리 (`FieldService.StashPoint`)
- 떨어진 금화는 하나뿐. 플레이어가 줍든 고블린이 줍든 `FieldService.TakeLoot` → `LootTaken`이 지점을 지우고 `DroppedGold`를 0으로
- 시간은 숲 `FieldClockController`만 흐르게 함. 게임 시간이라 메뉴·로딩창·대화·설명창에서 멈추고, 죽은 뒤 도망 시뮬에선 6배
- 의뢰 기간은 처음 출발할 때 정하고(`QuestDeadline`), 받을 때·끝낼 때 0. 목표를 이미 끝냈으면 기간이 지나도 실패 안 함
- 보스 수치는 기획 미정이라 임시. 6-1 `EnemyController.Setup`이 콜라이더·NavMeshAgent 크기를 몬스터 크기에 맞추므로 Boss.prefab은 크기를 따로 안 잡음
- 던전 자리 id나 숲 적 자리 id를 바꾸면 앞 판 기록과 안 맞음

---

## 진행 추적

- [x] 묶음 Logic — 새로: BossPatternController, FieldClockController, ForestResetSequence / 고침: RunEntry, QuestSO, MonsterSO, FieldService, EnemyController, MercenaryController, FieldHudUIBinder, JournalUIBinder, FieldView, FieldState, HubState (+ 아래 '다르게 한 것'의 FogMapService). 컴파일 에러·경고 0
- [x] 묶음 Editor-Localization — Field·Monster·Menu 줄, 시트(읽어서 대조), CSV, Import(키 수 Unity=CSV), 빠진 글자 없음
- [x] 묶음 Prefab·Data — 재질 2개, Boss(Enemy 변형)·Telegraph·ManaStream 프리팹, 보스 2개·고블린 줍기
- [x] 묶음 Editor-Scene — Forest(동굴 입구·지하 던전·리셋 연출·시계·FieldView 칸 25개 다 연결, NavMesh 둘 굽기·길 확인), 저장 뒤 다시 열어 확인
- [ ] 묶음 Verify — 사용자 확인

구현 중 plan과 다르게 한 것 (2026-11-24):
- **층 분리** (plan에 없던 보정): 던전이 숲 바로 아래(같은 x·z, y -60)라 적·용병·피해 찾기가 평면 거리만 보면 층을 넘어 알아채고 때림. `FieldService.LevelGap`(8m)·`SameLevel`을 만들고 `Nearest`·`FindInRadius`·`FindNearSegment`가 같은 층만 찾게 함. `EnemyController.UpdateChase`는 대상과 층이 다르면 포기 (던전 드나들 때 쫓던 적). `FogMapService.Reveal`은 숲 층(|y| ≤ 8)에서만 밝힘 (던전 안을 다녀도 숲 지도가 안 밝혀짐). 던전 안 미니맵·지도 표시는 숲 좌표 그대로라 의미 없음 (보류)
- `EnemyController.OnDamaged`: 쉬는 중뿐 아니라 금화 줍는 중·가져가는 중에 맞아도 무리를 깨움 (새 상태 Fetch·Carry가 안 깨던 구멍)
- `FieldState.Reveal` 리셋 분기에서 연출 전에 `SetControl(false)` (Forest 씬은 Player 스크립트가 켜진 채 저장돼 있어서 연출 중 조작이 살아 있었음)
- `FieldCastController.Init`의 마지막 인자 `_view.Detector`는 시전 방식 변경 뒤 코드 그대로 둠 (위 `OnSceneReady` 스니펫은 6개)
- 던전 점광은 plan '천장 아래 3m'를 높이 3m로 읽음. 동굴 입구 Mouth(높이 3)는 plan 값 그대로라 위쪽 1m가 비어 하늘이 보임 (임시 도형)
- 던전 NavMesh는 `Assets/Scenes/Forest/NavMesh-Dungeon.asset`, 숲 NavMesh는 동굴 입구를 넣고 다시 구움 (Start → 방A → 방B → 보스 자리 길 확인, 숲과 던전은 안 이어짐)

---

## 확인 목록 (사용자, 에디터 Play)

상황 만들기는 확인 패널 (`Tools > Mage > 확인 패널`, Play 중). 패널이 이 목록을 읽어 번호마다 버튼을 붙임: 줄 하나에 한 가지 (무엇을 하면 → 무엇이 보임), [ ] 안은 패널 버튼, 패널 '…'은 패널 상태 줄. ✅ 확인함 (패널 칩 처음 값). 2026-11-25 남은 항목은 Claude가 숲 씬 Play로 확인: 패널 버튼과 같은 메서드를 부름, Esc·F 키와 걷기는 같은 처리 함수·이동 경로로 대신 (진짜 키 입력은 사용자 확인 남음)

1. 날짜
   1-1. ✅ 숲 왼쪽 위 "숲 외곽 · 1일째 · 지도 N%"
   1-2. ✅ [다음 날 직전으로] → 3초 뒤 HUD 날짜 +1 (하루 10분)
   1-3. ✅ Esc 메뉴를 연 채로 → 패널 '시계' 초가 멈춤, 닫으면 다시 흐름
2. ✅ 새 게임에서 의뢰를 받으면 임무 일지 "의뢰: 숲 외곽 정리 (임시) (3일)", 출발하면 "(남은 기간 3일)". 숲에서 3일(30분)을 못 끝내고 넘기면 "의뢰 기간이 지났다: …" → 임무 일지 의뢰 줄·목표 없음 → 전초기지 게시판에서 다시 받을 수 있음 [기간 5초 남기기]
3. 떨어진 금화
   3-1. ✅ [고블린 옆에 금화 100] → 그 고블린이 주우러 감, 주우면 금화 지점이 사라짐 (패널 '떨어진 금화' 없음). 고블린 18m 안으로 가면 금화 대신 파티를 쫓음
   3-2. ✅ 주운 고블린이 동굴 입구(던전 입구 밖)로 가져감 → 패널 '던전' 보관 +100
   3-3. ✅ 나르는 중에 [금화 든 적 쓰러뜨리기] → 그 자리에 "[F] 떨어진 금화"
   3-4. ✅ 금화를 가진 채 [죽기] → 여관 침대에서 깸 → [경비병과 말하기]로 출발 → 숲에 들어오면 패널 '떨어진 금화'가 죽을 때 가진 금화, 그 자리에 금화 지점
4. ✅ 1구역 동굴 "[F] 고블린 동굴" → 화면이 잠깐 어두워졌다 던전 안 (용병도 따라옴), 나가는 문 "[F] 동굴 밖으로" → 동굴 입구 밖 [던전 입구 앞] [던전 안]
5. ✅ 던전: 방 둘에 고블린 무리, 보스방에 보스 하나 (챔피언이면 내 자리에 붉은 원 → 1.1초 뒤 강타, 대주술사면 불탄). 체력 절반에서 고블린 3마리. 보스를 쓰러뜨리면 "고블린 동굴을 정리했다". 보관함 "[F] 고블린이 모은 물건" → 고블린이 가져간 금화가 있으면 "되찾은 금화: N", 없으면 "비어 있다" [보스방 앞] [보스 체력 절반] [보스 즉사]
6. 던전 다시 만들기
   6-1. ✅ [보관함에 금화 100] → [보스 즉사] → [나갔다 다시 들어오기] → 패널 '던전' 세대 +1·보관 0, 던전 적이 다시 있고 보스를 다시 고름, 보관함 [F] → "비어 있다"
   6-2. ✅ 클리어 전에 [보관함에 금화 100] → [나갔다 다시 들어오기] → 패널 '던전' 세대·보관 그대로, 쓰러뜨린 던전 적은 시체 그대로
   6-3. ✅ 클리어 전 보관 금화가 있을 때 [죽기] → [경비병과 말하기]로 출발 → 패널 '던전' 보관 그대로
7. 숲 리셋
   7-1. ✅ [숲 적 70% 쓰러뜨리기] → 패널 '리셋까지' 0마리 더 → [나갔다 다시 들어오기] → 들어오자마자 HUD가 사라지고 연출 (차원문 맥동 → 보라 구가 퍼짐 → 차원문에서 시체마다 줄기 → 가까운 시체 쪽에서 시체들이 일어남, 약 6초)
   7-2. ✅ 연출이 끝나면 시체가 다 일어나 원래 자리 둘레를 돌아다님, HUD·조작 돌아옴, 패널 '숲 적' 쓰러뜨림 0
   7-3. ✅ [리셋 한 마리 전까지] → 패널 '리셋까지' 1마리 더 → [나갔다 다시 들어오기] → 리셋 없음 (시체 그대로)
8. 소탕 대상 다시 셈
   8-1. ✅ 의뢰를 못 끝낸 채 [고블린 1마리만 남기고 70%] → 패널 '의뢰' 남은 소탕 대상 1 → [나갔다 다시 들어오기] → 리셋 연출 뒤 남은 소탕 대상 9, 임무 일지 목표 그대로 (소탕)
9. ✅ 영어로 바꾸면 던전·날짜 글자 영어
10. ✅ 콘솔 에러·경고 없음 (AudioListener 둘·NavMesh 경고 포함)
11. 층 분리 (던전 바로 위 숲: x ±12, z 0~108)
   11-1. ✅ [방A 위 숲]에서 걸어 다님 → 패널 '쫓는 적' 다른 층 0 (던전 고블린이 안 깸)
   11-2. ✅ [던전 안]에서 걸어 다님 → 패널 '쫓는 적' 다른 층 0 (숲 위 적이 안 옴)
   11-3. ✅ 던전 안을 걸은 뒤 M → 숲 지도가 더 안 밝혀짐
12. 추격 포기
   12-1. ✅ [던전 입구 앞] → [근처 적 깨우기] → 패널 '쫓는 적' 내 층 N (46m 안 적만)
   12-2. ✅ 쫓기는 채 "[F] 고블린 동굴"로 들어감 → 패널 '쫓는 적' 0, 다시 나오면 쫓던 적이 입구 근처에 서 있음 (따라 들어오지 않음)

---

## 커밋 메시지

```
Add — 6-2 숲: 고블린 던전·물건 줍기, 숲 70% 리셋 연출, 날짜·의뢰 기간

- 고블린 던전: 1구역 동굴 입구 → 숲 지하 고정 던전(방 둘 + 보스방), 보스는 챔피언(예고 원 강타)·대주술사(불탄) 중 하나, 체력 절반에서 부하. 보스를 쓰러뜨리면 클리어, 클리어하고 나갔다 오면 새 던전 (며칠 조건 보류)
- 물건 줍기: 고블린이 떨어진 금화를 주워 동굴 입구로 가져가 보관, 들고 쓰러지면 그 자리에 떨굼. 보관함에서 되찾음, 클리어 뒤 안 챙기면 사라짐
- 숲 리셋: 숲 적의 70% 이상을 쓰러뜨리고 나가면 다음 진입 때 연출(차원문 맥동 → 마력 분출 → 시체로 스며듦 → 일어남) 뒤 시체가 다 살아남
- 날짜: 하루 10분 (숲에서만 흐름, 임시). 의뢰 기간은 처음 출발부터, 지나면 의뢰 실패 → 게시판에 다시. 임무 일지에 남은 기간
- 층 분리: 던전(숲 지하)과 숲은 같은 x·z라도 다른 층이라 서로 못 알아채고 못 때림 (높이 8m 넘으면 다른 층), 던전 안에선 숲 지도를 안 밝힘. 줍는·가져가는 중에 맞아도 무리가 깸

Why: 6단계 결정 중 6-1에서 미룬 것 (기획 12 결정 70·72·80·82·79). 숲이 고정이라 비면 다시 채울 길과, 죽음 뒤 물건을 되찾는 길이 필요함
Alternatives: 던전을 별도 씬으로(드나들 때마다 로딩·숲 상태 저장이 늘어 뺌), 리셋을 숲 안에서 바로(소탕 중 고블린이 살아나 사용자가 '숲 밖에서'로 정함)
```
