using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mage
{
    /// <summary>정적 데이터 목록 담당 (룬·주문·몬스터·의뢰 SO). 지금은 룬만</summary>
    public sealed class DataManager : MonoSingleton<DataManager>
    {
        /// <summary>모든 룬 (Boot에서 연결. 이 순서대로 생성 화면 카드·코덱스 룬 주문 장이 나옴)</summary>
        [SerializeField, FormerlySerializedAs("_elements")] RuneSO[] _runes;

        /// <summary>모든 룬 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<RuneSO> Runes => _runes;

        /// <summary>로딩창 팁 전부 (Boot에서 연결)</summary>
        [SerializeField] TipSO[] _tips;

        /// <summary>로딩창 팁 전부 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<TipSO> Tips => _tips;

        /// <summary>튜토리얼 전부 (Boot에서 연결. 이 순서대로 튜토리얼 탭 목록이 나옴)</summary>
        [SerializeField] TutorialSO[] _tutorials;

        /// <summary>튜토리얼 전부 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<TutorialSO> Tutorials => _tutorials;

        /// <summary>용병 직업 전부 (Boot에서 연결. 영입 후보가 여기서 뽑힘)</summary>
        [SerializeField] JobSO[] _jobs;

        /// <summary>용병 직업 전부 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<JobSO> Jobs => _jobs;

        /// <summary>의뢰 전부 (Boot에서 연결. 첫 칸이 첫 의뢰)</summary>
        [SerializeField] QuestSO[] _quests;

        /// <summary>의뢰 전부 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<QuestSO> Quests => _quests;

        /// <summary>도감 주문 전부 (Boot에서 연결. 이 순서가 인식기 비교 순서)</summary>
        [SerializeField] SpellSO[] _spells;

        /// <summary>도감 주문 전부 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<SpellSO> Spells => _spells;

        /// <summary>시전 실패 사운드 (숲·책상 연습 공용. Boot에서 연결. 비면 무음)</summary>
        [SerializeField] SoundDataSO _castFailSound;

        /// <summary>시전 실패 사운드 (없으면 null)</summary>
        public SoundDataSO CastFailSound => _castFailSound;

        #region InitSingleton - 팁·튜토리얼·직업·의뢰·주문 목록 검사, 룬 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침, 그림 획 없음·점 2개 미만 획
        protected override void InitSingleton()
        {
            ValidateTips();
            ValidateTutorials();
            ValidateJobs();
            ValidateQuests();
            ValidateSpells();
            if (_runes == null || _runes.Length == 0)
            {
                Debug.LogError("[Data] 룬 목록 비어 있음");
                _runes = Array.Empty<RuneSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _runes.Length; i++)
            {
                // 검사할 룬
                RuneSO rune = _runes[i];
                if (rune == null)
                    Debug.LogError($"[Data] 룬 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(rune.Id))
                    Debug.LogError($"[Data] {rune.name}의 id 비어 있음");
                else if (!ids.Add(rune.Id))
                    Debug.LogError($"[Data] 룬 id 겹침: {rune.Id} ({rune.name})");
                else if (rune.Strokes.Count == 0)
                    Debug.LogError($"[Data] {rune.name}의 그림 획 없음");
                else
                    foreach (StrokeEntry stroke in rune.Strokes)
                        if (stroke.Points.Count < 2)
                        {
                            Debug.LogError($"[Data] {rune.name}에 점 2개 미만 획");
                            break;
                        }
            }
        }
        #endregion

        #region ValidateTips - 팁 목록 검사: 비었으면 경고(로딩창에 팁 없이 나옴), 빈 칸, 제목·본문 키 빔
        void ValidateTips()
        {
            if (_tips == null || _tips.Length == 0)
            {
                Debug.LogWarning("[Data] 팁 목록 비어 있음. 로딩창에 팁 없이 나옴");
                _tips = Array.Empty<TipSO>();
                return;
            }

            for (int i = 0; i < _tips.Length; i++)
            {
                // 검사할 팁
                TipSO tip = _tips[i];
                if (tip == null)
                    Debug.LogError($"[Data] 팁 {i}번 칸 비어 있음");
                else if (tip.Title.IsEmpty || tip.Body.IsEmpty)
                    Debug.LogError($"[Data] {tip.name}의 제목·본문 키 비어 있음");
            }
        }
        #endregion

        #region FindTutorial - id로 튜토리얼 찾기 (대소문자 구분). 없으면 null
        /// <param name="id">튜토리얼 id</param>
        public TutorialSO FindTutorial(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (TutorialSO tutorial in _tutorials)
                if (tutorial != null && string.Equals(tutorial.Id, id, StringComparison.Ordinal)) return tutorial;
            return null;
        }
        #endregion

        #region FindRune - id로 룬 찾기 (대소문자 구분). 없으면 null
        /// <param name="id">룬 id</param>
        public RuneSO FindRune(string id)
        {
            if (string.IsNullOrEmpty(id) || _runes == null) return null;
            foreach (RuneSO rune in _runes)
                if (rune != null && string.Equals(rune.Id, id, StringComparison.Ordinal)) return rune;
            return null;
        }
        #endregion

        #region ValidateJobs - 직업 목록 검사: 비었으면 에러(영입 후보가 안 나옴), 빈 칸, 빈 id, id 겹침, 이름·설명 키 빔
        void ValidateJobs()
        {
            if (_jobs == null || _jobs.Length == 0)
            {
                Debug.LogError("[Data] 직업 목록 비어 있음. 영입 후보가 안 나옴");
                _jobs = Array.Empty<JobSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _jobs.Length; i++)
            {
                // 검사할 직업
                JobSO job = _jobs[i];
                if (job == null)
                    Debug.LogError($"[Data] 직업 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(job.Id))
                    Debug.LogError($"[Data] {job.name}의 id 비어 있음");
                else if (!ids.Add(job.Id))
                    Debug.LogError($"[Data] 직업 id 겹침: {job.Id} ({job.name})");
                else if (job.Name.IsEmpty || job.Description.IsEmpty)
                    Debug.LogError($"[Data] {job.name}의 이름·설명 키 비어 있음");
            }
        }
        #endregion

        #region ValidateQuests - 의뢰 목록 검사: 비었으면 에러(게시판이 빔), 빈 칸, 빈 id, id 겹침, 이름·설명 키 빔
        void ValidateQuests()
        {
            if (_quests == null || _quests.Length == 0)
            {
                Debug.LogError("[Data] 의뢰 목록 비어 있음. 게시판이 빔");
                _quests = Array.Empty<QuestSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _quests.Length; i++)
            {
                // 검사할 의뢰
                QuestSO quest = _quests[i];
                if (quest == null)
                    Debug.LogError($"[Data] 의뢰 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(quest.Id))
                    Debug.LogError($"[Data] {quest.name}의 id 비어 있음");
                else if (!ids.Add(quest.Id))
                    Debug.LogError($"[Data] 의뢰 id 겹침: {quest.Id} ({quest.name})");
                else if (quest.Title.IsEmpty || quest.Description.IsEmpty)
                    Debug.LogError($"[Data] {quest.name}의 이름·설명 키 비어 있음");
            }
        }
        #endregion

        #region ValidateSpells - 주문 목록 검사: 비었으면 경고(책상 연습에 비교할 그림이 없음), 빈 칸, 빈 id, id 겹침, 이름·설명 키 빔, 그림 부분 없음, 부분 검사
        void ValidateSpells()
        {
            if (_spells == null || _spells.Length == 0)
            {
                Debug.LogWarning("[Data] 주문 목록 비어 있음. 책상 연습에 비교할 그림이 없음");
                _spells = Array.Empty<SpellSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _spells.Length; i++)
            {
                // 검사할 주문
                SpellSO spell = _spells[i];
                if (spell == null)
                    Debug.LogError($"[Data] 주문 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(spell.Id))
                    Debug.LogError($"[Data] {spell.name}의 id 비어 있음");
                else if (!ids.Add(spell.Id))
                    Debug.LogError($"[Data] 주문 id 겹침: {spell.Id} ({spell.name})");
                else if (spell.Name.IsEmpty || spell.Description.IsEmpty)
                    Debug.LogError($"[Data] {spell.name}의 이름·설명 키 비어 있음");
                else if (spell.Parts.Count == 0)
                    Debug.LogError($"[Data] {spell.name}의 그림 부분 없음");
                else
                    ValidateParts(spell);
            }
        }
        #endregion

        #region ValidateParts - 주문 그림 부분 검사: 룬 빔·룬 목록에 없음·같은 룬 두 번, 획 없음·점 2개 미만 획
        /// <param name="spell">검사할 주문</param>
        void ValidateParts(SpellSO spell)
        {
            // 이 주문에서 이미 본 룬
            var seen = new HashSet<RuneSO>();
            foreach (SpellPartEntry part in spell.Parts)
            {
                if (part.Rune == null)
                {
                    Debug.LogError($"[Data] {spell.name}의 그림 부분 룬 비어 있음");
                    continue;
                }
                if (_runes == null || Array.IndexOf(_runes, part.Rune) < 0)
                    Debug.LogError($"[Data] {spell.name}의 {part.Rune.name}가 룬 목록에 없음");
                if (!seen.Add(part.Rune))
                    Debug.LogError($"[Data] {spell.name}에 {part.Rune.name} 부분이 둘 이상");
                if (part.Strokes.Count == 0)
                    Debug.LogError($"[Data] {spell.name}의 {part.Rune.name} 부분에 획 없음");
                foreach (StrokeEntry stroke in part.Strokes)
                    if (stroke.Points.Count < 2)
                    {
                        Debug.LogError($"[Data] {spell.name}의 {part.Rune.name} 부분에 점 2개 미만 획");
                        break;
                    }
            }
        }
        #endregion

        #region ValidateTutorials - 튜토리얼 목록 검사: 비었으면 경고, 빈 칸, 빈 id, id 겹침, 제목·설명 키 빔
        void ValidateTutorials()
        {
            if (_tutorials == null || _tutorials.Length == 0)
            {
                Debug.LogWarning("[Data] 튜토리얼 목록 비어 있음. 조작 안내·튜토리얼 탭이 안 나옴");
                _tutorials = Array.Empty<TutorialSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _tutorials.Length; i++)
            {
                // 검사할 튜토리얼
                TutorialSO tutorial = _tutorials[i];
                if (tutorial == null)
                    Debug.LogError($"[Data] 튜토리얼 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(tutorial.Id))
                    Debug.LogError($"[Data] {tutorial.name}의 id 비어 있음");
                else if (!ids.Add(tutorial.Id))
                    Debug.LogError($"[Data] 튜토리얼 id 겹침: {tutorial.Id} ({tutorial.name})");
                else if (tutorial.Title.IsEmpty || tutorial.Body.IsEmpty)
                    Debug.LogError($"[Data] {tutorial.name}의 제목·설명 키 비어 있음");
            }
        }
        #endregion
    }
}
