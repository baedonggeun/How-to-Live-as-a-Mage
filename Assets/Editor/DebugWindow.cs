using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mage
{
    /// <summary>디버그 패널 (에디터 전용 도구 창, Tools > Mage > 디버그 패널): 기능 쪽 Debug partial에 선언한 조작([DebugControl])을 묶음별로 모아, Play 중 지금 흐름 상태가 넘긴 곳(DebugTargets)에 붙여 보임. 켜기/끄기·숫자 칸·값 줄·버튼. 켠 토글은 이 PC에 기억(EditorPrefs)해서 Play 중 화면이 바뀔 때마다 자동으로 다시 켬 (창을 닫아 둬도). Play 전에도 토글을 켜 둘 수 있음. 기능 이름·조작 목록은 여기 없음 (기능이 선언)</summary>
    public sealed class DebugWindow : EditorWindow
    {
        /// <summary>켜 둔 토글을 기억하는 EditorPrefs 키 앞부분 (뒤에 묶음/이름)</summary>
        const string RememberPrefix = "Mage.Debug.On.";

        /// <summary>숫자 칸 너비 (px)</summary>
        const float NumberWidth = 70f;

        /// <summary>지금 값 글 너비 (px)</summary>
        const float CurrentWidth = 90f;

        /// <summary>조작 모양 (묶음 안에서 이 순서로 보임)</summary>
        enum ControlKind
        {
            /// <summary>값 줄 (읽기만)</summary>
            Value,

            /// <summary>켜기/끄기 (읽고 쓰는 bool)</summary>
            Toggle,

            /// <summary>숫자 칸 + 넣기 (읽고 쓰는 int·float)</summary>
            Number,

            /// <summary>버튼 (인자 없는 메서드)</summary>
            Button,
        }

        /// <summary>선언된 조작 하나 (한 형식의 프로퍼티·메서드 하나)</summary>
        sealed class ControlEntry
        {
            /// <summary>붙인 프로퍼티·메서드</summary>
            public MemberInfo Member;

            /// <summary>모양</summary>
            public ControlKind Kind;
        }

        /// <summary>패널 한 줄: 같은 묶음·이름의 조작을 모음 (지금 화면에 붙은 곳 하나로 보임)</summary>
        sealed class RowEntry
        {
            /// <summary>묶음 이름</summary>
            public string Group;

            /// <summary>조작 이름</summary>
            public string Label;

            /// <summary>모양 (먼저 나온 조작 기준)</summary>
            public ControlKind Kind;

            /// <summary>이 줄의 조작 (선언한 곳마다 하나)</summary>
            public readonly List<ControlEntry> Members = new();

            /// <summary>기억 키 (묶음/이름)</summary>
            public string Key => Group + "/" + Label;
        }

        /// <summary>패널 줄 (묶음 이름 순, 묶음 안은 모양·선언한 형식 이름·선언 순. 창과 자동 켜기가 같이 씀, 컴파일하면 다시 모음)</summary>
        static readonly List<RowEntry> Rows = new();

        /// <summary>잘못 선언된 것 (모양을 못 정함·같은 이름에 다른 모양)</summary>
        static readonly List<string> Invalid = new();

        /// <summary>조작을 모았는지</summary>
        static bool _collected;

        /// <summary>자동 켜기가 찾아 둔 GameManager (Play마다 다시 찾음)</summary>
        static GameManager _autoGame;

        /// <summary>켜 둔 토글을 마지막으로 다시 켠 흐름 상태 (화면이 바뀌면 다시 켬)</summary>
        static FlowStateBase _appliedState;

        /// <summary>지금 상태가 넘긴 곳 (형식 → 인스턴스. Layout 때마다 다시 받음)</summary>
        readonly Dictionary<Type, object> _live = new();

        /// <summary>숫자 칸에 적어 둔 값 (기억 키 → 값)</summary>
        readonly Dictionary<string, double> _pending = new(StringComparer.Ordinal);

        /// <summary>찾아 둔 GameManager (Play마다 다시 찾음)</summary>
        GameManager _game;

        /// <summary>지금 흐름 상태 이름 (없으면 null)</summary>
        string _stateName;

        /// <summary>Play 중인지 (Layout 때 정함)</summary>
        bool _playing;

        /// <summary>조작을 막을지 (상태 없음·화면 전환 중·상태가 막음. Layout 때 정함)</summary>
        bool _busy;

        /// <summary>붙일 곳을 받다 난 에러 (없으면 null)</summary>
        string _targetError;

        /// <summary>스크롤 위치</summary>
        Vector2 _scroll;

        /// <summary>마지막 조작이 한 일</summary>
        string _log = "조작하면 한 일이 여기 나와.";

        #region Open - 메뉴에서 창 열기
        [MenuItem("Tools/Mage/디버그 패널")]
        static void Open() => GetWindow<DebugWindow>("디버그 패널");
        #endregion

        #region HookAutoApply - 에디터를 켤 때·컴파일 뒤 자동 켜기를 매 업데이트에 걸어 둠 (창을 닫아 둬도 돎)
        [InitializeOnLoadMethod]
        static void HookAutoApply()
        {
            EditorApplication.update -= AutoApply;
            EditorApplication.update += AutoApply;
        }
        #endregion

        #region AutoApply - Play 중 화면(흐름 상태)이 바뀌고 준비되면 한 번: 켜 둔 토글 중 그 화면에 붙은 것이 꺼져 있으면 켬
        static void AutoApply()
        {
            if (!EditorApplication.isPlaying)
            {
                _autoGame = null;
                _appliedState = null;
                return;
            }
            if (_autoGame == null) _autoGame = Object.FindAnyObjectByType<GameManager>();
            // 지금 흐름 상태 (시작 전이면 null)
            FlowStateBase state = _autoGame != null ? _autoGame.State : null;
            if (state == null || state == _appliedState || _autoGame.IsSwitching || state.DebugBusy) return;
            _appliedState = state;
            EnsureCollected();
            // 이 화면에 붙은 곳
            var live = new Dictionary<Type, object>();
            try
            {
                FillLive(state, live);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
            // 다시 켠 수
            int count = 0;
            foreach (RowEntry row in Rows)
            {
                if (row.Kind != ControlKind.Toggle || !IsRemembered(row)) continue;
                foreach (ControlEntry control in row.Members)
                {
                    if (!live.TryGetValue(control.Member.DeclaringType, out object target)) continue;
                    try
                    {
                        if (Read(control, target) is bool on && on) continue;
                        Write(control, target, true);
                        count++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(Unwrap(e));
                    }
                }
            }
            if (count > 0) Debug.Log(string.Format(CultureInfo.InvariantCulture, "[Debug] 켜 둔 조작 {0}개를 {1}에 다시 켬", count, state.GetType().Name));
        }
        #endregion

        #region OnEnable - 선언된 조작을 모음
        void OnEnable() => EnsureCollected();
        #endregion

        #region OnInspectorUpdate - Play 중이면 1초에 10번 다시 그림 (지금 값)
        void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }
        #endregion

        #region EnsureCollected - 처음이면 게임 코드의 [DebugControl] 프로퍼티·메서드를 모아 모양을 정하고(못 정하면 잘못 선언), 묶음 이름·모양·형식 이름·선언 순으로 정렬해 같은 묶음·이름끼리 한 줄로 모음
        static void EnsureCollected()
        {
            if (_collected) return;
            _collected = true;
            Rows.Clear();
            Invalid.Clear();
            // 모은 조작 (정렬 전)
            var found = new List<(DebugControlAttribute info, ControlEntry control)>();
            // 형식마다 직접 선언한 인스턴스 멤버 (공개·비공개)
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (Type type in typeof(FlowStateBase).Assembly.GetTypes())
            {
                foreach (PropertyInfo property in type.GetProperties(flags))
                {
                    // 붙은 표시 (없으면 null)
                    var info = property.GetCustomAttribute<DebugControlAttribute>();
                    if (info == null) continue;
                    // 정한 모양 (못 정하면 null)
                    ControlKind? kind = KindOf(property);
                    if (kind == null)
                    {
                        Invalid.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1} (읽기 있는 프로퍼티: 쓰기도 있으면 bool·int·float만)", type.Name, property.Name));
                        continue;
                    }
                    found.Add((info, new ControlEntry { Member = property, Kind = kind.Value }));
                }
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    // 붙은 표시 (없으면 null)
                    var info = method.GetCustomAttribute<DebugControlAttribute>();
                    if (info == null) continue;
                    if (method.GetParameters().Length != 0 || (method.ReturnType != typeof(void) && method.ReturnType != typeof(string)))
                    {
                        Invalid.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1} (인자 없음·void 또는 string)", type.Name, method.Name));
                        continue;
                    }
                    found.Add((info, new ControlEntry { Member = method, Kind = ControlKind.Button }));
                }
            }
            found.Sort((a, b) =>
            {
                // 묶음 이름 비교
                int byGroup = string.CompareOrdinal(a.info.Group, b.info.Group);
                if (byGroup != 0) return byGroup;
                // 모양 비교 (값 줄 → 켜기/끄기 → 숫자 → 버튼)
                int byKind = a.control.Kind.CompareTo(b.control.Kind);
                if (byKind != 0) return byKind;
                // 선언한 형식 이름 비교
                int byType = string.CompareOrdinal(a.control.Member.DeclaringType?.FullName, b.control.Member.DeclaringType?.FullName);
                return byType != 0 ? byType : a.control.Member.MetadataToken.CompareTo(b.control.Member.MetadataToken);
            });
            foreach (var (info, control) in found)
            {
                // 같은 묶음·이름의 줄 (없으면 null)
                RowEntry row = Rows.Find(r => string.Equals(r.Group, info.Group, StringComparison.Ordinal) && string.Equals(r.Label, info.Label, StringComparison.Ordinal));
                if (row == null)
                {
                    row = new RowEntry { Group = info.Group, Label = info.Label, Kind = control.Kind };
                    Rows.Add(row);
                }
                else if (row.Kind != control.Kind)
                {
                    Invalid.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1} ({2}/{3}에 다른 모양이 이미 있음)", control.Member.DeclaringType?.Name, control.Member.Name, info.Group, info.Label));
                    continue;
                }
                row.Members.Add(control);
            }
        }
        #endregion

        #region KindOf - 프로퍼티 모양: 읽기만 = 값 줄, 읽고 쓰는 bool = 켜기/끄기, 읽고 쓰는 int·float = 숫자 칸. 아니면 null
        /// <param name="property">검사할 프로퍼티</param>
        static ControlKind? KindOf(PropertyInfo property)
        {
            if (property.GetMethod == null || property.GetIndexParameters().Length > 0) return null;
            if (property.SetMethod == null) return ControlKind.Value;
            // 프로퍼티 형식
            Type type = property.PropertyType;
            if (type == typeof(bool)) return ControlKind.Toggle;
            if (type == typeof(int) || type == typeof(float)) return ControlKind.Number;
            return null;
        }
        #endregion

        #region FillLive - 흐름 상태가 넘긴 곳을 형식 → 인스턴스로 채움 (지워진 것 뺌)
        /// <param name="state">지금 흐름 상태</param>
        /// <param name="live">채울 곳 (먼저 비움)</param>
        static void FillLive(FlowStateBase state, Dictionary<Type, object> live)
        {
            live.Clear();
            foreach (object target in state.DebugTargets())
            {
                if (target == null || (target is Object unityObject && unityObject == null)) continue;
                live[target.GetType()] = target;
            }
        }
        #endregion

        #region OnGUI - Layout 때 붙일 곳을 새로 받고, 머리줄(지금 상태), 잘못 선언, 묶음마다 줄, 마지막 한 일
        void OnGUI()
        {
            EnsureCollected();
            if (Event.current.type == EventType.Layout) Refresh();
            EditorGUILayout.LabelField(_playing
                ? string.Format(CultureInfo.InvariantCulture, "지금: {0}", _stateName ?? "-")
                : "Play 전: 토글만 켜 둘 수 있음 (켜 둔 건 Play 때 화면마다 자동으로 켬)", EditorStyles.boldLabel);
            if (Invalid.Count > 0) EditorGUILayout.HelpBox("잘못 선언 (무시함): " + string.Join(", ", Invalid), MessageType.Warning);
            if (_targetError != null) EditorGUILayout.HelpBox("붙일 곳을 못 받음: " + _targetError, MessageType.Warning);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            // 지금 그리는 묶음 (없으면 null)
            string group = null;
            foreach (RowEntry row in Rows)
            {
                if (!string.Equals(row.Group, group, StringComparison.Ordinal))
                {
                    if (group != null) EditorGUILayout.EndVertical();
                    group = row.Group;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(group, EditorStyles.boldLabel);
                }
                DrawRow(row);
            }
            if (group != null) EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("→ " + _log, MessageType.None);
        }
        #endregion

        #region Refresh - Play 중이면 지금 상태와 그 상태가 넘긴 곳을 받고, 조작을 막을지 정함
        void Refresh()
        {
            _playing = EditorApplication.isPlaying;
            _live.Clear();
            _stateName = null;
            _targetError = null;
            // 지금 흐름 상태 (Play 중이 아니거나 시작 전이면 null)
            FlowStateBase state = null;
            if (_playing)
            {
                if (_game == null) _game = FindAnyObjectByType<GameManager>();
                state = _game != null ? _game.State : null;
            }
            if (state != null)
            {
                _stateName = state.GetType().Name;
                try
                {
                    FillLive(state, _live);
                }
                catch (Exception e)
                {
                    _live.Clear();
                    _targetError = e.Message;
                }
            }
            _busy = state == null || _game.IsSwitching || state.DebugBusy || _targetError != null;
        }
        #endregion

        #region DrawRow - 줄 하나: 지금 화면에 붙은 조작을 골라 모양대로 그림 (막혀 있으면 붙은 곳이 없는 것처럼)
        /// <param name="row">그릴 줄</param>
        void DrawRow(RowEntry row)
        {
            // 지금 화면에 붙은 조작과 붙은 곳 (없으면 null)
            ControlEntry control = LiveMember(row, out object target);
            // 지금 조작할 수 있는지
            bool live = control != null && !_busy;
            switch (row.Kind)
            {
                case ControlKind.Toggle: DrawToggle(row, control, target, live); break;
                case ControlKind.Number: DrawNumber(row, control, target, live); break;
                case ControlKind.Value: DrawValue(row, control, target, live); break;
                case ControlKind.Button: DrawButton(row, control, target, live); break;
            }
        }
        #endregion

        #region LiveMember - 줄의 조작 중 지금 상태가 넘긴 곳에 선언된 첫 조작 (없으면 null)
        /// <param name="row">줄</param>
        /// <param name="target">붙은 곳 (없으면 null)</param>
        ControlEntry LiveMember(RowEntry row, out object target)
        {
            foreach (ControlEntry control in row.Members)
                if (_live.TryGetValue(control.Member.DeclaringType, out target)) return control;
            target = null;
            return null;
        }
        #endregion

        #region DrawToggle - 켜기/끄기: 지금 화면에 붙었으면 실제 값을 보이고 바꾸면 적용·기억, 아니면 기억만 바꿈 (Play 전·다른 화면). 옆에 기억 표시
        /// <param name="row">줄</param>
        /// <param name="control">붙은 조작 (없으면 null)</param>
        /// <param name="target">붙은 곳 (없으면 null)</param>
        /// <param name="live">지금 조작할 수 있는지</param>
        void DrawToggle(RowEntry row, ControlEntry control, object target, bool live)
        {
            // 켜 둔 것으로 기억했는지
            bool remembered = IsRemembered(row);
            EditorGUILayout.BeginHorizontal();
            if (live)
            {
                // 지금 실제 값
                bool current = TryRead(control, target, out object value) && value is bool on && on;
                // 고친 값
                bool next = EditorGUILayout.ToggleLeft(row.Label, current);
                if (next != current) SetToggle(row, control, target, next);
            }
            else
            {
                // 고친 기억
                bool next = EditorGUILayout.ToggleLeft(row.Label, remembered);
                if (next != remembered)
                {
                    Remember(row, next);
                    _log = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", row.Label, next ? "켜 둠 (그 화면에서 자동으로 켬)" : "기억 지움");
                }
            }
            GUILayout.Label(ToggleNote(live, remembered), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region ToggleNote - 토글 옆 글: 기억했는지·지금 화면이 아닌지
        /// <param name="live">지금 조작할 수 있는지</param>
        /// <param name="remembered">켜 둔 것으로 기억했는지</param>
        string ToggleNote(bool live, bool remembered)
        {
            if (live) return remembered ? "기억함" : string.Empty;
            if (!_playing) return remembered ? "켜 둠 · Play 때 자동으로 켬" : string.Empty;
            return remembered ? "지금 화면 아님 · 그 화면에서 자동으로 켬" : "지금 화면 아님";
        }
        #endregion

        #region SetToggle - 켜거나 끄고 기억을 맞춤 (켜면 기억, 끄면 지움). 다시 읽어 안 바뀌었으면 그렇다고 알림
        /// <param name="row">줄</param>
        /// <param name="control">붙은 조작</param>
        /// <param name="target">붙은 곳</param>
        /// <param name="on">켤지</param>
        void SetToggle(RowEntry row, ControlEntry control, object target, bool on)
        {
            try
            {
                Write(control, target, on);
                Remember(row, on);
                // 다시 읽은 값
                bool now = TryRead(control, target, out object value) && value is bool b && b;
                _log = now == on
                    ? string.Format(CultureInfo.InvariantCulture, "{0} {1}", row.Label, on ? "켬 (기억함)" : "끔")
                    : string.Format(CultureInfo.InvariantCulture, "{0}: 안 바뀜 (지금은 못 바꾸거나 되돌릴 원래 값이 없음)", row.Label);
            }
            catch (Exception e)
            {
                Fail(row, e);
            }
        }
        #endregion

        #region DrawNumber - 숫자 칸: 지금 값, 적을 칸(처음엔 지금 값), 넣기 (지금 화면에 붙었을 때만)
        /// <param name="row">줄</param>
        /// <param name="control">붙은 조작 (없으면 null)</param>
        /// <param name="target">붙은 곳 (없으면 null)</param>
        /// <param name="live">지금 조작할 수 있는지</param>
        void DrawNumber(RowEntry row, ControlEntry control, object target, bool live)
        {
            // 소수 칸인지 (첫 조작의 형식 기준)
            bool isFloat = ((PropertyInfo)row.Members[0].Member).PropertyType == typeof(float);
            // 지금 값 글
            string current = "-";
            if (live && TryRead(control, target, out object value))
            {
                current = Format(value);
                if (!_pending.ContainsKey(row.Key)) _pending[row.Key] = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(row.Label);
            using (new EditorGUI.DisabledScope(!live))
            {
                GUILayout.Label("지금 " + current, GUILayout.Width(CurrentWidth));
                _pending.TryGetValue(row.Key, out double pending);
                // 고친 값
                double edited = isFloat
                    ? EditorGUILayout.FloatField((float)pending, GUILayout.Width(NumberWidth))
                    : EditorGUILayout.IntField((int)Math.Round(pending), GUILayout.Width(NumberWidth));
                _pending[row.Key] = edited;
                if (GUILayout.Button("넣기", GUILayout.ExpandWidth(false))) SetNumber(row, control, target, edited, isFloat);
            }
            if (!live) GUILayout.Label(_playing ? "지금 화면 아님" : "Play 중에만", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region SetNumber - 적은 값을 넣고 다시 읽은 값을 한 일에
        /// <param name="row">줄</param>
        /// <param name="control">붙은 조작</param>
        /// <param name="target">붙은 곳</param>
        /// <param name="value">넣을 값</param>
        /// <param name="isFloat">소수 칸인지</param>
        void SetNumber(RowEntry row, ControlEntry control, object target, double value, bool isFloat)
        {
            if (control == null) return;
            try
            {
                Write(control, target, isFloat ? (float)value : (int)Math.Round(value));
                TryRead(control, target, out object now);
                _pending.Remove(row.Key);
                _log = string.Format(CultureInfo.InvariantCulture, "{0} → {1}", row.Label, Format(now));
            }
            catch (Exception e)
            {
                Fail(row, e);
            }
        }
        #endregion

        #region DrawValue - 값 줄: 지금 화면에 붙었으면 값, 아니면 안내
        /// <param name="row">줄</param>
        /// <param name="control">붙은 조작 (없으면 null)</param>
        /// <param name="target">붙은 곳 (없으면 null)</param>
        /// <param name="live">지금 조작할 수 있는지</param>
        void DrawValue(RowEntry row, ControlEntry control, object target, bool live)
        {
            // 보일 글 (읽다 에러면 에러 글)
            string text;
            if (!_playing) text = "Play 중에만";
            else if (control == null) text = "지금 화면 아님";
            else if (!live) text = "준비 중";
            else
            {
                TryRead(control, target, out object value);
                text = Format(value);
            }
            EditorGUILayout.LabelField(row.Label, text);
        }
        #endregion

        #region DrawButton - 버튼 (지금 화면에 붙었을 때만 켜짐). 누르면 부르고 돌려준 글(없으면 이름)을 한 일에
        /// <param name="row">줄</param>
        /// <param name="control">붙은 조작 (없으면 null)</param>
        /// <param name="target">붙은 곳 (없으면 null)</param>
        /// <param name="live">지금 조작할 수 있는지</param>
        void DrawButton(RowEntry row, ControlEntry control, object target, bool live)
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!live))
            {
                if (GUILayout.Button(row.Label, GUILayout.ExpandWidth(false)) && control != null)
                {
                    try
                    {
                        _log = ((MethodInfo)control.Member).Invoke(target, null) as string ?? row.Label;
                    }
                    catch (Exception e)
                    {
                        Fail(row, e);
                    }
                }
            }
            if (!live) GUILayout.Label(_playing ? "지금 화면 아님" : "Play 중에만", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region TryRead - 프로퍼티 값을 읽음. 에러면 value에 에러 글을 넣고 false
        /// <param name="control">조작</param>
        /// <param name="target">붙은 곳</param>
        /// <param name="value">읽은 값 (에러면 에러 글)</param>
        static bool TryRead(ControlEntry control, object target, out object value)
        {
            try
            {
                value = Read(control, target);
                return true;
            }
            catch (Exception e)
            {
                value = "에러: " + Unwrap(e).Message;
                return false;
            }
        }
        #endregion

        #region Read - 프로퍼티 값
        /// <param name="control">조작</param>
        /// <param name="target">붙은 곳</param>
        static object Read(ControlEntry control, object target) => ((PropertyInfo)control.Member).GetValue(target);
        #endregion

        #region Write - 프로퍼티에 값을 넣음
        /// <param name="control">조작</param>
        /// <param name="target">붙은 곳</param>
        /// <param name="value">넣을 값</param>
        static void Write(ControlEntry control, object target, object value) => ((PropertyInfo)control.Member).SetValue(target, value);
        #endregion

        #region Format - 값 글 (없으면 -, 소수는 둘째 자리까지)
        /// <param name="value">값</param>
        static string Format(object value) => value switch
        {
            null => "-",
            float number => number.ToString("0.##", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture),
        };
        #endregion

        #region IsRemembered - 켜 둔 것으로 기억했는지 (EditorPrefs)
        /// <param name="row">줄</param>
        static bool IsRemembered(RowEntry row) => EditorPrefs.GetBool(RememberPrefix + row.Key, false);
        #endregion

        #region Remember - 켜 둔 것으로 기억하거나 지움
        /// <param name="row">줄</param>
        /// <param name="on">기억할지 (아니면 지움)</param>
        static void Remember(RowEntry row, bool on)
        {
            if (on) EditorPrefs.SetBool(RememberPrefix + row.Key, true);
            else EditorPrefs.DeleteKey(RememberPrefix + row.Key);
        }
        #endregion

        #region Fail - 조작 안에서 난 에러를 콘솔과 한 일에
        /// <param name="row">줄</param>
        /// <param name="e">에러</param>
        void Fail(RowEntry row, Exception e)
        {
            // 조작 안에서 난 에러
            Exception inner = Unwrap(e);
            Debug.LogException(inner);
            _log = string.Format(CultureInfo.InvariantCulture, "{0}: 에러 {1}", row.Label, inner.Message);
        }
        #endregion

        #region Unwrap - 리플렉션이 감싼 에러를 벗김
        /// <param name="e">에러</param>
        static Exception Unwrap(Exception e) => e is TargetInvocationException { InnerException: not null } wrapped ? wrapped.InnerException : e;
        #endregion
    }
}
