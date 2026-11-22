using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Mage
{
    /// <summary>영입 창 용병 한 줄 참조 (RecruitRow 프리팹 루트): 이름·직업·레벨·전투·버튼</summary>
    public sealed class RecruitRowView : MonoBehaviour
    {
        /// <summary>이름 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _nameText;

        /// <summary>직업 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _jobText;

        /// <summary>직업 글자에 마우스를 올리면 무기·역할 툴팁 (인스펙터에서만 연결)</summary>
        [SerializeField] TooltipHandler _jobTooltip;

        /// <summary>레벨 글자 (Hub/hub.recruit.level, {0} 레벨. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _levelText;

        /// <summary>전투 글자 (Hub/hub.recruit.combat, {0} 전투 수치. 인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _combatText;

        /// <summary>영입·내보내기 버튼 (인스펙터에서만 연결)</summary>
        [SerializeField] Button _button;

        /// <summary>버튼 글자 (인스펙터에서만 연결)</summary>
        [SerializeField] LocalizeStringEvent _buttonLabel;

        /// <summary>이름 글자</summary>
        public LocalizeStringEvent NameText => _nameText;

        /// <summary>직업 글자</summary>
        public LocalizeStringEvent JobText => _jobText;

        /// <summary>직업 툴팁</summary>
        public TooltipHandler JobTooltip => _jobTooltip;

        /// <summary>레벨 글자</summary>
        public LocalizeStringEvent LevelText => _levelText;

        /// <summary>전투 글자</summary>
        public LocalizeStringEvent CombatText => _combatText;

        /// <summary>영입·내보내기 버튼</summary>
        public Button Button => _button;

        /// <summary>버튼 글자</summary>
        public LocalizeStringEvent ButtonLabel => _buttonLabel;
    }
}
