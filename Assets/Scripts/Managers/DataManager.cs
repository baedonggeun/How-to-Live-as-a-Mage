using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>정적 데이터 목록 담당 (요소·주문·몬스터·의뢰 SO). 지금은 요소만</summary>
    public sealed class DataManager : MonoSingleton<DataManager>
    {
        /// <summary>모든 요소 (Boot에서 연결. 이 순서대로 생성 화면 카드가 나옴)</summary>
        [SerializeField] ElementSO[] _elements;

        /// <summary>모든 요소 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<ElementSO> Elements => _elements;

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

        #region InitSingleton - 팁·튜토리얼·직업·의뢰 목록 검사, 요소 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침
        protected override void InitSingleton()
        {
            ValidateTips();
            ValidateTutorials();
            ValidateJobs();
            ValidateQuests();
            if (_elements == null || _elements.Length == 0)
            {
                Debug.LogError("[Data] 요소 목록 비어 있음");
                _elements = Array.Empty<ElementSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _elements.Length; i++)
            {
                // 검사할 요소
                ElementSO element = _elements[i];
                if (element == null)
                    Debug.LogError($"[Data] 요소 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(element.Id))
                    Debug.LogError($"[Data] {element.name}의 id 비어 있음");
                else if (!ids.Add(element.Id))
                    Debug.LogError($"[Data] 요소 id 겹침: {element.Id} ({element.name})");
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
