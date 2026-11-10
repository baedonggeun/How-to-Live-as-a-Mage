---
name: commit-msg
description: 커밋 메시지 생성. 코드 변경 후 사용.
allowed-tools: Bash(git diff *) Bash(git diff) Bash(git log *) Bash(git status) Bash(git status *) Bash(git add *) Bash(git add) Bash(git commit *) Bash(git commit) Bash(grep *) Bash(sed *) Bash(mv *) Bash(mktemp *) Bash(mktemp) Bash(ls *) Bash(awk *) Bash(basename *)
user-invocable: true
---

<!-- Scope: [portable] 거의 전부. Tier 라벨(T1/T2/T3) + 묶음 토글은 메타 시스템이라 다른 프로젝트로 이주 시에도 그대로 작동. 카피 시 조정 불필요. -->


현재 변경 상태:
```
!`git diff --stat 2>/dev/null`
```

Staged 상태:
```
!`git diff --cached --stat 2>/dev/null`
```

Untracked 파일:
```
!`git ls-files --others --exclude-standard 2>/dev/null`
```

위 변경 내용을 분석해서 커밋 메시지를 생성하고 자동 커밋하라.

## 형식
{Type} — {한줄 요약 40자 이내}

## Type 선택 기준
- Add: 신규 기능/파일 추가
- Fix: 버그 수정
- Refactor: 구조 변경 (기능 동일)
- Update: 기존 기능/문서 개선
- Delete: 제거

## 규칙
- Type은 1개만
- 구분자: ` — ` (em dash, 앞뒤 공백)
- 요약은 한국어, 40자 이내
- 상세 내용은 본문에 bullet (필요 시)
- 본문에 항상 `Why:` 포함 (의사결정 동기 한 줄)
- 대안 검토했으면 `Alternatives:` 포함

## 실행 절차
1. 변경 내용 분석 → 커밋 메시지 생성
2. **묶음 토글 (stateless 추론)** — 메시지에 `묶음 {라벨}` 표기(도메인 라벨: `Logic`/`Prefab`/`Editor`/`Doc`/`Verify` + 1단계 서브태그, 예: `Logic-Migration`)가 있으면 PENDING > 0 + mtime 최신 plan 파일 1개를 자동 식별해 해당 묶음 라인을 `[x]`로 토글. 매핑 의존 없음.
   ```bash
   BUNDLE=$(echo "$MSG" | grep -oE '묶음 [A-Za-z]+(-[A-Za-z]+)?' | head -1)
   if [ -n "$BUNDLE" ]; then
     CANDIDATES=$(ls -t docs/plan-*.md 2>/dev/null \
       | awk '!/-followup\.md$/' \
       | while read f; do
           P=$(awk '/^- \[ \]/ {c++} END {print c+0}' "$f")
           [ "$P" -gt 0 ] && echo "$f"
         done)
     COUNT=$(echo "$CANDIDATES" | grep -c . 2>/dev/null || echo 0)
     if [ "$COUNT" = "1" ]; then
       PLAN="$CANDIDATES"
       TMP=$(mktemp)
       sed "s/^- \[ \] ${BUNDLE} /- [x] ${BUNDLE} /" "$PLAN" > "$TMP" && mv "$TMP" "$PLAN"
       git add "$PLAN"
     elif [ "$COUNT" -gt 1 ]; then
       echo "[묶음 토글 skip] PENDING > 0인 plan ${COUNT}건 — 사용자 수동 Edit 권장"
     fi
   fi
   ```
   - 후보 0건 / 다중 후보 / 메시지에 `묶음 {라벨}` 없음 / 이미 `[x]` → 조용히 스킵
   - 메시지에 묶음 표기가 여러 개여도 첫 번째만 토글 (한 커밋 = 한 묶음 원칙)
3. `git add` — 변경/추가된 코드 파일 staging
4. `git commit -m "{메시지}"` 실행
5. push는 실행하지 않음 (사용자 제어 영역)

## 금지
- Co-Authored-By 줄 추가 금지 (AI 공동저자 표기 불필요)
