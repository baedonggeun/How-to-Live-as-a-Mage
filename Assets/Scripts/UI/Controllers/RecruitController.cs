using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>영입 창: 후보·파티 목록을 줄로 만들고, 영입·내보내기를 누르면 상태가 준 할 일을 부른 뒤 목록을 다시 만듦. 목록 데이터는 상태가 넘김. 여닫기에 따른 일시정지는 UIManager</summary>
    public sealed class RecruitController : MonoBehaviour
    {
        /// <summary>영입 버튼 글자 키</summary>
        const string HireKey = "hub.recruit.hire";

        /// <summary>내보내기 버튼 글자 키</summary>
        const string DismissKey = "hub.recruit.dismiss";

        /// <summary>레벨 글자 키 ({0} 레벨)</summary>
        const string LevelKey = "hub.recruit.level";

        /// <summary>전투 글자 키 ({0} 전투 수치)</summary>
        const string CombatKey = "hub.recruit.combat";

        /// <summary>영입 창 문구 테이블</summary>
        static readonly TableReference Table = "Hub";

        /// <summary>같은 창의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] RecruitView _view;

        /// <summary>지금 만든 줄</summary>
        readonly List<RecruitRowView> _rows = new();

        /// <summary>영입 후보 (상태가 넘긴 판 데이터 목록)</summary>
        IReadOnlyList<MercenaryEntry> _candidates;

        /// <summary>지금 파티 (상태가 넘긴 판 데이터 목록)</summary>
        IReadOnlyList<MercenaryEntry> _party;

        /// <summary>파티에 넣을 수 있는 용병 수</summary>
        int _partyMax;

        /// <summary>영입을 누르면 할 일 (상태가 후보 → 파티로 옮김)</summary>
        Action<MercenaryEntry> _onHire;

        /// <summary>내보내기를 누르면 할 일 (상태가 파티 → 후보로 옮김)</summary>
        Action<MercenaryEntry> _onDismiss;

        /// <summary>같은 창의 참조</summary>
        public RecruitView View => _view;

        /// <summary>창이 떠 있는지</summary>
        public bool IsOpen => gameObject.activeSelf;

        #region Show - 목록과 할 일을 받아 창을 켜고 줄을 만듦
        /// <param name="candidates">영입 후보</param>
        /// <param name="party">지금 파티</param>
        /// <param name="partyMax">파티에 넣을 수 있는 용병 수</param>
        /// <param name="onHire">영입을 누르면 할 일</param>
        /// <param name="onDismiss">내보내기를 누르면 할 일</param>
        public void Show(IReadOnlyList<MercenaryEntry> candidates, IReadOnlyList<MercenaryEntry> party, int partyMax,
            Action<MercenaryEntry> onHire, Action<MercenaryEntry> onDismiss)
        {
            if (_view == null)
            {
                Debug.LogError("[Recruit] RecruitView 연결 안 됨", this);
                return;
            }
            _candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _partyMax = partyMax;
            _onHire = onHire ?? throw new ArgumentNullException(nameof(onHire));
            _onDismiss = onDismiss ?? throw new ArgumentNullException(nameof(onDismiss));
            gameObject.SetActive(true);
            Rebuild();
        }
        #endregion

        #region Hide - 창을 끄고 줄·할 일을 비움
        public void Hide()
        {
            ClearRows();
            _onHire = null;
            _onDismiss = null;
            gameObject.SetActive(false);
        }
        #endregion

        #region Rebuild - 줄을 지우고 후보(영입, 파티가 차면 못 누름)·파티(내보내기) 순서대로 다시 만듦
        void Rebuild()
        {
            ClearRows();
            // 파티에 자리가 남았는지
            bool hasRoom = _party.Count < _partyMax;
            foreach (MercenaryEntry candidate in _candidates)
            {
                // 누를 때 쓸 후보 (반복 변수를 붙잡지 않게)
                MercenaryEntry picked = candidate;
                AddRow(_view.CandidateList, candidate, HireKey, hasRoom, () => _onHire?.Invoke(picked));
            }
            foreach (MercenaryEntry member in _party)
            {
                // 누를 때 쓸 파티원
                MercenaryEntry picked = member;
                AddRow(_view.PartyList, member, DismissKey, true, () => _onDismiss?.Invoke(picked));
            }
        }
        #endregion

        #region AddRow - 용병 한 줄: 이름·직업(툴팁)·레벨·전투 글자와 버튼. 누르면 할 일 뒤 목록을 다시 만듦
        /// <param name="list">줄을 넣을 자리</param>
        /// <param name="entry">보일 용병</param>
        /// <param name="buttonKey">버튼 글자 키</param>
        /// <param name="interactable">버튼을 누를 수 있는지</param>
        /// <param name="onClick">버튼을 누르면 할 일</param>
        void AddRow(RectTransform list, MercenaryEntry entry, string buttonKey, bool interactable, Action onClick)
        {
            // 이 용병의 줄
            RecruitRowView row = Instantiate(_view.RowPrefab, list);
            row.NameText.StringReference.SetReference(entry.Name.TableReference, entry.Name.TableEntryReference);
            row.JobText.StringReference.SetReference(entry.Job.Name.TableReference, entry.Job.Name.TableEntryReference);
            row.JobTooltip.Set(entry.Job.Name, entry.Job.Description);
            SetArgsText(row.LevelText, LevelKey, entry.Level);
            SetArgsText(row.CombatText, CombatKey, entry.Combat);
            row.ButtonLabel.StringReference.SetReference(Table, buttonKey);
            row.Button.interactable = interactable;
            row.Button.onClick.AddListener(() =>
            {
                onClick();
                if (IsOpen) Rebuild();
            });
            _rows.Add(row);
        }
        #endregion

        #region SetArgsText - 이 창 테이블의 키와 {0} 값을 넣고 글자 갱신
        /// <param name="text">넣을 글자</param>
        /// <param name="key">문구 키</param>
        /// <param name="value">{0} 자리 값</param>
        static void SetArgsText(UnityEngine.Localization.Components.LocalizeStringEvent text, string key, int value)
        {
            // 글자의 String Reference
            var reference = text.StringReference;
            reference.SetReference(Table, key);
            reference.Arguments = new object[] { value };
            text.RefreshString();
        }
        #endregion

        #region ClearRows - 만든 줄을 지움 (누른 버튼의 onClick 안에서 불려도 프레임 끝에 지워짐)
        void ClearRows()
        {
            foreach (RecruitRowView row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();
        }
        #endregion
    }
}
