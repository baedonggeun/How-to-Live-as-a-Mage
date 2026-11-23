using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>주문 그리기 칸: 좌클릭으로 시전 상태, 버튼을 떼도 이어 그리기, 마지막 입력 뒤 대기 시간이 지나거나 Space(Player/Cast)면 판정, 우클릭은 취소. 그린 획을 칸 좌표로 모으고, 판정은 받은 함수가 하고 결과 글은 여기서 보임. 대기 시간은 게임 시간 (일시정지 중엔 멈춤)</summary>
    public sealed class SpellDrawController : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>바로 판정 입력 액션 경로 (전역 액션, Space)</summary>
        const string CastActionPath = "Player/Cast";

        /// <summary>이보다 가까운 점은 안 넣음 (칸 단위. 점 수를 줄이려고)</summary>
        const float MinPointGap = 2f;

        /// <summary>시전 결과 키 ({0} 주문 이름, {1} 일치율 %)</summary>
        const string CastKey = "cast.result.cast";

        /// <summary>정확 시전 결과 키 ({0} 주문 이름, {1} 일치율 %)</summary>
        const string CastExactKey = "cast.result.cast_exact";

        /// <summary>취소 결과 키 ({0} 이유)</summary>
        const string CancelKey = "cast.result.cancel";

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

        /// <summary>마지막 입력 뒤 판정까지 시간 (초, 게임 시간. 임시 3초)</summary>
        [SerializeField, Min(0.5f)] float _waitSeconds = 3f;

        /// <summary>끝난 획 (칸 좌표, 점 2개 이상)</summary>
        readonly List<Vector2[]> _strokes = new();

        /// <summary>그리는 중인 획. 없으면 null</summary>
        List<Vector2> _stroke;

        /// <summary>시전 상태인지 (첫 좌클릭부터 판정·취소까지)</summary>
        bool _casting;

        /// <summary>마지막 입력 뒤 지난 시간 (초)</summary>
        float _idle;

        /// <summary>판정의 ShapeIndex 번호 → 결과 글에 쓸 주문 이름 (지금 언어). 그리기를 안 받으면 null</summary>
        Func<int, string> _nameAt;

        /// <summary>판정 (그린 획 → 결과). 그리기를 안 받으면 null</summary>
        Func<IReadOnlyList<Vector2[]>, SpellJudgeSpec> _judge;

        /// <summary>판정·취소 뒤 할 일</summary>
        Action<SpellJudgeSpec> _onJudged;

        /// <summary>바로 판정 입력. 없으면 null (Awake에서 에러 로그)</summary>
        InputAction _cast;

        /// <summary>그리기를 받는 중인지 (Begin ~ End)</summary>
        public bool IsActive => _judge != null;

        /// <summary>시전 상태인지</summary>
        public bool IsCasting => _casting;

        #region Awake - 연결 확인, 바로 판정 액션 찾기, 안내 글에 대기 초를 넣고 빈 칸으로
        void Awake()
        {
            if (_view == null) Debug.LogError("[Draw] SpellDrawView 연결 안 됨", this);
            _cast = InputSystem.actions.FindAction(CastActionPath);
            if (_cast == null) Debug.LogError($"[Draw] {CastActionPath} 액션 없음. Space 바로 판정이 안 됨", this);
            if (_view != null)
            {
                _view.Hint.StringReference.Arguments = new object[] { Mathf.RoundToInt(_waitSeconds) };
                _view.Hint.RefreshString();
            }
            ResetDrawing();
        }
        #endregion

        #region OnDisable - 꺼지면 그리기를 끝냄 (입력 구독 해제)
        void OnDisable() => End();
        #endregion

        #region Begin - 그리기 받기 시작: 판정·결과 할 일과 결과 글에 쓸 주문 이름을 받고, Space 구독, 획·결과를 비움 (다시 부르면 처음부터)
        /// <param name="nameAt">판정 결과의 ShapeIndex 번호 → 주문 이름 (지금 언어)</param>
        /// <param name="judge">그린 획 → 판정</param>
        /// <param name="onJudged">판정·취소 뒤 할 일 (없으면 null)</param>
        public void Begin(Func<int, string> nameAt, Func<IReadOnlyList<Vector2[]>, SpellJudgeSpec> judge, Action<SpellJudgeSpec> onJudged)
        {
            _nameAt = nameAt ?? throw new ArgumentNullException(nameof(nameAt));
            _judge = judge ?? throw new ArgumentNullException(nameof(judge));
            _onJudged = onJudged;
            if (_cast != null)
            {
                _cast.performed -= OnCastPerformed;
                _cast.performed += OnCastPerformed;
            }
            ResetDrawing();
        }
        #endregion

        #region End - 그리기 끝: Space 구독 해제, 할 일·획·결과·따라 그릴 그림을 비움 (여러 번 불려도 됨, Awake 전에도 됨)
        public void End()
        {
            if (_cast != null) _cast.performed -= OnCastPerformed;
            _judge = null;
            _onJudged = null;
            _nameAt = null;
            ResetDrawing();
            if (_view != null) _view.Guide.Clear();
        }
        #endregion

        #region ShowGuide - 따라 그릴 그림을 흐리게 깔고 안내 글을 숨김. null이면 그림을 지우고 안내 글을 보임 (자유 그리기, 그리기 전일 때)
        /// <param name="shape">따라 그릴 그림 (자유 그리기면 null)</param>
        public void ShowGuide(SpellShapeEntry shape)
        {
            if (_view == null) return;
            if (shape == null) _view.Guide.Clear();
            else _view.Guide.SetStrokes(shape.PartStrokes.SelectMany(part => part), true);
            if (!_casting) _view.Hint.gameObject.SetActive(shape == null);
        }
        #endregion

        #region CancelCast - 직접 취소 (Esc·우클릭): 시전 상태면 '직접 취소'로 끝냄
        public void CancelCast()
        {
            if (!_casting) return;
            Finish(SpellJudgeSpec.Cancel(SpellJudgeReason.Canceled));
        }
        #endregion

        #region Update - 시전 상태에서 그리는 중이 아니면 대기 시간을 세고 막대를 줄임. 다 차면 판정 (게임 시간)
        void Update()
        {
            if (!_casting || _view == null) return;
            if (_stroke != null)
            {
                _view.Timer.fillAmount = 1f;
                return;
            }
            _idle += Time.deltaTime;
            _view.Timer.fillAmount = Mathf.Clamp01(1f - _idle / _waitSeconds);
            if (_idle >= _waitSeconds) Resolve();
        }
        #endregion

        #region OnInitializePotentialDrag - 끌기 문턱 없이 첫 움직임부터 그림
        /// <param name="eventData">포인터 정보</param>
        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;
        #endregion

        #region OnPointerDown - 우클릭은 취소. 좌클릭은 (시전 상태가 아니면 시작하고) 새 획 시작
        /// <param name="eventData">포인터 정보</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsActive) return;
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                CancelCast();
                return;
            }
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!_casting) StartCast();
            EndStroke();
            _stroke = new List<Vector2> { ToLocal(eventData) };
            _idle = 0f;
            RefreshInk();
        }
        #endregion

        #region OnDrag - 그리는 중이면 점을 더함 (앞 점과 너무 가까우면 뺌)
        /// <param name="eventData">포인터 정보</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (_stroke == null || eventData.button != PointerEventData.InputButton.Left) return;
            // 칸 좌표 점
            Vector2 point = ToLocal(eventData);
            if ((point - _stroke[_stroke.Count - 1]).sqrMagnitude < MinPointGap * MinPointGap) return;
            _stroke.Add(point);
            _idle = 0f;
            RefreshInk();
        }
        #endregion

        #region OnPointerUp - 좌클릭을 떼면 획 끝 (칸 밖에서 떼도 옴)
        /// <param name="eventData">포인터 정보</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            EndStroke();
        }
        #endregion

        #region OnCastPerformed - Space: 시전 상태면 그리던 획을 끝내고 바로 판정
        /// <param name="context">입력 정보 (안 씀)</param>
        void OnCastPerformed(InputAction.CallbackContext context)
        {
            if (!_casting) return;
            EndStroke();
            Resolve();
        }
        #endregion

        #region StartCast - 시전 상태 시작: 앞 그림·결과 글·안내 글을 지우고 막대를 채움
        void StartCast()
        {
            _casting = true;
            _strokes.Clear();
            _idle = 0f;
            if (_view == null) return;
            _view.Result.gameObject.SetActive(false);
            _view.Detail.gameObject.SetActive(false);
            _view.Hint.gameObject.SetActive(false);
            _view.Timer.gameObject.SetActive(true);
            _view.Timer.fillAmount = 1f;
        }
        #endregion

        #region EndStroke - 그리던 획을 끝냄 (점 2개 이상이면 남김)
        void EndStroke()
        {
            if (_stroke == null) return;
            if (_stroke.Count >= 2) _strokes.Add(_stroke.ToArray());
            _stroke = null;
            _idle = 0f;
            RefreshInk();
        }
        #endregion

        #region Resolve - 그린 획(복사본)으로 판정하고 끝냄
        void Resolve()
        {
            if (_judge == null) return;
            Finish(_judge(_strokes.ToArray()));
        }
        #endregion

        #region Finish - 시전 상태를 끝내고 결과 글을 보인 뒤 할 일 (그린 획은 다음 좌클릭까지 남겨 둠)
        /// <param name="spec">판정 결과</param>
        void Finish(SpellJudgeSpec spec)
        {
            _casting = false;
            _stroke = null;
            _idle = 0f;
            if (_view != null) _view.Timer.gameObject.SetActive(false);
            ShowResult(spec);
            _onJudged?.Invoke(spec);
        }
        #endregion

        #region ResetDrawing - 시전 상태·획·결과 글을 비우고 안내 글을 보임
        void ResetDrawing()
        {
            _casting = false;
            _stroke = null;
            _idle = 0f;
            _strokes.Clear();
            if (_view == null) return;
            _view.Ink.Clear();
            _view.Timer.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Detail.gameObject.SetActive(false);
            _view.Hint.gameObject.SetActive(true);
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

        #region ToLocal - 포인터 화면 좌표를 이 칸 좌표로 (World Space 캔버스면 누른 카메라 기준)
        /// <param name="eventData">포인터 정보</param>
        Vector2 ToLocal(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 local);
            return local;
        }
        #endregion

        #region ShowResult - 결과 줄 (시전: "이름 일치율%", 정확이면 표시 / 취소: "취소: 이유")과 자세히 (맞는 주문이 없으면 가장 가까운 주문, 아니면 부분 일치율·남는 선)
        /// <param name="spec">판정 결과</param>
        void ShowResult(SpellJudgeSpec spec)
        {
            if (_view == null) return;
            _view.Hint.gameObject.SetActive(false);
            if (spec.IsCast)
                SetText(_view.Result, spec.Accuracy >= SpellRecognitionCalculator.ExactAccuracy ? CastExactKey : CastKey, SpellName(spec.Candidate), Percent(spec.Accuracy));
            else
                SetText(_view.Result, CancelKey, ReasonText(spec));
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

        #region ReasonText - 취소 이유 글 (부분 부족: 모자란 부분, 미습득: 빠진 룬, 애매함: 겹친 주문 이름)
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
