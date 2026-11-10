---
name: plan
description: plan 작성. 구현에 필요한 내용만 집중 작성.
arguments: [topic]
allowed-tools: Read Glob Grep Write Agent Bash(git diff*) Bash(git diff)
user-invocable: true
---

plan을 작성해줘. 주제: $topic

## 절차

0. **최근 변경 검토** (`git diff HEAD`)
   - 위반 없으면 → 다음 단계
   - 위반 있으면 → plan에 `## Ladder 수정` 섹션 추가 (위반 항목 + surgical fix 코드만 포함)

1. **관련 파일 탐색**
   - Read/Grep/Glob으로 직접 탐색 (처음 다루는 시스템은 `## 현행 분석` 섹션 추가)

2. **구현 예시 대화 제시** (plan 파일 작성 전 필수)

   파일 탐색 완료 후, plan 파일을 작성하기 전에 아래 형식으로 대화에서 먼저 제시:

   ```
   ## 구현 예정

   **C# 변경**
   - `경로/File.cs` — 변경 요약 1줄
   - `경로/Other.cs` — 변경 요약 1줄

   **Prefab 변경**
   - `경로/UI.prefab` — 변경 요약 1줄

   확인되면 plan 파일 작성합니다.
   ```

   사용자가 확인하면 → plan 파일 작성  
   수정 요청 시 → 방향 조정 후 재제시

3. **`docs/plan-{kebab-case-topic}.md` 작성** (아래 구조)

4. "plan 작성 완료"만 말하고 즉시 멈춤

---

## 파일명 컨벤션

- 경로: `docs/plan-{kebab-case-topic}.md`
- 한글 주제 → 영문 kebab-case 변환 (예: "골드 보너스" → `plan-gold-bonus.md`)
- followup 짝: `docs/plan-{topic}-followup.md`

---

## plan 템플릿

```markdown
# Plan: {제목}

## C# 변경

### `Assets/Scripts/Domain/ClassName.cs`

**추가/변경 내용:**
- 구체적 변경 사항

```csharp
// 추가하거나 변경할 코드 전문
// 위치(메서드명, 몇 번째 줄 등) 명시
public event Action OnFoo;

void Bar() {
    // 기존 코드 ...
    OnFoo?.Invoke(); // ← 여기 추가
}
```

**호출/구독 관계:**
- 어디서 호출되는지, 누가 구독하는지

---

### `Assets/Scripts/Domain/OtherClass.cs`

(위와 동일 구조 반복)

---

## Prefab 변경

### `Assets/Prefabs/경로/UI.prefab`

1. 단계별 Inspector 조작 순서
2. Add Component → `ComponentName`
3. SerializeField 연결: A 필드 → B 오브젝트 드래그

---

## 주의사항

- 구현 중 놓치기 쉬운 것만 (호출 순서, null 체크, 기존 패턴 예외 등)

---

## 진행 추적

- [ ] 묶음 Logic — 설명
- [ ] 묶음 Prefab — 설명

---

## 커밋 메시지

```
{Type} — {제목}

{본문: Why: 포함, 대안 있었으면 Alternatives: 포함}
```
```

---

## 상세도 기준

사용자가 plan만 보고 질문 없이 구현 가능한 수준:
- 수정 파일 경로 + 대상 클래스/메서드 명시
- 추가/변경 코드 전문 (before/after 또는 삽입 위치 명시)
- Prefab Inspector 조작은 단계별 번호로
- 호출자·구독자 등 연결 관계 명시

---

## 현행 분석 포함 조건

- 처음 다루는 시스템/레이어
- 기존 구조 파악이 필요한 변경
- architecture에 관련 문서 없음

위 경우 `## 현행 분석` 섹션을 plan 앞에 추가.

---

## 금지

- 코드 구현 절대 금지
- 배경/의도/docs 참조/ADR 언급 plan 본문 포함 금지
- ExitPlanMode, AskUserQuestion 호출 금지
- "승인하시겠습니까?", "진행할까요?" 등 재촉 금지
- plan 작성 후 아무것도 하지 않음
