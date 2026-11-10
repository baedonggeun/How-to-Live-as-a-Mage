#!/bin/bash
# session-init.sh — SessionStart: 브랜치/날짜 안내 + 응답 규칙 주입

set -e

cd "$CLAUDE_PROJECT_DIR" 2>/dev/null || true

BRANCH=$(git branch --show-current 2>/dev/null || echo "unknown")
TODAY=$(date +%Y-%m-%d)

RULES=$(cat <<'RULES'
## 응답 규칙 (항상 적용)

### 말투
한국어 응답은 반말 (해/해줘/됨/아님). 존댓말 금지.

### 압축
제거: 관사(a/an/the), 필러(just/really/basically/actually), 인사말(sure/certainly/물론이죠/~해드릴게요), 헤징(~일 수도/~하는 게 좋을 것 같아요). 단문 OK. 기술 용어·코드·에러 문자열: 원문 유지. 짧은 동의어(big ← extensive, fix ← "솔루션을 구현").

패턴: `[대상] [동작] [이유]. [다음 단계].`

❌ "네, 물론이죠! 말씀하신 문제는 아마도 인증 미들웨어에서 발생하는 것 같은데요..."
✅ "auth 미들웨어 버그. 토큰 만료 체크 `<` → `<=` 수정:"

### 예외 (압축 해제)
- 보안 경고
- 되돌릴 수 없는 작업 확인 (git reset --hard 등)
- 단문 순서 모호로 오해 가능한 다단계 시퀀스

### 워크플로우 (순서 엄수)
(1) 파일 탐색 → (2) 작업 계획 제시 → [승인 대기] → (3) 작업 → (4) 커밋(사용자)

### 코드 작성 전 Ladder (full)
코드 작성 전 Ladder 각 단계를 번호와 함께 출력하고 결론을 낸 뒤 작성. 출력 없이 코드 작성 금지.
1. 이게 필요한가? → NO: 건너뜀 (YAGNI)
2. 코드베이스에 이미 있나? → 기존 Service/Manager/Pool 재사용
3. Unity 내장 API로 되나? → UnityEngine.*, Physics, Animation, AI, UGUI, Timeline
4. 설치된 패키지로 되나? → URP, 2D Feature, Test Framework 등
5. C# stdlib로 되나? → LINQ, collections, System.*
6. 한 줄로 되나? → 한 줄
7. 그래도 안 되면 → 최소 구현

금지: 요청 없는 추상화, 보일러플레이트, 영리한 코드
절대 생략 금지: validation / 에러 핸들링 / 보안
RULES
)

MSG="세션 시작 — 브랜치: ${BRANCH} | 오늘: ${TODAY}

${RULES}"

ESCAPED=$(printf '%s' "$MSG" | jq -Rs .)
cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": ${ESCAPED}
  }
}
EOF
exit 0
