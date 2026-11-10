---
name: pclean
description: 완료된 plan 파일 자동 정리 (git rm). 인자: clean [plan-filename]
allowed-tools: Read Glob Bash(git diff *) Bash(git diff) Bash(git rm *) Bash(ls *) Bash(awk *) Grep
user-invocable: true
argument-hint: clean [plan-filename]
---

현재 변경 파일:
```
!`git diff --name-only 2>/dev/null`
```

사용자 인자: $ARGUMENTS

## plan 자동 정리 (stateless 식별)

인자 파싱:
- `clean {plan-filename}`: 해당 파일 직접 처리
- 인자 없음 또는 `clean`: 본문 PENDING==0 + followup PENDING==0인 plan 자동 스캔

```bash
PLAN_ARG=$(echo "$ARGUMENTS" | awk '{print $2}')

if [ -n "$PLAN_ARG" ]; then
  CANDIDATES="docs/$PLAN_ARG"
  [ ! -f "$CANDIDATES" ] && { echo "[Plan 정리] 인자 파일 없음: $CANDIDATES"; CANDIDATES=""; }
else
  CANDIDATES=$(for f in $(ls -t docs/plan-*.md 2>/dev/null | awk '!/-followup\.md$/'); do
    P=$(awk '/^- \[ \]/ {c++} END {print c+0}' "$f" 2>/dev/null)
    FU="${f%.md}-followup.md"
    FU_P=0
    [ -f "$FU" ] && FU_P=$(awk '/^- \[ \]/ {c++} END {print c+0}' "$FU" 2>/dev/null)
    if [ "$P" = "0" ] && [ "$FU_P" = "0" ]; then
      echo "$f"
    fi
  done)
fi

COUNT=$(echo "$CANDIDATES" | grep -c . 2>/dev/null || echo 0)

if [ "$COUNT" = "0" ]; then
  echo "[Plan 정리] 완료된 plan 없음 (PENDING > 0 또는 plan 미존재)"
elif [ "$COUNT" = "1" ]; then
  PLAN_FILE="$CANDIDATES"
  FOLLOWUP_FILE="${PLAN_FILE%.md}-followup.md"
  RM_FILES="$PLAN_FILE"
  [ -f "$FOLLOWUP_FILE" ] && RM_FILES="$RM_FILES $FOLLOWUP_FILE"
  if git rm $RM_FILES >/dev/null 2>&1; then
    echo "[Plan 정리] $RM_FILES 자동 rm — git history로 보존"
  else
    echo "[Plan 정리 실패] $RM_FILES — 수동 git rm 필요"
  fi
else
  echo "[Plan 정리] 완료된 plan 후보 ${COUNT}건 — 사용자 선택 필요:"
  echo "$CANDIDATES"
  echo "→ '/pclean clean {plan-filename}'으로 명시 호출하라"
fi

# 본문 완료 / followup 잔여 안내
if [ -z "$PLAN_ARG" ]; then
  for f in $(ls -t docs/plan-*.md 2>/dev/null | awk '!/-followup\.md$/'); do
    P=$(awk '/^- \[ \]/ {c++} END {print c+0}' "$f" 2>/dev/null)
    FU="${f%.md}-followup.md"
    if [ "$P" = "0" ] && [ -f "$FU" ]; then
      FU_P=$(awk '/^- \[ \]/ {c++} END {print c+0}' "$FU" 2>/dev/null)
      [ "$FU_P" -gt 0 ] 2>/dev/null && echo "[$f 본문 완료, followup 잔여 ${FU_P}건] → 분할 plan 권장"
    fi
  done
fi
```
