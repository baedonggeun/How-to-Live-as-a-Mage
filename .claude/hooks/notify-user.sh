#!/bin/bash
set -e

notify_windows() {
  local MESSAGE="$1"
  powershell.exe -NoProfile -Command "
    try {
      [void][Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]
      \$template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
      \$textNodes = \$template.GetElementsByTagName('text')
      \$textNodes.Item(0).AppendChild(\$template.CreateTextNode('Claude Code')) | Out-Null
      \$textNodes.Item(1).AppendChild(\$template.CreateTextNode('$MESSAGE')) | Out-Null
      \$toast = [Windows.UI.Notifications.ToastNotification]::new(\$template)
      [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Claude Code').Show(\$toast)
    } catch {
      [System.Console]::Beep(800, 300)
    }
  " 2>/dev/null
  local EXIT_CODE=$?
  if [ $EXIT_CODE -ne 0 ]; then
    echo '{"systemMessage": "Windows 알림 전송 실패 (PowerShell exit: '$EXIT_CODE')"}'
  else
    echo '{}'
  fi
}

notify_windows "Claude가 응답을 완료했습니다. 확인이 필요합니다."

exit 0
