using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>임무 일지 탭 ← 이번 판 현재 목표·받은 의뢰. 탭이 켜질 때마다 채움 (목표가 없으면 빈 글, 의뢰가 없으면 의뢰 줄 숨김)</summary>
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

        #region OnEnable - 받은 의뢰 줄을 채우고, 현재 목표의 이름·설명 키를 넣음. 목표가 없으면 빈 글만
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] JournalUIBinder에 GameMenuView 연결 안 됨", this);
                return;
            }
            // 이번 판 데이터 (없으면 null)
            RunEntry run = GameManager.Instance.Run;
            // 받은 의뢰 (판이나 의뢰가 없으면 null)
            QuestSO quest = run?.Quest.Current;
            _view.QuestText.gameObject.SetActive(quest != null);
            if (quest != null)
            {
                // 기간을 세는 중인지 (처음 출발 뒤)
                bool counting = run.Quest.Deadline > 0f;
                // 보일 날 수 (세는 중이면 남은 날 올림, 아니면 의뢰 기간)
                int days = counting ? Mathf.Max(0, Mathf.CeilToInt((run.Quest.Deadline - run.Clock.PlaySeconds) / ClockEntry.DaySeconds)) : quest.PeriodDays;
                _view.QuestText.StringReference.SetReference(MenuTable, counting ? QuestLeftKey : QuestKey);
                _view.QuestText.StringReference.Arguments = new object[] { quest.Title.GetLocalizedString(), days };
                _view.QuestText.RefreshString();
            }
            // 이번 판 현재 목표 (판이나 목표가 없으면 null)
            ObjectiveSO objective = GameManager.Instance.Run?.CurrentObjective;
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
