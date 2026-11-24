using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>주문 그리기 패드 (숲 HUD와 책상 코덱스 연습이 같이 씀. 연습이 실전과 같게): F를 꾹(InteractDetector.HoldSeconds) 누르면 시전 상태 → 양피지가 뜨고 누르는 동안 마우스 이동량으로 펜 점을 옮김(커서 잠금, 양피지 안에서만) → 좌클릭을 누른 동안 선을 그음 → F를 떼면(또는 Space) 판정하고 양피지·선이 같이 사라짐. 우클릭은 취소: 그림을 판정해 맞는 마법이면 룬 수 × 1초 동안 갈무리 (아무것도 못 함). 판정은 받은 함수가 하고 결과 글은 여기서 보임. 갈무리·결과 글 시간은 게임 시간 (일시정지 중엔 멈춤)</summary>
    public sealed class SpellDrawController : MonoBehaviour
    {
        /// <summary>시전 상태를 시작하고 판정하는 키 입력 액션 경로 (전역 액션, F)</summary>
        const string HoldActionPath = "Player/Interact";

        /// <summary>선을 그을 입력 액션 경로 (전역 액션, 왼쪽 버튼)</summary>
        const string PenActionPath = "Player/Attack";

        /// <summary>취소 입력 액션 경로 (전역 액션, 오른쪽 버튼)</summary>
        const string CancelActionPath = "Player/CancelCast";

        /// <summary>펜을 움직일 입력 액션 경로 (전역 액션, 마우스 이동량)</summary>
        const string LookActionPath = "Player/Look";

        /// <summary>바로 판정 입력 액션 경로 (전역 액션, Space)</summary>
        const string JudgeActionPath = "Player/Cast";

        /// <summary>이보다 가까운 점은 안 넣음 (칸 단위. 점 수를 줄이려고)</summary>
        const float MinPointGap = 2f;

        /// <summary>펜이 칸 가장자리에서 떨어져 있는 거리 (칸 단위)</summary>
        const float PenMargin = 8f;

        /// <summary>룬 하나당 갈무리 시간 (초). 룬 주문 1초, 2룬 마법 2초 …</summary>
        public const float RecoverSecondsPerRune = 1f;

        /// <summary>시전 결과 키 ({0} 주문 이름, {1} 일치율 %)</summary>
        const string CastKey = "cast.result.cast";

        /// <summary>정확 시전 결과 키 ({0} 주문 이름, {1} 일치율 %)</summary>
        const string CastExactKey = "cast.result.cast_exact";

        /// <summary>취소 결과 키 ({0} 이유)</summary>
        const string CancelKey = "cast.result.cancel";

        /// <summary>실패 결과 키 ({0} 이유)</summary>
        const string FailKey = "cast.result.fail";

        /// <summary>갈무리 중 글 키 ({0} 남은 초)</summary>
        const string RecoverKey = "cast.recover";

        /// <summary>이유: 그린 것이 없음</summary>
        const string WhyEmptyKey = "cast.why.empty";

        /// <summary>이유: 너무 작음</summary>
        const string WhySmallKey = "cast.why.small";

        /// <summary>이유: 비교할 주문 그림이 없음</summary>
        const string WhyNoneKey = "cast.why.none";

        /// <summary>이유: 맞는 주문이 없음</summary>
        const string WhyNoMatchKey = "cast.why.nomatch";

        /// <summary>이유: 부분이 모자람 ({0} 주문, {1} 모자란 부분, {2} 부분 기준 %)</summary>
        const string WhyPartsKey = "cast.why.parts";

        /// <summary>이유: 안 익힌 룬 ({0} 주문, {1} 룬)</summary>
        const string WhyUnlearnedKey = "cast.why.unlearned";

        /// <summary>이유: 하나로 안 정해짐 ({0} 주문 이름 목록)</summary>
        const string WhyAmbiguousKey = "cast.why.ambiguous";

        /// <summary>이유: 직접 취소</summary>
        const string WhyCanceledKey = "cast.why.canceled";

        /// <summary>부분 하나 ({0} 룬, {1} 일치율 %)</summary>
        const string PartKey = "cast.part";

        /// <summary>남는 선 ({0} %)</summary>
        const string ExtraKey = "cast.extra";

        /// <summary>자세히: 부분 일치율 줄 ({0} 부분 목록)</summary>
        const string PartsKey = "cast.parts";

        /// <summary>자세히: 가장 가까운 주문 ({0} 주문, {1} 일치율 %)</summary>
        const string NearestKey = "cast.nearest";

        /// <summary>주문 그리기 문구 테이블</summary>
        static readonly TableReference Table = "Cast";

        /// <summary>같은 칸의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] SpellDrawView _view;

        /// <summary>펜 속도 (마우스 화면 픽셀 1당 펜 화면 픽셀. 숲·책상 같게)</summary>
        [SerializeField, Min(0.1f)] float _penSpeed = 1f;

        /// <summary>그리기 전 안내 글을 보일지 (책상 연습은 켬, 숲은 끔)</summary>
        [SerializeField] bool _showHint = true;

        /// <summary>판정 글을 보이는 시간 (초, 게임 시간). 0이면 다음 시전까지 남김 (책상 연습). 숲은 1.4</summary>
        [SerializeField, Min(0f)] float _resultSeconds;

        /// <summary>끝난 획 (칸 좌표, 점 2개 이상)</summary>
        readonly List<Vector2[]> _strokes = new();

        /// <summary>칸 모서리 월드 좌표 (펜 속도 계산에 다시 씀)</summary>
        readonly Vector3[] _corners = new Vector3[4];

        /// <summary>그리는 중인 획. 없으면 null</summary>
        List<Vector2> _stroke;

        /// <summary>시전 상태인지 (F를 꾹 누른 때부터 판정·취소까지)</summary>
        bool _casting;

        /// <summary>좌클릭을 누르고 있는지 (선을 긋는 중)</summary>
        bool _penDown;

        /// <summary>펜 자리 (칸 가운데 기준, 칸 단위)</summary>
        Vector2 _pen;

        /// <summary>칸 단위 1당 화면 픽셀 (시전을 시작할 때 잼. 책상 월드 캔버스도 마우스 1픽셀 = 펜 1픽셀이 되게)</summary>
        float _pixelsPerUnit = 1f;

        /// <summary>F를 누르고 있는지</summary>
        bool _holdPressed;

        /// <summary>이번 누름이 꾹 누르기 시간을 넘겼는지 (한 번 넘기면 뗄 때까지 다시 시작 안 함)</summary>
        bool _holdFired;

        /// <summary>F를 누른 시각 (실제 시간, 초)</summary>
        float _holdAt;

        /// <summary>갈무리 남은 시간 (초, 게임 시간). 0이면 갈무리 아님</summary>
        float _lockLeft;

        /// <summary>갈무리 글에 마지막으로 넣은 남은 시간 (0.1초 단위, 바뀔 때만 글을 다시 씀)</summary>
        int _lockShown = -1;

        /// <summary>판정 글을 지울 때까지 (초, 게임 시간. 0이면 안 지움)</summary>
        float _resultLeft;

        /// <summary>시전을 시작하기 전 커서 잠금 상태</summary>
        CursorLockMode _prevCursor;

        /// <summary>시전을 시작할 때 보이던 포인터의 화면 위치 (책상). 포인터가 없던 때(숲)는 null</summary>
        Vector2? _pointerBefore;

        /// <summary>커서를 푼 뒤 포인터를 되돌릴 차례인지 (푼 프레임엔 유니티가 포인터를 가운데에 두어서 다음 프레임에 되돌림)</summary>
        bool _restorePending;

        /// <summary>시전 중 이 캔버스 포인터 입력(UI 클릭)을 꺼 뒀는지</summary>
        bool _raycasterOff;

        /// <summary>이 칸이 든 맨 위 캔버스 (펜 속도 계산)</summary>
        Canvas _canvas;

        /// <summary>이 칸이 든 캔버스의 UI 클릭 검출 (시전 중엔 꺼서 커서 자리의 버튼이 눌리지 않게)</summary>
        GraphicRaycaster _raycaster;

        /// <summary>판정의 ShapeIndex 번호 → 결과 글에 쓸 주문 이름 (지금 언어). 그리기를 안 받으면 null</summary>
        Func<int, string> _nameAt;

        /// <summary>판정 (그린 획 → 결과). 그리기를 안 받으면 null</summary>
        Func<IReadOnlyList<Vector2[]>, SpellJudgeSpec> _judge;

        /// <summary>판정·취소 뒤 할 일</summary>
        Action<SpellJudgeSpec> _onJudged;

        /// <summary>시전을 시작해도 되는지 묻는 함수 (조준 중 같은 때 막음). 없으면 null (늘 됨)</summary>
        Func<bool> _canStart;

        /// <summary>시전 시작·판정 키 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _holdAction;

        /// <summary>선 긋기 입력</summary>
        InputAction _penAction;

        /// <summary>취소 입력</summary>
        InputAction _cancelAction;

        /// <summary>펜 이동 입력 (마우스 이동량)</summary>
        InputAction _lookAction;

        /// <summary>바로 판정 입력</summary>
        InputAction _judgeAction;

        /// <summary>시전 상태가 시작됨 (이동·시점을 막을 쪽이 받음)</summary>
        public event Action CastStarted;

        /// <summary>갈무리가 시작됨 (갈무리 초)</summary>
        public event Action<float> Locked;

        /// <summary>갈무리가 끝남</summary>
        public event Action Unlocked;

        /// <summary>그리기를 받는 중인지 (Begin ~ End)</summary>
        public bool IsActive => _judge != null;

        /// <summary>시전 상태인지</summary>
        public bool IsCasting => _casting;

        /// <summary>갈무리 중인지 (아무것도 못 함)</summary>
        public bool IsLocked => _lockLeft > 0f;

        #region Awake - 연결 확인, 입력 액션·캔버스 찾기, 빈 칸으로
        void Awake()
        {
            if (_view == null) Debug.LogError("[Draw] SpellDrawView 연결 안 됨", this);
            _holdAction = FindAction(HoldActionPath);
            _penAction = FindAction(PenActionPath);
            _cancelAction = FindAction(CancelActionPath);
            _lookAction = FindAction(LookActionPath);
            _judgeAction = FindAction(JudgeActionPath);
            Canvas canvas = GetComponentInParent<Canvas>();
            _canvas = canvas != null ? canvas.rootCanvas : null;
            _raycaster = GetComponentInParent<GraphicRaycaster>();
            ResetDrawing();
        }
        #endregion

        #region FindAction - 전역 입력 액션 찾기. 없으면 에러 로그 후 null
        /// <param name="path">액션 경로 (묶음/이름)</param>
        static InputAction FindAction(string path)
        {
            // 찾은 액션 (없으면 null)
            var action = InputSystem.actions.FindAction(path);
            if (action == null) Debug.LogError($"[Draw] {path} 액션 없음");
            return action;
        }
        #endregion

        #region OnDisable - 꺼지면 그리기를 끝냄 (입력 구독 해제)
        void OnDisable() => End();
        #endregion

        #region Begin - 그리기 받기 시작: 판정·결과 할 일과 결과 글에 쓸 주문 이름을 받고 입력을 구독, 획·결과를 비움 (다시 부르면 처음부터)
        /// <param name="nameAt">판정 결과의 ShapeIndex 번호 → 주문 이름 (지금 언어)</param>
        /// <param name="judge">그린 획 → 판정</param>
        /// <param name="onJudged">판정·취소 뒤 할 일 (없으면 null)</param>
        /// <param name="canStart">시전을 시작해도 되는지 묻는 함수 (없으면 null: 늘 됨)</param>
        public void Begin(Func<int, string> nameAt, Func<IReadOnlyList<Vector2[]>, SpellJudgeSpec> judge, Action<SpellJudgeSpec> onJudged, Func<bool> canStart = null)
        {
            _nameAt = nameAt ?? throw new ArgumentNullException(nameof(nameAt));
            _judge = judge ?? throw new ArgumentNullException(nameof(judge));
            _onJudged = onJudged;
            _canStart = canStart;
            Subscribe(false);
            Subscribe(true);
            ResetDrawing();
        }
        #endregion

        #region End - 그리기 끝: 입력 구독 해제, 시전·갈무리를 조용히 끝내고 포인터 되돌리기 대기·할 일·획·결과·따라 그릴 그림을 비움 (여러 번 불려도 됨, Awake 전에도 됨)
        public void End()
        {
            Subscribe(false);
            _holdPressed = false;
            _holdFired = false;
            if (_casting) EndCasting();
            _restorePending = false;
            _lockLeft = 0f;
            _judge = null;
            _onJudged = null;
            _nameAt = null;
            _canStart = null;
            ResetDrawing();
            if (_view != null && _view.Guide != null) _view.Guide.Clear();
        }
        #endregion

        #region Subscribe - 입력 구독·해제 (시전 시작·판정 키, 선 긋기, 취소, 바로 판정)
        /// <param name="on">구독할지 (false면 해제)</param>
        void Subscribe(bool on)
        {
            if (_holdAction != null)
            {
                if (on)
                {
                    _holdAction.performed += OnHoldPressed;
                    _holdAction.canceled += OnHoldReleased;
                }
                else
                {
                    _holdAction.performed -= OnHoldPressed;
                    _holdAction.canceled -= OnHoldReleased;
                }
            }
            if (_penAction != null)
            {
                if (on)
                {
                    _penAction.performed += OnPenPressed;
                    _penAction.canceled += OnPenReleased;
                }
                else
                {
                    _penAction.performed -= OnPenPressed;
                    _penAction.canceled -= OnPenReleased;
                }
            }
            if (_cancelAction != null)
            {
                if (on) _cancelAction.performed += OnCancelPressed;
                else _cancelAction.performed -= OnCancelPressed;
            }
            if (_judgeAction != null)
            {
                if (on) _judgeAction.performed += OnJudgePressed;
                else _judgeAction.performed -= OnJudgePressed;
            }
        }
        #endregion

        #region ShowGuide - 따라 그릴 그림을 흐리게 깔고 안내 글을 숨김. null이면 그림을 지우고 안내 글을 보임 (자유 그리기, 그리기 전일 때)
        /// <param name="shape">따라 그릴 그림 (자유 그리기면 null)</param>
        public void ShowGuide(SpellShapeEntry shape)
        {
            if (_view == null) return;
            if (shape == null) _view.Guide.Clear();
            else _view.Guide.SetStrokes(shape.PartStrokes.SelectMany(part => part), true);
            if (!_casting) _view.Hint.gameObject.SetActive(_showHint && shape == null);
        }
        #endregion

        #region CancelCast - 직접 취소 (Esc·창이 떠서 입력이 끊김): 판정·갈무리 없이 시전 상태만 끝냄
        public void CancelCast()
        {
            if (!_casting) return;
            EndCasting();
            SpellJudgeSpec canceled = SpellJudgeSpec.Cancel(SpellJudgeReason.Canceled);
            ShowResult(canceled);
            _resultLeft = _resultSeconds;
            _onJudged?.Invoke(canceled);
        }
        #endregion

        #region JudgeNow - 그리던 획을 끝내고 바로 판정: 양피지·선이 같이 사라지고 결과 글을 보인 뒤 할 일 (시전 상태가 아니면 무시)
        public void JudgeNow()
        {
            if (!_casting || _judge == null) return;
            EndStroke();
            // 그린 획 복사본으로 한 판정
            SpellJudgeSpec spec = _judge(_strokes.ToArray());
            EndCasting();
            ShowResult(spec);
            _resultLeft = _resultSeconds;
            _onJudged?.Invoke(spec);
        }
        #endregion

        #region Recover - 갈무리: 룬 수 × 1초 동안 아무것도 못 함 (취소한 마법의 룬 수. 0 이하면 무시)
        /// <param name="runeCount">취소한 마법의 룬 수</param>
        public void Recover(int runeCount) => Lock(runeCount * RecoverSecondsPerRune);
        #endregion

        #region Lock - 갈무리 시작: 새 시전을 막고 "갈무리 중 · N초"를 보임, 끝날 때까지 시작·끝을 알림
        /// <param name="seconds">갈무리 초 (0 이하면 무시)</param>
        public void Lock(float seconds)
        {
            if (seconds <= 0f) return;
            _lockLeft = seconds;
            _lockShown = -1;
            _resultLeft = 0f;
            ShowLockText();
            Locked?.Invoke(seconds);
        }
        #endregion

        #region Update - 포인터 되돌리기 확인. 그리기를 받는 중일 때만: 꾹 누르기 확인, 시전 중이면 펜 이동, 갈무리·결과 글 시간 (게임 시간)
        void Update()
        {
            UpdateRestore();
            if (!IsActive) return;
            UpdateHold();
            if (_casting) UpdatePen();
            // 이번 프레임 게임 시간 (초). 일시정지 중엔 0
            float dt = Time.deltaTime;
            UpdateLock(dt);
            UpdateResult(dt);
        }
        #endregion

        #region UpdateRestore - 커서를 푼 다음 프레임에 보이던 포인터를 시전 전 자리로 되돌림 (그 사이 다시 잠겼으면 안 함)
        void UpdateRestore()
        {
            if (!_restorePending) return;
            _restorePending = false;
            if (_pointerBefore.HasValue && Mouse.current != null && Cursor.lockState != CursorLockMode.Locked)
                Mouse.current.WarpCursorPosition(_pointerBefore.Value);
        }
        #endregion

        #region UpdateHold - F를 누른 지 꾹 누르기 시간이 지났으면 (한 번만) 시전 상태 시작. 갈무리 중·시작을 막은 때·이미 시전 중이면 안 함
        void UpdateHold()
        {
            if (!_holdPressed || _holdFired) return;
            if (Time.unscaledTime - _holdAt < InteractDetector.HoldSeconds) return;
            _holdFired = true;
            if (_casting || IsLocked) return;
            if (_canStart != null && !_canStart()) return;
            StartCast();
        }
        #endregion

        #region UpdatePen - 마우스 이동량으로 펜을 옮기고 칸(양피지) 안으로 자름. 선을 긋는 중이면 점을 더함
        void UpdatePen()
        {
            if (_lookAction == null) return;
            // 이번 프레임 펜 이동 (칸 단위)
            Vector2 delta = _lookAction.ReadValue<Vector2>() * (_penSpeed / _pixelsPerUnit);
            if (delta == Vector2.zero) return;
            // 칸 크기
            Rect area = ((RectTransform)transform).rect;
            _pen = new Vector2(
                Mathf.Clamp(_pen.x + delta.x, area.xMin + PenMargin, area.xMax - PenMargin),
                Mathf.Clamp(_pen.y + delta.y, area.yMin + PenMargin, area.yMax - PenMargin));
            if (_view != null && _view.Pen != null) _view.Pen.anchoredPosition = _pen;
            if (_penDown) PenMove(_pen);
        }
        #endregion

        #region UpdateLock - 갈무리 시간을 줄이고 글을 갱신. 끝나면 글을 지우고 알림
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateLock(float dt)
        {
            if (_lockLeft <= 0f) return;
            _lockLeft -= dt;
            if (_lockLeft <= 0f)
            {
                _lockLeft = 0f;
                HideTexts();
                Unlocked?.Invoke();
                return;
            }
            ShowLockText();
        }
        #endregion

        #region UpdateResult - 판정 글을 보인 지 정한 시간이 지났으면 지움 (정한 시간이 0이면 안 지움)
        /// <param name="dt">프레임 시간 (초)</param>
        void UpdateResult(float dt)
        {
            if (_resultLeft <= 0f) return;
            _resultLeft -= dt;
            if (_resultLeft <= 0f)
            {
                _resultLeft = 0f;
                HideTexts();
            }
        }
        #endregion

        #region OnHoldPressed - F를 누름: 누른 시각을 적음 (시작은 UpdateHold가 꾹 누르기 시간이 지났을 때)
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnHoldPressed(InputAction.CallbackContext context)
        {
            _holdPressed = true;
            _holdFired = false;
            _holdAt = Time.unscaledTime;
        }
        #endregion

        #region OnHoldReleased - F를 뗌: 시전 중이면 판정. 창·메뉴가 떠서 입력이 꺼지며 온 떼기면 판정 없이 취소. 시전 중이 아니면(단발) 아무것도 안 함
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnHoldReleased(InputAction.CallbackContext context)
        {
            _holdPressed = false;
            if (!_casting) return;
            if (Time.timeScale <= 0f)
            {
                CancelCast();
                return;
            }
            JudgeNow();
        }
        #endregion

        #region OnPenPressed - 좌클릭: 시전 중이면 펜 자리에서 새 획 시작
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnPenPressed(InputAction.CallbackContext context)
        {
            if (!_casting) return;
            _penDown = true;
            BeginStroke(_pen);
        }
        #endregion

        #region OnPenReleased - 좌클릭을 뗌: 그리던 획 끝
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnPenReleased(InputAction.CallbackContext context)
        {
            if (!_casting || !_penDown) return;
            _penDown = false;
            EndStroke();
        }
        #endregion

        #region OnCancelPressed - 우클릭: 시전 중이면 취소 (그림을 판정해 갈무리)
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (_casting) CancelRecover();
        }
        #endregion

        #region OnJudgePressed - Space: 시전 중이면 바로 판정
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnJudgePressed(InputAction.CallbackContext context)
        {
            if (_casting) JudgeNow();
        }
        #endregion

        #region StartCast - 시전 상태 시작: 앞 결과·그림을 지우고 양피지·펜 점을 켬(펜은 보이던 포인터 자리, 없으면 가운데), 커서를 잠그고 이동·시점 막을 쪽에 알림
        void StartCast()
        {
            _casting = true;
            _penDown = false;
            _stroke = null;
            _strokes.Clear();
            _resultLeft = 0f;
            _pixelsPerUnit = PixelsPerUnit();
            _pen = PenStart();
            if (_view != null)
            {
                _view.Ink.Clear();
                _view.Result.gameObject.SetActive(false);
                _view.Detail.gameObject.SetActive(false);
                _view.Hint.gameObject.SetActive(false);
                if (_view.Board != null) _view.Board.SetActive(true);
                if (_view.Pen != null)
                {
                    _view.Pen.anchoredPosition = _pen;
                    _view.Pen.gameObject.SetActive(true);
                }
            }
            LockCursor();
            CastStarted?.Invoke();
        }
        #endregion

        #region EndCasting - 시전 상태 끝: 양피지·선·펜 점을 같이 지우고 커서·UI 클릭을 되돌림 (판정·취소 결과는 부른 쪽이 보임)
        void EndCasting()
        {
            _casting = false;
            _penDown = false;
            _stroke = null;
            _strokes.Clear();
            if (_view != null)
            {
                if (_view.Ink != null) _view.Ink.Clear();
                if (_view.Board != null) _view.Board.SetActive(false);
                if (_view.Pen != null) _view.Pen.gameObject.SetActive(false);
            }
            UnlockCursor();
        }
        #endregion

        #region CancelRecover - 우클릭 취소: 그린 그림을 판정해, 맞는 마법이면 그 마법의 룬 수 × 1초 갈무리 (마법이 아니면 갈무리 없이 취소)
        void CancelRecover()
        {
            if (!_casting || _judge == null) return;
            EndStroke();
            // 취소 순간 그림의 판정
            SpellJudgeSpec drawn = _judge(_strokes.ToArray());
            // 맞는 마법이면 그 마법의 룬 수 (아니면 0)
            int runes = drawn.IsCast && drawn.Candidate != null ? drawn.Candidate.Shape.PartRuneIds.Count : 0;
            EndCasting();
            SpellJudgeSpec canceled = SpellJudgeSpec.Cancel(SpellJudgeReason.Canceled);
            if (runes <= 0)
            {
                ShowResult(canceled);
                _resultLeft = _resultSeconds;
            }
            _onJudged?.Invoke(canceled);
            Recover(runes);
        }
        #endregion

        #region PenStart - 펜 시작 자리: 시전 전에 포인터가 보였으면(책상) 그 화면 위치를 칸 좌표로 바꿔 칸 안으로 맞추고 되돌릴 위치도 적어 둠. 포인터가 없었거나(숲) 못 바꾸면 칸 가운데
        Vector2 PenStart()
        {
            _pointerBefore = null;
            if (Cursor.lockState == CursorLockMode.Locked || Mouse.current == null) return Vector2.zero;
            // 포인터 화면 위치
            Vector2 screen = Mouse.current.position.ReadValue();
            _pointerBefore = screen;
            // 이 칸
            var rect = (RectTransform)transform;
            // 월드 캔버스면 그 카메라 (오버레이면 없음)
            Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, cam, out Vector2 local)) return Vector2.zero;
            // 칸 크기
            Rect area = rect.rect;
            return new Vector2(
                Mathf.Clamp(local.x, area.xMin + PenMargin, area.xMax - PenMargin),
                Mathf.Clamp(local.y, area.yMin + PenMargin, area.yMax - PenMargin));
        }
        #endregion

        #region LockCursor - 커서를 잠그고(펜은 마우스 이동량으로 움직임) 이 캔버스의 UI 클릭을 꺼 둠 (책상: 커서 자리의 버튼이 눌리지 않게). 되돌리기 대기 중이었으면 취소
        void LockCursor()
        {
            _restorePending = false;
            _prevCursor = Cursor.lockState;
            Cursor.lockState = CursorLockMode.Locked;
            if (_raycaster != null && _raycaster.enabled)
            {
                _raycaster.enabled = false;
                _raycasterOff = true;
            }
        }
        #endregion

        #region UnlockCursor - 커서·UI 클릭을 되돌림. 커서는 우리가 잠근 그대로일 때만 (그 사이 일시정지가 풀었으면 건드리지 않음). 시전 전에 포인터가 보였으면 되돌릴 차례를 받음
        void UnlockCursor()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = _prevCursor;
                _restorePending = _pointerBefore.HasValue;
            }
            if (_raycasterOff && _raycaster != null) _raycaster.enabled = true;
            _raycasterOff = false;
        }
        #endregion

        #region PixelsPerUnit - 이 칸의 화면 픽셀 폭 / 칸 단위 폭 (오버레이 캔버스면 배율, 월드 캔버스면 카메라에 비친 크기)
        float PixelsPerUnit()
        {
            // 이 칸
            var rect = (RectTransform)transform;
            if (_canvas == null || rect.rect.width <= 0f) return 1f;
            // 월드 좌표를 화면에 옮길 카메라 (오버레이면 없음)
            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            rect.GetWorldCorners(_corners);
            // 왼쪽 아래~오른쪽 아래 화면 픽셀 거리
            float pixels = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(cam, _corners[0]), RectTransformUtility.WorldToScreenPoint(cam, _corners[3]));
            return pixels > 0.01f ? pixels / rect.rect.width : 1f;
        }
        #endregion

        #region BeginStroke - 새 획 시작 (앞 획은 끝냄)
        /// <param name="point">칸 좌표 점</param>
        void BeginStroke(Vector2 point)
        {
            EndStroke();
            _stroke = new List<Vector2> { point };
            RefreshInk();
        }
        #endregion

        #region PenMove - 그리는 중이면 점을 더함 (앞 점과 너무 가까우면 뺌)
        /// <param name="point">칸 좌표 점</param>
        void PenMove(Vector2 point)
        {
            if (_stroke == null) return;
            if ((point - _stroke[_stroke.Count - 1]).sqrMagnitude < MinPointGap * MinPointGap) return;
            _stroke.Add(point);
            RefreshInk();
        }
        #endregion

        #region EndStroke - 그리던 획을 끝냄 (점 2개 이상이면 남김)
        void EndStroke()
        {
            if (_stroke == null) return;
            if (_stroke.Count >= 2) _strokes.Add(_stroke.ToArray());
            _stroke = null;
            RefreshInk();
        }
        #endregion

        #region ResetDrawing - 시전 상태·획·결과 글을 비우고 안내 글을 보임
        void ResetDrawing()
        {
            if (_casting) EndCasting();
            _penDown = false;
            _stroke = null;
            _strokes.Clear();
            _resultLeft = 0f;
            _lockShown = -1;
            if (_view == null) return;
            if (_view.Ink != null) _view.Ink.Clear();
            if (_view.Board != null) _view.Board.SetActive(false);
            if (_view.Pen != null) _view.Pen.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Detail.gameObject.SetActive(false);
            _view.Hint.gameObject.SetActive(_showHint);
        }
        #endregion

        #region HideTexts - 결과·자세히 글을 지우고, 시전 중이 아니면 안내 글을 다시 보임
        void HideTexts()
        {
            if (_view == null) return;
            _view.Result.gameObject.SetActive(false);
            _view.Detail.gameObject.SetActive(false);
            if (!_casting) _view.Hint.gameObject.SetActive(_showHint);
        }
        #endregion

        #region RefreshInk - 끝난 획과 그리는 중인 획을 다시 그림
        void RefreshInk()
        {
            if (_view == null) return;
            // 그릴 획 전부
            var all = new List<IReadOnlyList<Vector2>>(_strokes);
            if (_stroke != null) all.Add(_stroke);
            _view.Ink.SetStrokes(all, false);
        }
        #endregion

        #region ShowLockText - "갈무리 중 · N초" (0.1초가 바뀔 때만 다시 씀)
        void ShowLockText()
        {
            if (_view == null) return;
            // 보일 남은 시간 (0.1초 단위)
            int tenths = Mathf.CeilToInt(_lockLeft * 10f);
            if (tenths == _lockShown) return;
            _lockShown = tenths;
            _view.Hint.gameObject.SetActive(false);
            _view.Detail.gameObject.SetActive(false);
            SetText(_view.Result, RecoverKey, (tenths / 10f).ToString("F1", CultureInfo.InvariantCulture));
        }
        #endregion

        #region ShowResult - 결과 줄 (시전: "이름 일치율%", 정확이면 표시 / 실패: "실패: 이유" / 취소: "취소: 이유")과 자세히 (맞는 주문이 없으면 가장 가까운 주문, 아니면 부분 일치율·남는 선)
        /// <param name="spec">판정 결과</param>
        void ShowResult(SpellJudgeSpec spec)
        {
            if (_view == null) return;
            _view.Hint.gameObject.SetActive(false);
            if (spec.IsCast)
                SetText(_view.Result, spec.Accuracy >= SpellRecognitionCalculator.ExactAccuracy ? CastExactKey : CastKey, SpellName(spec.Candidate), Percent(spec.Accuracy));
            else
                SetText(_view.Result, spec.IsFailure ? FailKey : CancelKey, ReasonText(spec));
            if (spec.Reason == SpellJudgeReason.NoMatch && spec.Candidate != null)
                SetText(_view.Detail, NearestKey, SpellName(spec.Candidate), Percent(spec.Accuracy));
            else if (spec.Candidate != null && spec.PartCoverage.Count > 0)
                SetText(_view.Detail, PartsKey, PartsText(spec, false));
            else
                _view.Detail.gameObject.SetActive(false);
        }
        #endregion

        #region SetText - 글자에 이 테이블 키·자리 값을 넣고 켬
        /// <param name="text">넣을 글자</param>
        /// <param name="key">문구 키</param>
        /// <param name="args">자리 값</param>
        static void SetText(LocalizeStringEvent text, string key, params object[] args)
        {
            // 글자의 String Reference
            var reference = text.StringReference;
            reference.SetReference(Table, key);
            reference.Arguments = args;
            text.gameObject.SetActive(true);
            text.RefreshString();
        }
        #endregion

        #region ReasonText - 취소·실패 이유 글 (부분 부족: 모자란 부분, 미습득: 빠진 룬, 애매함: 겹친 주문 이름)
        /// <param name="spec">판정 결과</param>
        string ReasonText(SpellJudgeSpec spec) => spec.Reason switch
        {
            SpellJudgeReason.Parts => Text(WhyPartsKey, SpellName(spec.Candidate), PartsText(spec, true), Percent(spec.PartMin)),
            SpellJudgeReason.Unlearned => Text(WhyUnlearnedKey, SpellName(spec.Candidate), UIManager.Instance.JoinList(spec.MissingRuneIds.Select(RuneName))),
            SpellJudgeReason.Ambiguous => Text(WhyAmbiguousKey, UIManager.Instance.JoinList(spec.AmbiguousShapes.Select(SpellNameAt))),
            SpellJudgeReason.Empty => Text(WhyEmptyKey),
            SpellJudgeReason.Small => Text(WhySmallKey),
            SpellJudgeReason.NoCandidates => Text(WhyNoneKey),
            SpellJudgeReason.Canceled => Text(WhyCanceledKey),
            _ => Text(WhyNoMatchKey),
        };
        #endregion

        #region PartsText - 부분 일치율 목록 ("불 96%, …". lowOnly면 부분 기준 아래만) + 남는 선 (lowOnly면 한도를 넘을 때만)
        /// <param name="spec">판정 결과 (Candidate 있음)</param>
        /// <param name="lowOnly">모자란 것만 넣을지</param>
        string PartsText(SpellJudgeSpec spec, bool lowOnly)
        {
            // 넣을 항목 글
            var items = new List<string>();
            for (int i = 0; i < spec.PartCoverage.Count && i < spec.Candidate.PartRuneIds.Count; i++)
            {
                // 이 부분 일치율
                float coverage = spec.PartCoverage[i];
                if (lowOnly && coverage >= spec.PartMin) continue;
                items.Add(Text(PartKey, RuneName(spec.Candidate.PartRuneIds[i]), Percent(coverage)));
            }
            if (!lowOnly || spec.Extra > SpellRecognitionCalculator.MaxExtra) items.Add(Text(ExtraKey, Percent(spec.Extra)));
            return UIManager.Instance.JoinList(items);
        }
        #endregion

        #region Text - 이 테이블 문구를 지금 언어로 읽음
        /// <param name="key">문구 키</param>
        /// <param name="args">자리 값</param>
        static string Text(string key, params object[] args) => new LocalizedString(Table, key).GetLocalizedString(args);
        #endregion

        #region SpellName - 비교 그림의 주문 이름 (지금 언어, 없으면 빈 글)
        /// <param name="candidate">비교 그림</param>
        string SpellName(SpellCandidateEntry candidate) => candidate != null ? SpellNameAt(candidate.ShapeIndex) : string.Empty;
        #endregion

        #region SpellNameAt - 주문 그림 번호의 주문 이름 (지금 언어, 없으면 빈 글)
        /// <param name="index">주문 그림 번호 (판정의 ShapeIndex)</param>
        string SpellNameAt(int index) => _nameAt != null ? _nameAt(index) ?? string.Empty : string.Empty;
        #endregion

        #region RuneName - 룬 id의 이름 (지금 언어, 모르는 id면 id 그대로)
        /// <param name="id">룬 id</param>
        static string RuneName(string id)
        {
            // 찾은 룬
            RuneSO rune = DataManager.Instance.FindRune(id);
            return rune != null ? rune.Name.GetLocalizedString() : id;
        }
        #endregion

        #region Percent - 0~1을 % 정수로
        /// <param name="value">0~1 값</param>
        static int Percent(float value) => Mathf.RoundToInt(value * 100f);
        #endregion
    }
}
