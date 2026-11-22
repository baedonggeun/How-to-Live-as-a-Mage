using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>NPC 대화: 화면 아래 말한 사람·대사, 그 위에 선택지 버튼. 고르면 선택지의 할 일만 부름 (다음 대사·닫기는 그 할 일이 정함). 여닫기에 따른 일시정지는 UIManager</summary>
    public sealed class DialogueController : MonoBehaviour
    {
        /// <summary>같은 오브젝트의 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] DialogueView _view;

        /// <summary>지금 만든 선택지 버튼</summary>
        readonly List<DialogueChoiceView> _choices = new();

        /// <summary>대화가 떠 있는지</summary>
        public bool IsOpen => _view != null && _view.Root.activeSelf;

        #region Show - 말한 사람·대사 키를 넣고 선택지 버튼을 다시 만들어 띄움 (떠 있으면 내용만 바꿈)
        /// <param name="table">말한 사람·대사가 든 String Table</param>
        /// <param name="speakerKey">말한 사람 키</param>
        /// <param name="lineKey">대사 키</param>
        /// <param name="choices">선택지 (하나 이상)</param>
        /// <param name="lineArgs">대사의 {0}, {1}… 자리에 넣을 값</param>
        public void Show(TableReference table, string speakerKey, string lineKey, IReadOnlyList<DialogueChoiceEntry> choices, params object[] lineArgs)
        {
            if (_view == null)
            {
                Debug.LogError("[Dialogue] DialogueView 연결 안 됨", this);
                return;
            }
            if (choices == null || choices.Count == 0)
            {
                Debug.LogError($"[Dialogue] 선택지 없는 대화: {lineKey}", this);
                return;
            }
            _view.Speaker.StringReference.SetReference(table, speakerKey);
            // 대사 글자의 String Reference
            var line = _view.Line.StringReference;
            line.SetReference(table, lineKey);
            line.Arguments = lineArgs;
            ClearChoices();
            foreach (DialogueChoiceEntry choice in choices)
            {
                // 이 선택지 버튼
                DialogueChoiceView button = Instantiate(_view.ChoicePrefab, _view.ChoiceList);
                // 버튼 글자의 String Reference
                var label = button.Label.StringReference;
                label.SetReference(choice.Table, choice.Key);
                label.Arguments = choice.Args;
                // 누를 때 쓸 선택지 (반복 변수를 붙잡지 않게)
                DialogueChoiceEntry picked = choice;
                button.Button.onClick.AddListener(() => picked.OnPick());
                button.Label.RefreshString();
                _choices.Add(button);
            }
            _view.Root.SetActive(true);
            _view.Speaker.RefreshString();
            _view.Line.RefreshString();
        }
        #endregion

        #region Hide - 대화를 끄고 선택지 버튼을 지움
        public void Hide()
        {
            ClearChoices();
            if (_view != null) _view.Root.SetActive(false);
        }
        #endregion

        #region ClearChoices - 만든 선택지 버튼을 지움 (누른 버튼의 onClick 안에서 불려도 프레임 끝에 지워짐)
        void ClearChoices()
        {
            foreach (DialogueChoiceView choice in _choices)
                if (choice != null) Destroy(choice.gameObject);
            _choices.Clear();
        }
        #endregion
    }
}
