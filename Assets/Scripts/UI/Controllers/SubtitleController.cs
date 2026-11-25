using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>자막: 말한 사람·대사를 게임 시간 4초 보이고(일시정지 중엔 멈춤) 숨긴 뒤 끝나면 할 일을 부름. 새 자막이 오면 앞 자막은 알림 없이 끊음. 언제 띄울지는 UIManager가 정함 (Subtitle 오브젝트에 붙음, 안쪽 Root만 켜고 끔)</summary>
    public sealed class SubtitleController : MonoBehaviour
    {
        /// <summary>자막을 보이는 시간 (초, 게임 시간)</summary>
        const float ShowSeconds = 4f;

        /// <summary>같은 자막의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] SubtitleView _view;

        /// <summary>숨길 때까지 남은 게임 시간 (초). 숨겨져 있으면 0</summary>
        float _left;

        /// <summary>지금 자막이 끝까지 보이고 사라지면 할 일. 없으면 null</summary>
        Action _onHidden;

        #region Show - 말한 사람·대사를 넣고 켬. 앞 자막은 알림 없이 끊음. 빈 자막이거나 연결이 없으면 바로 할 일을 부름
        /// <param name="speaker">말한 사람</param>
        /// <param name="line">대사</param>
        /// <param name="onHidden">자막이 끝까지 보이고 사라진 뒤 할 일 (없으면 null)</param>
        public void Show(LocalizedString speaker, LocalizedString line, Action onHidden)
        {
            if (speaker == null || line == null || speaker.IsEmpty || line.IsEmpty)
            {
                Debug.LogWarning("[Subtitle] 빈 자막 무시");
                onHidden?.Invoke();
                return;
            }
            if (_view == null)
            {
                Debug.LogError("[Subtitle] SubtitleView 연결 안 됨", this);
                onHidden?.Invoke();
                return;
            }
            Hide();
            _view.Speaker.StringReference.SetReference(speaker.TableReference, speaker.TableEntryReference);
            _view.Line.StringReference.SetReference(line.TableReference, line.TableEntryReference);
            _view.Root.SetActive(true);
            _onHidden = onHidden;
            _left = ShowSeconds;
        }
        #endregion

        #region Hide - 자막을 바로 숨김. 끝나면 할 일은 버림 (상태를 나갈 때, 새 자막 전)
        public void Hide()
        {
            _left = 0f;
            _onHidden = null;
            if (_view != null) _view.Root.SetActive(false);
        }
        #endregion

        #region Update - 떠 있는 동안 게임 시간으로 세다 다 되면 숨기고 할 일 실행 (꺼내고 비운 뒤)
        void Update()
        {
            if (_left <= 0f) return;
            _left -= Time.deltaTime;
            if (_left > 0f) return;
            if (_view != null) _view.Root.SetActive(false);
            // 끝나면 할 일 (꺼내고 비운 뒤 실행)
            Action onHidden = _onHidden;
            _onHidden = null;
            onHidden?.Invoke();
        }
        #endregion
    }
}
