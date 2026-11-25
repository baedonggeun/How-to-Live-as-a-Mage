using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mage
{
    /// <summary>확인 패널 (에디터 전용 도구 창, Tools > Mage > 확인 패널): 기능 쪽 Debug partial에 선언한 확인 버튼([CheckButton])·상태 줄([CheckStatus])을 다 모아, Play 중 지금 흐름 상태가 넘긴 곳(CheckTargets)에 붙여 보임. 확인 목록 글은 docs/plan-{plan}.md의 '## 확인 목록'에서 읽음. 항목별(목록 순)·기능별(선언한 곳마다) 보기, 번호 칩으로 확인 표시 (EditorPrefs, 처음 값은 목록의 ✅). 기능 이름·버튼 목록은 여기 없음 (기능이 선언)</summary>
    public sealed class CheckWindow : EditorWindow
    {
        /// <summary>확인 표시 EditorPrefs 키 앞부분 (뒤에 plan.번호)</summary>
        const string DonePrefsPrefix = "Mage.Check.";

        /// <summary>plan 문서에서 확인 목록이 시작하는 줄 앞부분</summary>
        const string ListHeader = "## 확인 목록";

        /// <summary>확인한 항목 표시 (plan 문서)</summary>
        const string DoneMark = "✅";

        /// <summary>하위 항목 들여쓰기 (px)</summary>
        const float SubIndent = 16f;

        /// <summary>번호 칩 너비 (px)</summary>
        const float ChipWidth = 44f;

        /// <summary>확인 목록 한 줄 ("N." 또는 "N-k." + 글)</summary>
        static readonly Regex ItemLine = new(@"^\s*(\d+(?:-\d+)?)\.\s+(.*)$");

        /// <summary>확인한 칩 색</summary>
        static readonly Color DoneColor = new(0.55f, 0.85f, 0.55f);

        /// <summary>보기 이름 (항목별·기능별)</summary>
        static readonly string[] ViewNames = { "항목별", "기능별" };

        /// <summary>확인 목록 한 항목</summary>
        sealed class ItemEntry
        {
            /// <summary>번호 ("3" 또는 "3-1")</summary>
            public string Id;

            /// <summary>글 (✅ 뺌)</summary>
            public string Text;

            /// <summary>아래 번호가 있는 묶음 제목인지</summary>
            public bool Group;

            /// <summary>plan 문서에 ✅가 있는지 (확인 칩 처음 값)</summary>
            public bool DoneInDoc;
        }

        /// <summary>선언된 확인 버튼 하나</summary>
        sealed class ButtonEntry
        {
            /// <summary>누르면 부를 메서드</summary>
            public MethodInfo Method;

            /// <summary>plan 이름·번호·버튼 글</summary>
            public CheckButtonAttribute Info;
        }

        /// <summary>선언된 상태 줄 하나</summary>
        sealed class StatusEntry
        {
            /// <summary>읽을 메서드</summary>
            public MethodInfo Method;

            /// <summary>상태 줄 이름</summary>
            public string Label;
        }

        /// <summary>읽어 둔 plan 확인 목록 하나</summary>
        sealed class PlanEntry
        {
            /// <summary>항목 (문서 순서)</summary>
            public readonly List<ItemEntry> Items = new();

            /// <summary>읽을 때 문서 고친 시각 (바뀌면 다시 읽음)</summary>
            public DateTime Stamp;

            /// <summary>문서를 못 읽었으면 이유 (읽었으면 null)</summary>
            public string Error;
        }

        /// <summary>선언된 확인 버튼 (선언한 형식 이름·선언 순)</summary>
        readonly List<ButtonEntry> _buttons = new();

        /// <summary>선언된 상태 줄 (선언한 형식 이름·선언 순)</summary>
        readonly List<StatusEntry> _statuses = new();

        /// <summary>잘못 선언된 것 (static·인자 있음·돌려주는 형식 틀림)</summary>
        readonly List<string> _invalid = new();

        /// <summary>버튼·상태 줄을 선언한 형식 (이름 순)</summary>
        readonly List<Type> _owners = new();

        /// <summary>버튼이 쓰는 plan 이름 (처음 나온 순)</summary>
        readonly List<string> _planNames = new();

        /// <summary>plan 이름 → 읽어 둔 확인 목록</summary>
        readonly Dictionary<string, PlanEntry> _plans = new(StringComparer.Ordinal);

        /// <summary>지금 상태가 넘긴 곳 (넘긴 순서. Layout 때마다 다시 받음)</summary>
        readonly List<object> _targets = new();

        /// <summary>지금 상태가 넘긴 곳 (형식 → 인스턴스)</summary>
        readonly Dictionary<Type, object> _live = new();

        /// <summary>찾아 둔 GameManager (Play마다 다시 찾음)</summary>
        GameManager _game;

        /// <summary>지금 흐름 상태 이름 (없으면 null)</summary>
        string _stateName;

        /// <summary>Play 중인지 (Layout 때 정함)</summary>
        bool _playing;

        /// <summary>버튼을 막을지 (상태 없음·화면 전환 중·상태가 막음. Layout 때 정함)</summary>
        bool _busy;

        /// <summary>붙일 곳을 받다 난 에러 (없으면 null)</summary>
        string _targetError;

        /// <summary>기능별 보기인지 (아니면 항목별)</summary>
        bool _byOwner;

        /// <summary>스크롤 위치</summary>
        Vector2 _scroll;

        /// <summary>마지막 버튼이 한 일</summary>
        string _log = "버튼을 눌러 봐. 한 일이 여기 나와.";

        #region Open - 메뉴에서 창 열기
        [MenuItem("Tools/Mage/확인 패널")]
        static void Open() => GetWindow<CheckWindow>("확인 패널");
        #endregion

        #region OnEnable - 선언된 버튼·상태 줄을 모음
        void OnEnable() => Collect();
        #endregion

        #region OnInspectorUpdate - Play 중이면 1초에 10번 다시 그림 (상태 줄)
        void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }
        #endregion

        #region Collect - [CheckButton]·[CheckStatus]가 붙은 메서드를 모아 검사(인스턴스·인자 없음·string 또는 void)하고 형식 이름·선언 순으로 정렬, 선언한 형식·plan 이름을 모음
        void Collect()
        {
            _buttons.Clear();
            _statuses.Clear();
            _invalid.Clear();
            _owners.Clear();
            _planNames.Clear();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<CheckButtonAttribute>())
            {
                if (!IsValid(method, false)) continue;
                _buttons.Add(new ButtonEntry { Method = method, Info = method.GetCustomAttribute<CheckButtonAttribute>() });
            }
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<CheckStatusAttribute>())
            {
                if (!IsValid(method, true)) continue;
                _statuses.Add(new StatusEntry { Method = method, Label = method.GetCustomAttribute<CheckStatusAttribute>().Label });
            }
            _buttons.Sort((a, b) => CompareDeclared(a.Method, b.Method));
            _statuses.Sort((a, b) => CompareDeclared(a.Method, b.Method));
            foreach (ButtonEntry button in _buttons)
            {
                if (!_owners.Contains(button.Method.DeclaringType)) _owners.Add(button.Method.DeclaringType);
                if (!_planNames.Contains(button.Info.Plan)) _planNames.Add(button.Info.Plan);
            }
            foreach (StatusEntry status in _statuses)
                if (!_owners.Contains(status.Method.DeclaringType)) _owners.Add(status.Method.DeclaringType);
            _owners.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }
        #endregion

        #region IsValid - 인스턴스·인자 없음·돌려주는 형식(상태 줄은 string, 버튼은 string 또는 void)이면 true. 아니면 잘못 선언에 남기고 false
        /// <param name="method">검사할 메서드</param>
        /// <param name="status">상태 줄인지 (아니면 버튼)</param>
        bool IsValid(MethodInfo method, bool status)
        {
            // 돌려주는 형식이 맞는지
            bool returns = method.ReturnType == typeof(string) || (!status && method.ReturnType == typeof(void));
            if (!method.IsStatic && method.GetParameters().Length == 0 && returns) return true;
            _invalid.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1} ({2})", method.DeclaringType?.Name, method.Name, status ? "인스턴스·인자 없음·string" : "인스턴스·인자 없음·string 또는 void"));
            return false;
        }
        #endregion

        #region CompareDeclared - 선언한 형식 이름 순, 같은 형식이면 선언 순
        /// <param name="a">앞 메서드</param>
        /// <param name="b">뒤 메서드</param>
        static int CompareDeclared(MethodInfo a, MethodInfo b)
        {
            // 형식 이름 비교
            int byType = string.CompareOrdinal(a.DeclaringType?.FullName, b.DeclaringType?.FullName);
            return byType != 0 ? byType : a.MetadataToken.CompareTo(b.MetadataToken);
        }
        #endregion

        #region OnGUI - Layout 때 붙일 곳·plan 목록을 새로 받고, 머리줄(지금 상태·보기), 잘못 선언, 항목별(상태 줄 + 확인 목록) 또는 기능별, 마지막 한 일
        void OnGUI()
        {
            if (Event.current.type == EventType.Layout) Refresh();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_playing ? string.Format(CultureInfo.InvariantCulture, "지금: {0}", _stateName ?? "-") : "Play 중에만 버튼이 켜짐", EditorStyles.boldLabel);
            _byOwner = GUILayout.Toolbar(_byOwner ? 1 : 0, ViewNames, GUILayout.Width(140f)) == 1;
            EditorGUILayout.EndHorizontal();
            if (_invalid.Count > 0) EditorGUILayout.HelpBox("잘못 선언 (무시함): " + string.Join(", ", _invalid), MessageType.Warning);
            if (_targetError != null) EditorGUILayout.HelpBox("붙일 곳을 못 받음: " + _targetError, MessageType.Warning);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_byOwner) DrawByOwner();
            else
            {
                DrawStatuses();
                foreach (string plan in _planNames) DrawPlan(plan);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("→ " + _log, MessageType.None);
        }
        #endregion

        #region Refresh - Play 중이면 지금 상태와 그 상태가 넘긴 곳을 받고(지워진 것 뺌) 버튼을 막을지 정함, plan 문서가 바뀌었으면 다시 읽음
        void Refresh()
        {
            _playing = EditorApplication.isPlaying;
            _targets.Clear();
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
                    foreach (object target in state.CheckTargets())
                    {
                        if (target == null || (target is Object unityObject && unityObject == null)) continue;
                        _targets.Add(target);
                        _live[target.GetType()] = target;
                    }
                }
                catch (Exception e)
                {
                    _targets.Clear();
                    _live.Clear();
                    _targetError = e.Message;
                }
            }
            _busy = state == null || _game.IsSwitching || state.CheckBusy || _targetError != null;
            foreach (string plan in _planNames) LoadPlan(plan);
        }
        #endregion

        #region LoadPlan - docs/plan-{plan}.md가 바뀌었거나 처음이면 '## 확인 목록'의 "N."·"N-k." 줄을 읽어 둠 (못 읽으면 이유)
        /// <param name="plan">plan 이름</param>
        void LoadPlan(string plan)
        {
            // plan 문서 경로 (프로젝트 루트/docs)
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs", "plan-" + plan + ".md");
            // 문서 고친 시각 (없으면 MinValue)
            DateTime stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            if (_plans.TryGetValue(plan, out PlanEntry loaded) && loaded.Stamp == stamp) return;
            // 새로 읽는 목록
            var entry = new PlanEntry { Stamp = stamp };
            _plans[plan] = entry;
            if (stamp == DateTime.MinValue)
            {
                entry.Error = "문서 없음: " + path;
                return;
            }
            // 확인 목록 안인지
            bool inList = false;
            foreach (string line in File.ReadAllLines(path))
            {
                if (!inList)
                {
                    inList = line.StartsWith(ListHeader, StringComparison.Ordinal);
                    continue;
                }
                if (line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("---", StringComparison.Ordinal)) break;
                // 번호 줄
                Match match = ItemLine.Match(line);
                if (!match.Success) continue;
                // 글
                string text = match.Groups[2].Value;
                entry.Items.Add(new ItemEntry
                {
                    Id = match.Groups[1].Value,
                    Text = text.Replace(DoneMark, string.Empty).Trim(),
                    DoneInDoc = text.Contains(DoneMark, StringComparison.Ordinal),
                });
            }
            foreach (ItemEntry item in entry.Items)
                item.Group = entry.Items.Exists(other => other.Id.StartsWith(item.Id + "-", StringComparison.Ordinal));
            if (entry.Items.Count == 0) entry.Error = "'" + ListHeader + "'에 번호 줄 없음: " + path;
        }
        #endregion

        #region DrawStatuses - 넘긴 곳 순서로 상태 줄 '이름: 값' (Play 중이 아니면 안내만)
        void DrawStatuses()
        {
            if (!_playing) return;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            // 그린 줄이 있는지
            bool any = false;
            foreach (object target in _targets)
                foreach (StatusEntry status in _statuses)
                {
                    if (status.Method.DeclaringType != target.GetType()) continue;
                    EditorGUILayout.LabelField(status.Label, ReadStatus(status, target));
                    any = true;
                }
            if (!any) EditorGUILayout.LabelField("이 화면엔 상태 줄 없음");
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region ReadStatus - 상태 줄 값 (에러면 에러 글)
        /// <param name="status">상태 줄</param>
        /// <param name="target">붙일 곳</param>
        static string ReadStatus(StatusEntry status, object target)
        {
            try
            {
                return status.Method.Invoke(target, null) as string ?? "-";
            }
            catch (TargetInvocationException e)
            {
                return "에러: " + (e.InnerException ?? e).Message;
            }
        }
        #endregion

        #region DrawPlan - plan 하나: 머리줄(확인 수), 항목마다 칩·글·그 번호 버튼(글 속 [버튼 글] 순서), 목록에 없는 번호를 쓰는 버튼
        /// <param name="plan">plan 이름</param>
        void DrawPlan(string plan)
        {
            if (!_plans.TryGetValue(plan, out PlanEntry entry)) return;
            // 확인한 수·항목 수 (묶음 제목 뺌)
            int done = 0;
            int total = 0;
            foreach (ItemEntry item in entry.Items)
            {
                if (item.Group) continue;
                total++;
                if (IsDone(plan, item)) done++;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format(CultureInfo.InvariantCulture, "plan-{0} · 확인 {1} / {2}", plan, done, total), EditorStyles.boldLabel);
            if (entry.Error != null) EditorGUILayout.HelpBox(entry.Error, MessageType.Warning);
            foreach (ItemEntry item in entry.Items) DrawItem(plan, item);
            // 목록에 없는 번호를 쓰는 버튼
            foreach (ButtonEntry button in _buttons)
            {
                if (!string.Equals(button.Info.Plan, plan, StringComparison.Ordinal)) continue;
                foreach (string id in button.Info.Items)
                {
                    if (entry.Items.Exists(item => string.Equals(item.Id, id, StringComparison.Ordinal))) continue;
                    EditorGUILayout.HelpBox(string.Format(CultureInfo.InvariantCulture, "목록에 없는 번호 {0}: {1}.{2}", id, button.Method.DeclaringType?.Name, button.Method.Name), MessageType.Warning);
                }
            }
        }
        #endregion

        #region DrawItem - 항목 한 줄: 묶음 제목이면 굵게, 아니면 확인 칩(누르면 토글)·글. 아래에 이 번호를 쓰는 버튼 (글 속 [버튼 글] 순서)
        /// <param name="plan">plan 이름</param>
        /// <param name="item">항목</param>
        void DrawItem(string plan, ItemEntry item)
        {
            // 하위 항목 들여쓰기 (px)
            float indent = item.Id.Contains("-", StringComparison.Ordinal) ? SubIndent : 0f;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            if (item.Group) GUILayout.Label(item.Id + ". " + item.Text, EditorStyles.boldLabel);
            else
            {
                // 확인했는지
                bool done = IsDone(plan, item);
                // 칩 앞 색
                Color before = GUI.backgroundColor;
                if (done) GUI.backgroundColor = DoneColor;
                if (GUILayout.Button(item.Id, GUILayout.Width(ChipWidth))) EditorPrefs.SetInt(DoneKey(plan, item.Id), done ? 0 : 1);
                GUI.backgroundColor = before;
                GUILayout.Label(item.Text, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndHorizontal();
            // 이 번호를 쓰는 버튼 (글 속 순서)
            var buttons = _buttons.FindAll(button => string.Equals(button.Info.Plan, plan, StringComparison.Ordinal) && Array.IndexOf(button.Info.Items, item.Id) >= 0);
            buttons.Sort((a, b) => TextOrder(item.Text, a).CompareTo(TextOrder(item.Text, b)));
            foreach (ButtonEntry button in buttons) DrawButton(button, indent + ChipWidth + 4f, OwnerNote(button));
        }
        #endregion

        #region TextOrder - 항목 글 속 "[버튼 글]" 자리 (없으면 맨 뒤)
        /// <param name="text">항목 글</param>
        /// <param name="button">버튼</param>
        static int TextOrder(string text, ButtonEntry button)
        {
            // 글 속 자리
            int at = text.IndexOf("[" + button.Info.Label + "]", StringComparison.Ordinal);
            return at >= 0 ? at : int.MaxValue;
        }
        #endregion

        #region DrawByOwner - 선언한 형식마다(지금 넘긴 곳 먼저): 이름(지금 화면 아니면 표시), 상태 줄, 버튼과 쓰는 번호
        void DrawByOwner()
        {
            // 그릴 순서 (넘긴 곳 먼저, 나머지는 이름 순)
            var order = new List<Type>();
            foreach (object target in _targets)
                if (_owners.Contains(target.GetType()) && !order.Contains(target.GetType())) order.Add(target.GetType());
            foreach (Type owner in _owners)
                if (!order.Contains(owner)) order.Add(owner);
            foreach (Type owner in order)
            {
                // 지금 넘긴 곳 (없으면 null)
                _live.TryGetValue(owner, out object target);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(target != null || !_playing ? owner.Name : owner.Name + " · 지금 화면 아님", EditorStyles.boldLabel);
                if (target != null)
                    foreach (StatusEntry status in _statuses)
                        if (status.Method.DeclaringType == owner) EditorGUILayout.LabelField(status.Label, ReadStatus(status, target));
                foreach (ButtonEntry button in _buttons)
                    if (button.Method.DeclaringType == owner) DrawButton(button, 0f, string.Join(", ", button.Info.Items));
                EditorGUILayout.EndVertical();
            }
        }
        #endregion

        #region DrawButton - 버튼 한 줄 (Play 중·안 막음·지금 넘긴 곳이 있을 때만 켜짐) + 옆 글. 누르면 Press
        /// <param name="button">버튼</param>
        /// <param name="indent">왼쪽 띄움 (px)</param>
        /// <param name="note">옆 글 (선언한 곳 또는 쓰는 번호)</param>
        void DrawButton(ButtonEntry button, float indent, string note)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            using (new EditorGUI.DisabledScope(!_playing || _busy || !_live.ContainsKey(button.Method.DeclaringType)))
            {
                // 버튼 글과 알림 글 (선언한 메서드)
                var content = new GUIContent(button.Info.Label, button.Method.DeclaringType?.Name + "." + button.Method.Name);
                if (GUILayout.Button(content, GUILayout.ExpandWidth(false))) Press(button);
            }
            GUILayout.Label(note, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region OwnerNote - 항목별 버튼 옆 글: 선언한 형식 (Play 중 지금 화면이 아니면 표시)
        /// <param name="button">버튼</param>
        string OwnerNote(ButtonEntry button)
        {
            // 선언한 형식 이름
            string owner = button.Method.DeclaringType?.Name;
            return _playing && !_live.ContainsKey(button.Method.DeclaringType) ? owner + " · 지금 화면 아님" : owner;
        }
        #endregion

        #region Press - 지금 넘긴 곳에서 버튼 메서드를 부르고 돌려준 글(없으면 버튼 글)을 한 일에. 에러는 콘솔과 한 일에
        /// <param name="button">누른 버튼</param>
        void Press(ButtonEntry button)
        {
            if (!_live.TryGetValue(button.Method.DeclaringType, out object target)) return;
            try
            {
                _log = button.Method.Invoke(target, null) as string ?? button.Info.Label;
            }
            catch (TargetInvocationException e)
            {
                // 버튼 메서드 안에서 난 에러
                Exception inner = e.InnerException ?? e;
                Debug.LogException(inner);
                _log = "에러: " + inner.Message;
            }
        }
        #endregion

        #region IsDone - 확인 표시 (EditorPrefs. 없으면 plan 문서 ✅)
        /// <param name="plan">plan 이름</param>
        /// <param name="item">항목</param>
        static bool IsDone(string plan, ItemEntry item) => EditorPrefs.GetInt(DoneKey(plan, item.Id), item.DoneInDoc ? 1 : 0) == 1;
        #endregion

        #region DoneKey - 확인 표시 EditorPrefs 키
        /// <param name="plan">plan 이름</param>
        /// <param name="id">항목 번호</param>
        static string DoneKey(string plan, string id) => DonePrefsPrefix + plan + "." + id;
        #endregion
    }
}
