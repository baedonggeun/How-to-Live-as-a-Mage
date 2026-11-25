using System;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>임무 일지 탭 ← 상태가 넘긴 출처(지금 목표·받은 의뢰·의뢰 줄 날 수). 탭이 켜질 때마다 출처에서 받아 채움 (출처나 목표가 없으면 빈 글, 의뢰가 없으면 의뢰 줄 숨김)</summary>
    public sealed class JournalUIBinder : MonoBehaviour
    {
        /// <summary>의뢰 줄 키 ({0} 의뢰 이름, {1} 의뢰 기간 일)</summary>
        const string QuestKey = "menu.journal.quest";

        /// <summary>의뢰 줄 키, 기간을 세는 중 ({0} 의뢰 이름, {1} 남은 날)</summary>
        const string QuestLeftKey = "menu.journal.quest_left";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference MenuTable = "Menu";

        /// <summary>메뉴 창 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        /// <summary>탭에 보일 것을 만드는 출처 (상태가 Bind로 넘김. 없으면 null → 빈 일지)</summary>
        Func<JournalSpec> _source;

        #region Bind - 탭에 보일 것을 만드는 출처를 받음 (전초기지·숲이 들어올 때 UIManager를 거쳐)
        /// <param name="source">지금 목표·받은 의뢰·날 수를 만드는 함수</param>
        public void Bind(Func<JournalSpec> source) => _source = source ?? throw new ArgumentNullException(nameof(source));
        #endregion

        #region Unbind - 출처를 놓음 (나갈 때. 다음에 열면 빈 일지)
        public void Unbind() => _source = null;
        #endregion

        #region OnEnable - 출처에서 받아 의뢰 줄을 채우고, 현재 목표의 이름·설명 키를 넣음. 출처나 목표가 없으면 빈 글만
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] JournalUIBinder에 GameMenuView 연결 안 됨", this);
                return;
            }
            // 탭에 보일 것 (출처가 없으면 빈 것)
            JournalSpec spec = _source != null ? _source() : default;
            // 받은 의뢰 (없으면 null)
            QuestSO quest = spec.Quest;
            _view.QuestText.gameObject.SetActive(quest != null);
            if (quest != null)
            {
                _view.QuestText.StringReference.SetReference(MenuTable, spec.Counting ? QuestLeftKey : QuestKey);
                _view.QuestText.StringReference.Arguments = new object[] { quest.Title.GetLocalizedString(), spec.Days };
                _view.QuestText.RefreshString();
            }
            // 현재 목표 (없으면 null)
            ObjectiveSO objective = spec.Objective;
            // 목표가 있는지
            bool has = objective != null;
            _view.ObjectiveTitle.gameObject.SetActive(has);
            _view.ObjectiveDescription.gameObject.SetActive(has);
            _view.JournalEmpty.SetActive(!has);
            if (!has) return;
            _view.ObjectiveTitle.StringReference.SetReference(objective.Title.TableReference, objective.Title.TableEntryReference);
            _view.ObjectiveDescription.StringReference.SetReference(objective.Description.TableReference, objective.Description.TableEntryReference);
        }
        #endregion
    }
}
