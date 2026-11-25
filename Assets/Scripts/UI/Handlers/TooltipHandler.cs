using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>UI 요소에 마우스를 올리면 툴팁 알림, 떼거나 꺼지면 숨김 알림 (UIManager가 받아 띄우고 숨김). 내용은 인스펙터나 Set으로 (SO의 LocalizedString은 읽기만 함)</summary>
    public sealed class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>툴팁 제목 (비면 툴팁 안 띄움)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>툴팁 설명 (비면 제목만)</summary>
        [SerializeField] LocalizedString _body;

        /// <summary>이 요소가 툴팁을 띄운 상태인지 (남이 띄운 툴팁을 끄지 않게)</summary>
        bool _shown;

        /// <summary>툴팁을 띄워 달라는 알림 (제목, 설명. 설명이 비면 제목만). UIManager가 받아 띄움</summary>
        public static event Action<LocalizedString, LocalizedString> ShowRequested;

        /// <summary>툴팁을 숨겨 달라는 알림. UIManager가 받아 숨김</summary>
        public static event Action HideRequested;

        #region Set - 내용 바꾸기 (상태가 데이터를 넣을 때)
        /// <param name="title">제목</param>
        /// <param name="body">설명</param>
        public void Set(LocalizedString title, LocalizedString body)
        {
            _title = title;
            _body = body;
        }
        #endregion

        #region OnPointerEnter - 마우스가 들어오면 툴팁 알림 (제목이 비면 무시)
        /// <param name="eventData">포인터 정보 (안 씀)</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_title == null || _title.IsEmpty) return;
            _shown = true;
            ShowRequested?.Invoke(_title, _body);
        }
        #endregion

        #region OnPointerExit - 마우스가 나가면 숨김
        /// <param name="eventData">포인터 정보 (안 씀)</param>
        public void OnPointerExit(PointerEventData eventData) => HideIfShown();
        #endregion

        #region OnDisable - 꺼지거나 지워지면 숨김 (씬이 바뀌어도 툴팁이 남지 않게)
        void OnDisable() => HideIfShown();
        #endregion

        #region HideIfShown - 이 요소가 띄운 툴팁이면 숨김 알림
        void HideIfShown()
        {
            if (!_shown) return;
            _shown = false;
            HideRequested?.Invoke();
        }
        #endregion
    }
}
