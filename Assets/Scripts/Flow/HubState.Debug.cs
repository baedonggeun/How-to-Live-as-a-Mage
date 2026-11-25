#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>전초기지 상태 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 디버그 패널(DebugWindow)에 붙일 곳(이 상태·코덱스 캡슐)을 넘기고, 여러 캡슐·판 데이터에 걸치는 조작(금화·첫 도착 연출·방·용병·식량·첫 출발·의뢰·소탕)을 캡슐 진입점으로 만든 뒤 화면 글자·목표를 맞춤. 어디서든 정문 경비병과 말하기 (출발 검사·출발 경로 그대로)</summary>
    public sealed partial class HubState
    {
        /// <summary>조작을 막을지 (씬 준비 전·첫 도착 연출 중)</summary>
        public override bool DebugBusy => _view == null || _arriving;

        #region DebugTargets - 조작을 붙일 곳: 기본(이 상태·튜토리얼 담당), 코덱스 캡슐 (씬 준비 전엔 기본만)
        public override IEnumerable<object> DebugTargets()
        {
            foreach (object target in base.DebugTargets()) yield return target;
            if (_view == null) yield break;
            yield return _codex;
        }
        #endregion

        #region DebugGold - 금화 (넣으면 0 이상으로 자르고 금화·식량 표시 갱신)
        [DebugControl("판", "금화")]
        int DebugGold
        {
            get => Game.Run.Gold;
            set
            {
                Game.Run.Gold = Mathf.Max(0, value);
                RefreshMoney();
            }
        }
        #endregion

        #region DebugArrived - 첫 도착 연출(역마차)을 봤는지 (끄면 다음 전초기지 진입 때 연출부터)
        [DebugControl("판", "첫 도착 연출 봄")]
        bool DebugArrived
        {
            get => Game.Run.HasArrived;
            set => Game.Run.HasArrived = value;
        }
        #endregion

        #region DebugRoom - 여관 방을 잡았는지 (켜면 여관 캡슐로 방 잡기, 끄면 방 놓기. 금화는 안 씀). 목표 갱신
        [DebugControl("전초기지", "여관 방 잡음")]
        bool DebugRoom
        {
            get => _inn.HasRoom;
            set
            {
                if (value == _inn.HasRoom) return;
                if (value) _inn.Rent();
                else _inn.DebugCancel();
                UpdateObjective(true);
            }
        }
        #endregion

        #region DebugMembers - 파티 용병 수 (0~최대. 영입 캡슐로 후보를 뽑고 앞에서부터 영입, 줄이면 뒤에서부터 내보냄). 파티 수·목표 갱신
        [DebugControl("전초기지", "용병 수")]
        int DebugMembers
        {
            get => _recruit.MemberCount;
            set
            {
                // 맞출 용병 수
                int target = Mathf.Clamp(value, 0, RecruitService.MaxMembers);
                _recruit.EnsureCandidates(DataManager.Instance.Jobs);
                foreach (MercenaryEntry entry in _recruit.Candidates)
                    if (_recruit.MemberCount < target) _recruit.Hire(entry);
                // 지금 파티 (뒤에서부터 내보냄)
                MercenaryEntry[] members = _recruit.Members;
                for (int i = members.Length - 1; i >= 0 && _recruit.MemberCount > target; i--) _recruit.Dismiss(members[i]);
                RefreshParty();
                UpdateObjective(true);
            }
        }
        #endregion

        #region DebugRations - 원정 식량 (인분, 금화는 안 씀). 금화·식량 표시 갱신
        [DebugControl("전초기지", "식량 (인분)")]
        int DebugRations
        {
            get => _expedition.Rations;
            set
            {
                _expedition.DebugSetRations(value);
                RefreshMoney();
            }
        }
        #endregion

        #region DebugDeparted - 첫 출발을 했는지 (켜면 경비병이 모자란 걸 안 따짐). 목표 갱신
        [DebugControl("전초기지", "첫 출발함")]
        bool DebugDeparted
        {
            get => _expedition.HasDeparted;
            set
            {
                if (value == _expedition.HasDeparted) return;
                _expedition.DebugSetDeparted(value);
                UpdateObjective(true);
            }
        }
        #endregion

        #region DebugTalkGuard - 정문 경비병과 말하기 (모자란 게 있으면 막는 말, 아니면 출발 선택지)
        [DebugControl("전초기지", "경비병과 말하기")]
        string DebugTalkGuard()
        {
            if (DebugBusy) return "지금은 안 됨";
            TalkGuard();
            return "경비병 대화 (출발을 고르면 숲으로)";
        }
        #endregion

        #region DebugQuestTaken - 게시판 첫 의뢰를 받았는지 (켜면 의뢰 캡슐로 받기, 끄면 의뢰를 비움). 목표 갱신
        [DebugControl("의뢰", "첫 의뢰 받음")]
        bool DebugQuestTaken
        {
            get => _quest.HasQuest;
            set
            {
                if (value == _quest.HasQuest) return;
                if (value)
                {
                    // 게시판 의뢰 목록
                    IReadOnlyList<QuestSO> quests = DataManager.Instance.Quests;
                    if (quests.Count == 0 || quests[0] == null) return;
                    _quest.Take(quests[0]);
                }
                else _quest.DebugDrop();
                UpdateObjective(true);
            }
        }
        #endregion

        #region DebugQuestCleared - 받은 의뢰의 소탕을 끝냈는지 (의뢰가 없으면 안 바뀜. 숲에서 돌아오면 의뢰를 끝냄). 목표 갱신
        [DebugControl("의뢰", "소탕 끝냄")]
        bool DebugQuestCleared
        {
            get => _quest.DebugCleared;
            set
            {
                _quest.DebugSetCleared(value);
                UpdateObjective(true);
            }
        }
        #endregion
    }
}
#endif
