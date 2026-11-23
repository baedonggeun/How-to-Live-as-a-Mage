using System;
using UnityEngine;

namespace Mage
{
    /// <summary>지도 칸 ← 안개 지도·플레이어·현재 목표·숲 입구 (출처는 상태가 Bind로 넘김). 밝힌 칸이 바뀌면 그림을 다시 칠하고 매 프레임 표시를 옮김. 보는 범위가 0이면 지역 전체(메뉴 지도 탭), 아니면 플레이어를 가운데 둔 그 범위(미니맵). 출처가 없으면 자리 글만</summary>
    public sealed class MapUIBinder : MonoBehaviour
    {
        /// <summary>안 밝힌 칸 색</summary>
        static readonly Color32 HiddenColor = new(18, 20, 24, 230);

        /// <summary>밝힌 칸 색</summary>
        static readonly Color32 ShownColor = new(112, 128, 96, 255);

        /// <summary>같은 칸의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] MapView _view;

        /// <summary>보는 범위 한 변 (m). 0이면 지역 전체</summary>
        [SerializeField, Min(0f)] float _windowMeters;

        /// <summary>안개 지도 (없으면 자리 글만)</summary>
        FogMapService _fog;

        /// <summary>플레이어</summary>
        Transform _player;

        /// <summary>현재 목표 위치 (없으면 null을 돌려줌)</summary>
        Func<Vector3?> _objective;

        /// <summary>숲 입구 위치</summary>
        Vector3 _exit;

        /// <summary>안개 지도 그림 (칸 하나 = 픽셀 하나, 아래 줄부터)</summary>
        Texture2D _texture;

        /// <summary>그림 픽셀 (다시 칠할 때 씀)</summary>
        Color32[] _pixels;

        /// <summary>마지막으로 칠한 안개 지도 Version</summary>
        int _drawnVersion = -1;

        /// <summary>마지막으로 보인 진행도 %</summary>
        int _shownPercent = -1;

        #region Awake - 연결 확인, 출처가 없을 때 모습 (자리 글). 꺼진 채 시작하는 메뉴 창 지도는 첫 열 때 Awake가 돌아 이미 Bind된 뒤일 수 있어서, 출처가 있으면 그대로 둠
        void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[Map] MapView 연결 안 됨", this);
                return;
            }
            if (_fog == null) ShowBound(false);
        }
        #endregion

        #region Bind - 안개 지도·플레이어·목표·입구를 받고 그림을 (크기가 다르면 새로) 만들어 지도를 보임
        /// <param name="fog">안개 지도</param>
        /// <param name="player">플레이어</param>
        /// <param name="objective">현재 목표 위치 (없으면 null을 돌려주는 함수, 함수가 null이면 표시 안 함)</param>
        /// <param name="exit">숲 입구 위치</param>
        public void Bind(FogMapService fog, Transform player, Func<Vector3?> objective, Vector3 exit)
        {
            _fog = fog ?? throw new ArgumentNullException(nameof(fog));
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _objective = objective;
            _exit = exit;
            if (_texture == null || _texture.width != fog.Width || _texture.height != fog.Height)
            {
                if (_texture != null) Destroy(_texture);
                _texture = new Texture2D(fog.Width, fog.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _pixels = new Color32[fog.Width * fog.Height];
            }
            _drawnVersion = -1;
            _shownPercent = -1;
            if (_view == null) return;
            _view.Image.texture = _texture;
            ShowBound(true);
        }
        #endregion

        #region Unbind - 출처를 놓고 자리 글로
        public void Unbind()
        {
            _fog = null;
            _player = null;
            _objective = null;
            ShowBound(false);
        }
        #endregion

        #region ShowBound - 지도(그림·진행도)와 자리 글 중 하나를 보임
        /// <param name="bound">출처가 있는지</param>
        void ShowBound(bool bound)
        {
            if (_view == null) return;
            _view.Image.gameObject.SetActive(bound);
            if (_view.Placeholder != null) _view.Placeholder.SetActive(!bound);
            if (_view.Progress != null) _view.Progress.gameObject.SetActive(bound);
        }
        #endregion

        #region LateUpdate - 출처가 있으면 밝힌 칸이 바뀌었을 때 다시 칠하고, 표시를 옮기고, 진행도 글을 갱신
        void LateUpdate()
        {
            if (_fog == null || _player == null || _view == null) return;
            if (_fog.Version != _drawnVersion) Redraw();
            PlaceMarkers();
            RefreshProgress();
        }
        #endregion

        #region Redraw - 칸마다 밝혔으면 땅색, 아니면 어두운 색으로 칠함
        void Redraw()
        {
            for (int z = 0; z < _fog.Height; z++)
                for (int x = 0; x < _fog.Width; x++)
                    _pixels[z * _fog.Width + x] = _fog.IsRevealed(x, z) ? ShownColor : HiddenColor;
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            _drawnVersion = _fog.Version;
        }
        #endregion

        #region PlaceMarkers - 보는 범위(전체 또는 플레이어 가운데)를 그림 uv로 정하고 플레이어(방향 포함)·목표·입구 표시를 옮김. 범위 밖 표시는 끔
        void PlaceMarkers()
        {
            // 플레이어 위치 (지역 비율)
            Vector2 player = _fog.ToNormalized(_player.position);
            // 보는 범위 크기 (지역 비율)
            Vector2 window = _windowMeters > 0f ? new Vector2(_windowMeters / _fog.Area.width, _windowMeters / _fog.Area.height) : Vector2.one;
            // 보는 범위 왼쪽 아래 (지역 비율)
            Vector2 origin = _windowMeters > 0f ? player - window * 0.5f : Vector2.zero;
            _view.Image.uvRect = new Rect(origin, window);
            Place(_view.Player, player, origin, window);
            if (_view.Player != null) _view.Player.localRotation = Quaternion.Euler(0f, 0f, -_player.eulerAngles.y);
            PlaceOptional(_view.Objective, _objective?.Invoke(), origin, window);
            PlaceOptional(_view.Exit, _exit, origin, window);
        }
        #endregion

        #region Place - 지역 비율 위치를 그림 안 자리로 옮김 (범위 밖이면 끔)
        /// <param name="marker">표시</param>
        /// <param name="normalized">지역 비율 위치</param>
        /// <param name="origin">보는 범위 왼쪽 아래</param>
        /// <param name="window">보는 범위 크기</param>
        void Place(RectTransform marker, Vector2 normalized, Vector2 origin, Vector2 window)
        {
            if (marker == null) return;
            // 보는 범위 안 비율 (0~1)
            Vector2 local = (normalized - origin) / window;
            // 범위 안인지
            bool inside = local.x >= 0f && local.x <= 1f && local.y >= 0f && local.y <= 1f;
            marker.gameObject.SetActive(inside);
            if (!inside) return;
            // 그림 칸 크기
            Rect rect = _view.Image.rectTransform.rect;
            marker.anchoredPosition = new Vector2((local.x - 0.5f) * rect.width, (local.y - 0.5f) * rect.height);
        }
        #endregion

        #region PlaceOptional - 위치가 있으면 옮기고 없으면 끔
        /// <param name="marker">표시</param>
        /// <param name="world">월드 위치 (없으면 null)</param>
        /// <param name="origin">보는 범위 왼쪽 아래</param>
        /// <param name="window">보는 범위 크기</param>
        void PlaceOptional(RectTransform marker, Vector3? world, Vector2 origin, Vector2 window)
        {
            if (marker == null) return;
            if (world == null)
            {
                marker.gameObject.SetActive(false);
                return;
            }
            Place(marker, _fog.ToNormalized(world.Value), origin, window);
        }
        #endregion

        #region RefreshProgress - 진행도 %가 바뀌었을 때만 글을 다시 씀 (글이 없으면 안 함)
        void RefreshProgress()
        {
            if (_view.Progress == null) return;
            // 지금 진행도 %
            int percent = Mathf.RoundToInt(_fog.Progress * 100f);
            if (percent == _shownPercent) return;
            _shownPercent = percent;
            _view.Progress.StringReference.Arguments = new object[] { percent };
            _view.Progress.RefreshString();
        }
        #endregion

        #region OnDestroy - 만든 그림을 지움
        void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
        }
        #endregion
    }
}
