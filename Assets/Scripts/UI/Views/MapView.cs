using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>지도 칸 참조 (숲 HUD 미니맵, 메뉴 창 지도 탭). 표시 셋은 그림의 자식, 앵커 가운데</summary>
    public sealed class MapView : MonoBehaviour
    {
        /// <summary>안개 지도 그림 (인스펙터에서만 연결)</summary>
        [SerializeField] RawImage _image;

        /// <summary>플레이어 표시 (위가 앞인 화살표. 인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _player;

        /// <summary>현재 목표 표시 (인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _objective;

        /// <summary>숲 입구 표시 (인스펙터에서만 연결)</summary>
        [SerializeField] RectTransform _exit;

        /// <summary>지도가 없을 때 글 (메뉴 지도 탭의 자리 글. 미니맵은 비움)</summary>
        [SerializeField] GameObject _placeholder;

        /// <summary>진행도 글 (Field/field.map_progress, {0} %. 미니맵은 비움)</summary>
        [SerializeField] LocalizeStringEvent _progress;

        /// <summary>안개 지도 그림</summary>
        public RawImage Image => _image;

        /// <summary>플레이어 표시</summary>
        public RectTransform Player => _player;

        /// <summary>현재 목표 표시</summary>
        public RectTransform Objective => _objective;

        /// <summary>숲 입구 표시</summary>
        public RectTransform Exit => _exit;

        /// <summary>지도가 없을 때 글 (없으면 null)</summary>
        public GameObject Placeholder => _placeholder;

        /// <summary>진행도 글 (없으면 null)</summary>
        public LocalizeStringEvent Progress => _progress;
    }
}
