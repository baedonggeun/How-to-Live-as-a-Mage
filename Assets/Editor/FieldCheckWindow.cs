using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 확인 패널 (에디터 전용 도구 창, Tools > Mage > 숲 확인): Play 중 숲에서만 켜짐. 위에 판 시계·의뢰 기한·숲 적 처치 수·던전·위치를 실시간으로 보이고, plan 확인 목록 번호마다 그 상황을 바로 만드는 버튼(FieldState.Debug 진입점)과 보일 것을 둠. 번호 칩으로 확인한 항목을 체크 (EditorPrefs에 남음)</summary>
    public sealed class FieldCheckWindow : EditorWindow
    {
        /// <summary>확인한 번호를 남기는 EditorPrefs 키 (쉼표로 이음)</summary>
        const string DonePrefsKey = "Mage.FieldCheck.Done";

        /// <summary>처음 켤 때 확인한 번호 (2026-11-25 현황: 1은 날짜 표시만이라 뺌)</summary>
        const string DoneDefault = "2,4,5,9,10";

        /// <summary>확인 목록 항목 수 (docs/plan-forest-dungeon.md)</summary>
        const int CheckCount = 12;

        /// <summary>한 줄: 확인 번호들, 제목, 버튼(글, 할 일), 보일 것</summary>
        sealed class Row
        {
            public int[] Checks;
            public string Title;
            public (string Label, Func<FieldState, string> Act)[] Buttons;
            public string Expect;
        }

        /// <summary>줄 목록 (plan 확인 목록 번호 순)</summary>
        static readonly Row[] Rows =
        {
            new()
            {
                Checks = new[] { 1 }, Title = "날짜·시계",
                Buttons = new (string, Func<FieldState, string>)[]
                {
                    ("다음 날 직전으로", s => { s.CheckNextDayShortly(); return "시계를 다음 날 3초 전으로 옮김"; }),
                },
                Expect = "HUD 날짜가 3초 뒤 +1. Esc 메뉴를 열면 위 시계가 멈춤",
            },
            new()
            {
                Checks = new[] { 2, 8 }, Title = "의뢰 기간",
                Buttons = new (string, Func<FieldState, string>)[]
                {
                    ("기간 5초 남기기", s => s.CheckExpireSoon() ? "의뢰 마감을 5초 뒤로" : "진행 중인 의뢰 없음 (없거나 이미 끝냄)"),
                },
                Expect = "\"의뢰 기간이 지났다\" 토스트, 임무 일지 의뢰 줄 사라짐. 8번: 리셋 뒤 일어난 고블린이 소탕 대상인지 임무 일지로",
            },
            new()
            {
                Checks = new[] { 3 }, Title = "떨어진 금화",
                Buttons = new (string, Func<FieldState, string>)[]
                {
                    ("내 앞에 금화 100", s => { s.CheckDropGold(); return "마법사 앞 6m에 금화 100을 떨굼 (가진 금화는 그대로)"; }),
                    ("죽기", s => { s.CheckDie(); return "실제 죽음 경로: 금화 떨굼 → 용병 도망 → 여관 부활"; }),
                },
                Expect = "18m 안 고블린이 주워 동굴 입구로 가져감. 중간에 처치하면 그 자리에 [F] 떨어진 금화",
            },
            new()
            {
                Checks = new[] { 4, 11, 12 }, Title = "이동·추격",
                Buttons = new (string, Func<FieldState, string>)[]
                {
                    ("던전 입구 앞", s => { s.CheckMoveTo(FieldState.CheckSpot.DungeonFront); return "마법사·용병을 던전 입구 앞으로"; }),
                    ("던전 안", s => { s.CheckMoveTo(FieldState.CheckSpot.DungeonStart); return "마법사·용병을 던전 시작으로"; }),
                    ("보스방 앞", s => { s.CheckMoveTo(FieldState.CheckSpot.BossFront); return "마법사·용병을 보스방 앞으로"; }),
                    ("방A 위 숲", s => { s.CheckMoveTo(FieldState.CheckSpot.AboveRoomA); return "마법사·용병을 던전 바로 위 숲으로"; }),
                    ("근처 적 깨우기", s => string.Format(CultureInfo.InvariantCulture, "가까운 적 {0}마리가 마법사를 쫓기 시작", s.CheckWakeNearby())),
                },
                Expect = "방A 위 숲을 걸어도 던전 고블린이 안 깸. 쫓기는 채 입구로 들어가면 쫓던 적이 포기",
            },
            new()
            {
                Checks = new[] { 5, 6 }, Title = "보스·던전",
                Buttons = new (string, Func<FieldState, string>)[]
                {
                    ("보스 체력 절반", s => s.CheckBossHalf() ? "보스 체력을 절반으로 (보스방 앞에서 눌러야 쫓다 부하 3마리 소환)" : "살아 있는 보스 없음"),
                    ("보스 즉사", s => s.CheckBossKill() ? "보스 즉사 → \"고블린 동굴을 정리했다\" 토스트" : "살아 있는 보스 없음"),
                    ("나갔다 다시 들어오기", Reenter),
                },
                Expect = "절반에서 고블린 3마리. 클리어 뒤 다시 들어오면 새 던전, 보관함 빔",
            },
            new()
            {
                Checks = new[] { 7 }, Title = "숲 리셋 연출",
                Buttons = new (string, Func<FieldState, string>)[]
                {
                    ("숲 적 70% 쓰러뜨리기", s => string.Format(CultureInfo.InvariantCulture, "숲 적 {0}마리를 쓰러뜨림. 이제 나갔다 다시 들어오기", s.CheckClearForest70())),
                    ("나갔다 다시 들어오기", Reenter),
                },
                Expect = "들어오자마자 HUD 꺼지고 연출 약 6초, 시체가 일어남",
            },
            new()
            {
                Checks = new[] { 9, 10 }, Title = "버튼 없음",
                Buttons = Array.Empty<(string, Func<FieldState, string>)>(),
                Expect = "영어 전환 글자, 콘솔 에러·경고",
            },
        };

        /// <summary>확인한 번호</summary>
        readonly HashSet<int> _done = new();

        /// <summary>찾아 둔 GameManager (Play마다 다시 찾음)</summary>
        GameManager _game;

        /// <summary>마지막 버튼이 한 일</summary>
        string _log = "버튼을 눌러 봐. 여기에 하는 일이 나와.";

        /// <summary>스크롤 위치</summary>
        Vector2 _scroll;

        #region Open - 메뉴에서 창 열기
        [MenuItem("Tools/Mage/숲 확인")]
        static void Open() => GetWindow<FieldCheckWindow>("숲 확인");
        #endregion

        #region OnEnable - 확인한 번호를 읽음
        void OnEnable()
        {
            _done.Clear();
            foreach (string part in EditorPrefs.GetString(DonePrefsKey, DoneDefault).Split(','))
                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) _done.Add(n);
        }
        #endregion

        #region OnInspectorUpdate - Play 중이면 1초에 10번 다시 그림 (실시간 상태)
        void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }
        #endregion

        #region OnGUI - 머리줄(진행), 실시간 상태, 줄마다 번호 칩·제목·버튼·보일 것, 마지막 한 일
        void OnGUI()
        {
            // 지금 숲 상태 (Play 중 숲이 아니면 null)
            FieldState field = FindField();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("숲 확인 · Play 중 숲에서만 켜짐", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "확인 {0} / {1}", _done.Count, CheckCount));
            EditorGUILayout.EndHorizontal();
            DrawStatus(field);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (Row row in Rows) DrawRow(row, field);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("→ " + _log, MessageType.None);
        }
        #endregion

        #region FindField - Play 중이고 지금 상태가 숲이면 그 상태 (아니면 null)
        FieldState FindField()
        {
            if (!EditorApplication.isPlaying) return null;
            if (_game == null) _game = FindAnyObjectByType<GameManager>();
            return _game != null ? _game.State as FieldState : null;
        }
        #endregion

        #region DrawStatus - 시계·의뢰·숲 적 처치·던전·위치 (숲이 아니면 안내만)
        /// <param name="field">지금 숲 상태 (없으면 null)</param>
        void DrawStatus(FieldState field)
        {
            if (field == null || _game.Run == null)
            {
                EditorGUILayout.HelpBox("Play 중 숲에서만 켜짐 (Forest 씬에서 Play)", MessageType.Info);
                return;
            }
            // 이번 판 데이터
            RunEntry run = _game.Run;
            // 지금 판 시각 (초)
            float now = run.Clock.PlaySeconds;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Line("시계", string.Format(CultureInfo.InvariantCulture, "{0:F0}초 · {1}일째", now, run.Clock.Day));
            Line("의뢰", run.Quest.Current == null ? "없음"
                : run.Quest.Cleared ? "목표 끝냄"
                : run.Quest.Deadline <= 0f ? "기한 안 셈"
                : string.Format(CultureInfo.InvariantCulture, "마감까지 {0:F0}초", Mathf.Max(0f, run.Quest.Deadline - now)));
            Line("숲 적 처치", string.Format(CultureInfo.InvariantCulture, "{0} / {1} ({2} 이상이면 리셋)", field.CheckDeadCount, field.CheckTotal, field.CheckResetNeed));
            Line("던전", string.Format(CultureInfo.InvariantCulture, "{0}세대 · {1} · {2} · 보관 {3}", run.Dungeon.Generation, run.Dungeon.BossId, run.Dungeon.Cleared ? "클리어" : "미클리어", run.Dungeon.StashGold));
            Line("위치", field.CheckBusy ? field.CheckWhere + " (지금은 버튼 안 받음)" : field.CheckWhere);
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Line - 이름: 값 한 줄
        /// <param name="label">이름</param>
        /// <param name="value">값</param>
        static void Line(string label, string value) => EditorGUILayout.LabelField(label, value);
        #endregion

        #region DrawRow - 번호 칩(누르면 확인 토글)·제목, 버튼(숲일 때만), 보일 것
        /// <param name="row">줄</param>
        /// <param name="field">지금 숲 상태 (없으면 null)</param>
        void DrawRow(Row row, FieldState field)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            foreach (int n in row.Checks)
            {
                // 확인했는지
                bool done = _done.Contains(n);
                // 칩 색 (확인했으면 초록)
                Color before = GUI.backgroundColor;
                if (done) GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
                if (GUILayout.Button(n.ToString(CultureInfo.InvariantCulture), GUILayout.Width(30f))) ToggleDone(n);
                GUI.backgroundColor = before;
            }
            EditorGUILayout.LabelField(row.Title, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            if (row.Buttons.Length > 0)
            {
                using (new EditorGUI.DisabledScope(field == null || field.CheckBusy))
                {
                    EditorGUILayout.BeginHorizontal();
                    foreach ((string label, Func<FieldState, string> act) in row.Buttons)
                        if (GUILayout.Button(label)) _log = act(field);
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.LabelField("보일 것: " + row.Expect, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region ToggleDone - 확인 표시를 바꾸고 EditorPrefs에 남김
        /// <param name="n">확인 번호</param>
        void ToggleDone(int n)
        {
            if (!_done.Remove(n)) _done.Add(n);
            // 남길 번호 (작은 순)
            var list = new List<int>(_done);
            list.Sort();
            EditorPrefs.SetString(DonePrefsKey, string.Join(",", list));
        }
        #endregion

        #region Reenter - 나갔다 다시 들어오기 (로딩창에서 넘기기 버튼을 눌러야 걷힘)
        /// <param name="field">지금 숲 상태</param>
        static string Reenter(FieldState field)
        {
            field.CheckReenter();
            return "숲을 나갔다 다시 들어옴 (로딩창 넘기기). 클리어했으면 새 던전, 70% 이상이면 리셋 연출";
        }
        #endregion
    }
}
