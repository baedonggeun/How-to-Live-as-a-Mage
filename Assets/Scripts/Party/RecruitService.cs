using System;
using System.Collections.Generic;

namespace Mage
{
    /// <summary>용병 파티 캡슐의 전초기지 쪽 (MonoBehaviour 아님. HubState가 만들고 이음): 파티 판 데이터를 받아 영입 후보를 처음 한 번 뽑고, 후보 영입(자리가 있을 때)·파티원 내보내기를 맡음. 목록은 복사본으로만 내줌</summary>
    public sealed class RecruitService
    {
        /// <summary>파티에 넣을 수 있는 용병 수 (플레이어 + 2명). 첫 출발에도 이만큼 필요</summary>
        public const int MaxMembers = 2;

        /// <summary>파티 판 데이터</summary>
        readonly PartyEntry _data;

        /// <summary>파티 용병 수</summary>
        public int MemberCount => _data.Members.Count;

        /// <summary>영입 후보 (복사본)</summary>
        public MercenaryEntry[] Candidates => _data.Candidates.ToArray();

        /// <summary>파티 용병 (복사본)</summary>
        public MercenaryEntry[] Members => _data.Members.ToArray();

        #region RecruitService - 파티 판 데이터를 받음
        /// <param name="data">파티 판 데이터</param>
        public RecruitService(PartyEntry data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }
        #endregion

        #region EnsureCandidates - 아직 안 뽑았으면 직업 목록으로 영입 후보를 뽑음 (처음 한 번)
        /// <param name="jobs">직업 목록</param>
        public void EnsureCandidates(IReadOnlyList<JobSO> jobs)
        {
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));
            if (_data.CandidatesDrawn) return;
            _data.Candidates.AddRange(RecruitPool.Draw(jobs));
            _data.CandidatesDrawn = true;
        }
        #endregion

        #region Hire - 후보를 파티로 옮김 (자리가 있을 때). 옮겼으면 true
        /// <param name="entry">영입할 후보</param>
        public bool Hire(MercenaryEntry entry)
        {
            if (_data.Members.Count >= MaxMembers || !_data.Candidates.Remove(entry)) return false;
            _data.Members.Add(entry);
            return true;
        }
        #endregion

        #region Dismiss - 파티원을 후보로 되돌림. 되돌렸으면 true
        /// <param name="entry">내보낼 파티원</param>
        public bool Dismiss(MercenaryEntry entry)
        {
            if (!_data.Members.Remove(entry)) return false;
            _data.Candidates.Add(entry);
            return true;
        }
        #endregion
    }
}
