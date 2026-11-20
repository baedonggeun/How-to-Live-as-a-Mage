using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>툴팁 띄우기·숨기기·마우스 따라가기. 기본은 마우스 오른쪽 아래, 화면을 넘으면 마우스 반대쪽으로</summary>
    public sealed class TooltipController : MonoBehaviour
    {
        /// <summary>같은 오브젝트의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] TooltipView _view;

        /// <summary>툴팁이 놓인 캔버스 (UICanvas. 좌표 바꿀 때 씀. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _canvas;

        /// <summary>마우스에서 띄울 거리 (캔버스 단위, x 오른쪽, y 위)</summary>
        [SerializeField] Vector2 _offset = new(16f, -16f);

        #region Show - 제목·설명 키를 넣고 켬 (설명이 비면 제목만). 패널 크기를 바로 맞춰 마우스 옆에 놓음
        /// <param name="title">제목</param>
        /// <param name="body">설명 (없으면 null 또는 빈 값)</param>
        public void Show(LocalizedString title, LocalizedString body)
        {
            if (_view == null || _canvas == null)
            {
                Debug.LogError("[Tooltip] TooltipView·캔버스 연결 안 됨", this);
                return;
            }
            _view.Title.StringReference.SetReference(title.TableReference, title.TableEntryReference);
            // 설명이 있는지
            bool hasBody = body != null && !body.IsEmpty;
            _view.Body.gameObject.SetActive(hasBody);
            if (hasBody) _view.Body.StringReference.SetReference(body.TableReference, body.TableEntryReference);
            gameObject.SetActive(true);
            // 새 글자 크기로 패널 크기를 바로 맞춤 (안 하면 첫 프레임 화면 끝 판단이 옛 크기로 됨)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Panel);
            Follow();
        }
        #endregion

        #region Hide - 끔
        public void Hide() => gameObject.SetActive(false);
        #endregion

        #region LateUpdate - 떠 있는 동안 마우스를 따라감
        void LateUpdate() => Follow();
        #endregion

        #region Follow - 마우스 위치를 캔버스 좌표로 바꿔 패널을 옮김. 오른쪽·아래가 넘치면 마우스 반대쪽으로
        void Follow()
        {
            // 마우스 (없으면 안 따라감)
            var mouse = Mouse.current;
            if (mouse == null || _view == null || _canvas == null) return;
            // 마우스 위치 (캔버스 가운데 기준 좌표)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, mouse.position.ReadValue(), null, out Vector2 local);
            // 패널 크기 (레이아웃이 정한 크기)
            Vector2 size = _view.Panel.rect.size;
            // 캔버스 가운데에서 가장자리까지 거리
            Vector2 half = _canvas.rect.size * 0.5f;
            // 패널 왼쪽 위 x
            float x = local.x + _offset.x;
            if (x + size.x > half.x) x = local.x - _offset.x - size.x;
            // 패널 왼쪽 위 y
            float y = local.y + _offset.y;
            if (y - size.y < -half.y) y = local.y - _offset.y + size.y;
            _view.Panel.anchoredPosition = new Vector2(x, y);
        }
        #endregion
    }
}
