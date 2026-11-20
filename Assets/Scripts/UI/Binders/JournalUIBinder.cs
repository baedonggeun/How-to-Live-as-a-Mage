using UnityEngine;

namespace Mage
{
    /// <summary>임무 일지 탭 ← 이번 판 현재 목표. 탭이 켜질 때마다 채움 (목표가 없으면 빈 글)</summary>
    public sealed class JournalUIBinder : MonoBehaviour
    {
        /// <summary>메뉴 창 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        #region OnEnable - 현재 목표의 이름·설명 키를 넣음. 목표가 없으면 빈 글만
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] JournalUIBinder에 GameMenuView 연결 안 됨", this);
                return;
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
