using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>의뢰 하나 (길드 임무 게시판에 붙음). 지금은 첫 의뢰 하나</summary>
    [CreateAssetMenu(fileName = "Quest", menuName = "Mage/Quest")]
    public sealed class QuestSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (forest_01 …)</summary>
        [SerializeField] string _id;

        /// <summary>의뢰 이름 (Quest 테이블 quest.{id}.title)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>의뢰 설명 (Quest 테이블 quest.{id}.desc)</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>원정 기간 (일). 식량이 (플레이어 + 용병) × 기간 인분만큼 듦</summary>
        [SerializeField, Min(1)] int _days = 3;

        /// <summary>소탕 대상 몬스터 (숲에서 이 몬스터를 다 쓰러뜨리면 의뢰 목표 달성). 비면 소탕 없음</summary>
        [SerializeField] MonsterSO _target;

        /// <summary>의뢰 기간 (일). 0이면 원정 기간(_days)과 같음 (튜토리얼 의뢰)</summary>
        [SerializeField, Min(0)] int _periodDays;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>의뢰 이름</summary>
        public LocalizedString Title => _title;

        /// <summary>의뢰 설명</summary>
        public LocalizedString Description => _description;

        /// <summary>원정 기간 (일)</summary>
        public int Days => _days;

        /// <summary>소탕 대상 몬스터 (없으면 null)</summary>
        public MonsterSO Target => _target;

        /// <summary>의뢰 기간 (일, 0이면 원정 기간)</summary>
        public int PeriodDays => _periodDays > 0 ? _periodDays : _days;
    }
}
