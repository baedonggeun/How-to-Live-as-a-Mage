# How-to-Live-as-a-Mage - Unity Project

## Project
- **Engine**: Unity 6000.3.9f1 (Unity 6), URP 17.3, Input System(새 입력만)
- **Platform**: Windows
- **Type**: 1인칭 3D 중세 판타지(용병 파티 의뢰·탐험·룬 조합 주문 그리기 전투, 가제 전초기지)
- **기획 문서**: `d:\obsidian\Vault\1_Projects\How to live as a mage\`(시작은 `docs/design/README.md`. `docs/prototype/outpost.html`은 v1 기지 운영판, 현장 전투·그리기 수치만 참고)
- **Unity MCP**: `unity-editor-mcp`. 에디터는 Unity Hub나 터미널 탭에서 열 것

## 4 Principles (Discipline)
1. **Think Before Coding** — 가정 표면화, plan으로 정렬
2. **Simplicity First** — 최소 코드, 추상화 도입 보류
3. **Surgical Changes** — 요청 라인만 수정, 주변 cleanup 금지
4. **Goal-Driven** — verification 가능한 성공 조건

## Naming Conventions

### UI 4축
| Suffix | Role | Examples |
|--------|------|----------|
| **View** | prefab 자식 참조 dumb holder (로직 없음). 필드는 `[SerializeField] private` + 읽기 전용 프로퍼티 | QueueSlotView, EnemyStatusItemView |
| **UIBinder** | UI 데이터 바인딩 (이벤트 → UI) | InventoryUIBinder, QueueUIBinder |
| **Controller** | 로직 위주 (연출 혼합 허용) | PlayerTurnController, GachaController |
| **Sequence** | 순수 연출 (비즈니스 로직 없음) | MapSequence, SynergyCounterShakeSequence |
| **Drawer** | UGUI `Graphic` 상속, 받은 점을 이어 선 메시로 그림 (로직 없음) | StrokeDrawer |

### 인프라 13종 허용 리스트
- **Manager** (MonoSingleton, Service 컴포지션) / **Service** (비즈니스 로직, MonoBehaviour 미상속) / **State** (FSM 노드, Enter/Update/Exit) / **SO** (ScriptableObject 데이터)
- **Effect** (Apply/Emit/Describe 3종 효과 1책임) / **Entry** (POCO 데이터) / **Pool** (데이터 추첨) / **Spawner** (풀 + 큐 + 코루틴 호스트)
- **Handler** (EventSystem 또는 Domain Action) / **Spec** (런타임 산출 materialized 컨테이너 — struct) / **Calculator** (static pure function 모음) / **단발 패턴**: Director / Core / Base / Filter / Detector / Router

### 룰
- **자가 점검**: 새 클래스 작성/리네이밍 시 위 표 자가 점검. 위배 시 명명/책임 재배치 후 진행
- **신규 접미사 도입**: 위 표 외 접미사가 필요하면 본 표 확장. 즉흥 명명 금지

## 다국어 (PaulowniaSky Localization Kit)
- 화면에 보이는 글자 하드코딩 금지 (코드·프리팹·SO). String Table 키로만 넣음
- UI 텍스트: `Assets/Localization/Prefabs/LocalizedText.prefab` (키 연결 + 언어별 폰트 교체 포함)
- SO·컴포넌트의 표시용 문자열 필드: `string` 대신 `LocalizedString`
- 키: `{영역}.{대상}[.{속성}]`, 소문자·`_` (예: `common.confirm`, `item.heal_potion.name`, `quest.forest_01.desc`)
- 문장 속 값은 자리표시자(`{0}`)로. 문자열 이어 붙이기 금지 (언어마다 어순이 다름). 이름 자리표시자(`{gold}`)·복수형은 그 항목 Smart 켬
- 비교·파싱·키 조합: `StringComparison.Ordinal`, `CultureInfo.InvariantCulture` (터키어 i 같은 문화권 차이 방지)
- 텍스트 원본: Google 시트 'How to Live as a Mage Localization' (탭 하나 = String Table 하나, 탭 이름 = 테이블 이름). 고친 뒤 탭을 CSV로 받아 `Assets/Localization/CSV/{테이블 이름}.csv`에 덮어씀 → `Tools > PaulowniaSky > Localization Kit > Import Tables from CSV` → Check Missing Translations·Check Missing Glyphs. CSV·테이블 창은 직접 고치지 않음 (고쳤으면 시트에도 옮김)
- 테이블은 영역별로 나눔 (정보가 섞이지 않게): `UI`는 공용 버튼·타이틀·설정·공용 창, 화면은 화면마다(`Creation`·`Opening`·`Hub`·`Field`), 콘텐츠는 SO 종류마다(`Rune`·`Spell`·`Quest`…). 새 영역은 새 테이블 + 시트 새 탭. UI에 남은 다른 영역 키는 그 영역 작업 때 옮김
- 언어 선택 UI: 키트 샘플(Package Manager > Samples > Language Dropdown)을 가져와서 꾸밈
- 키트: `Packages/com.paulowniasky.localization-kit/` (embedded). 사용법은 그 README

## 응답 규칙 (항상 적용)

### 말투
한국어 응답은 반말 (해/해줘/됨/아님). 존댓말 금지.

### 압축
제거: 관사(a/an/the), 필러(just/really/basically/actually), 인사말(sure/certainly/물론이죠/~해드릴게요), 헤징(~일 수도/~하는 게 좋을 것 같아요). 단문 OK. 기술 용어·코드·에러 문자열: 원문 유지. 짧은 동의어(big ← extensive, fix ← "솔루션을 구현").

패턴: `[대상] [동작] [이유]. [다음 단계].`

❌ "네, 물론이죠! 말씀하신 문제는 아마도..."
✅ "auth 미들웨어 버그. 토큰 만료 체크 `<` → `<=` 수정:"

### 예외 (압축 해제)
- 보안 경고
- 되돌릴 수 없는 작업 확인 (git reset --hard 등)
- 단문 순서 모호로 오해 가능한 다단계 시퀀스

## 코드 작성 Ladder (ponytail-full)
코드 작성 전 반드시 출력: YAGNI → 코드베이스 재사용 → Unity API → 패키지 → stdlib → 한 줄 → 최소 구현.
상세: `.claude/hooks/session-init.sh`

## 작업 방식
- 파일 생성·수정·삭제, 명령 실행(Unity 조작·설치·빌드 포함) 전: 대상 파일·변경 내용·검증 방법 먼저 보여주고 승인 후 진행
- 읽기·검색·상태 확인은 승인 없이 진행
- plan 파일은 사용자가 요청할 때만 (`/plan`)

## Plan 라우팅
- 모든 plan: `docs/plan-{kebab-topic}.md`. followup 짝: `docs/plan-{topic}-followup.md`
- 새 plan: `/plan {topic}`. 이어 진행은 사용자가 직접 `Read docs/plan-{topic}.md`

## 커밋
- 사용자가 직접 커밋. plan 있으면 끝 `## 커밋 메시지` 섹션 참고
- 커밋 본문에 항상 `Why:` 포함. 대안 검토했으면 `Alternatives:` 포함
