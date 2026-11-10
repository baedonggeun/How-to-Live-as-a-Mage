#!/bin/bash
# check-suffix-hint.sh — .cs Write/Edit 직후 1회성 접미사 책임 hint (suffix-discipline).
#
# 입력: PostToolUse(Write|Edit|MultiEdit) stdin JSON (.tool_input.file_path, .tool_name)
# 동작:
#   - file_path가 .cs일 때만 활성화 (.cs.meta 등 제외). Windows 경로(\)는 /로 정규화
#   - 검사 제외: Assets/Editor/, Assets/Plugins/, Assets/Scripts/Debug/, */Tests/*
#   - 파일에서 public/internal class 이름 추출 → 접미사를 CLAUDE.md Naming Conventions 표와 대조
#   - 위반 시 systemMessage 1줄 hint (자동 실행 X, 차단 X)
# stateless: ledger 없음, escalation 없음. 비차단, exit 0 고정.

INPUT=$(cat 2>/dev/null || echo "{}")
TOOL_NAME=$(echo "$INPUT" | jq -r '.tool_name // ""' 2>/dev/null || echo "")
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // ""' 2>/dev/null | tr '\\' '/')

skip() { echo '{"continue": true}'; exit 0; }

# Write/Edit/MultiEdit 외 도구는 skip
case "$TOOL_NAME" in
  Write|Edit|MultiEdit) ;;
  *) skip ;;
esac

# .cs 확장자만 (.cs.meta 등 제외)
case "$FILE_PATH" in
  *.cs) ;;
  *) skip ;;
esac

# 검사 제외 영역
case "$FILE_PATH" in
  */Assets/Editor/*|*/Assets/Plugins/*|*/Assets/Scripts/Debug/*|*/Tests/*) skip ;;
esac

# 파일 존재 확인
[ -f "$FILE_PATH" ] || skip

# public/internal class 선언에서 클래스명 추출 (sealed/abstract/partial/static 변형 허용)
# - 이름 뒤 제네릭 <T>, 상속 : Base 등은 버림
# - 한 파일 다중 클래스는 모두 검사
DECL='^\s*(public|internal)\s+((sealed|abstract|partial|static)\s+)*class\s+([A-Z][A-Za-z0-9_]+)'
CLASSES=$(grep -E "$DECL" "$FILE_PATH" 2>/dev/null | sed -E "s/${DECL}.*/\\4/" | sort -u)
[ -n "$CLASSES" ] || skip

# 허용 접미사 — CLAUDE.md Naming Conventions 표와 1:1 (표 바꾸면 여기도 같이)
# UI 4축 + 인프라 11종 + 단발 패턴 6종 + C# 관례 Attribute
ALLOWED='(View|UIBinder|Controller|Sequence|Manager|Service|State|SO|Effect|Entry|Pool|Spawner|Handler|Spec|Calculator|Director|Core|Base|Filter|Detector|Router|Attribute)$'

VIOLATIONS=$(printf '%s\n' "$CLASSES" | grep -vE "$ALLOWED" | tr '\n' ' ' | sed 's/ *$//')
[ -n "$VIOLATIONS" ] || skip

MSG="[suffix-hint] 접미사 미정의 — ${VIOLATIONS} : Naming Conventions 표 (CLAUDE.md) 자가 점검 후 명명/책임 재배치"

ESCAPED=$(printf '%s' "$MSG" | jq -Rs .)
cat <<EOF
{
  "systemMessage": ${ESCAPED}
}
EOF
exit 0
