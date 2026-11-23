using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>주문 도감 탭 ← 이번 판 코덱스 (도감에 등록한 주문). 탭이 켜질 때 코덱스 이름 머리글과 목록을 다시 만들고, 항목을 누르면 이름·종류·필요한 룬·그림·설명</summary>
    public sealed class SpellCodexUIBinder : MonoBehaviour
    {
        /// <summary>필요한 룬 키 ({0} 룬 목록)</summary>
        const string RunesKey = "menu.spells.runes";

        /// <summary>전투 주문 종류 키</summary>
        const string CombatKey = "menu.spells.kind.combat";

        /// <summary>두루마리 종류 키</summary>
        const string ScrollKey = "menu.spells.kind.scroll";

        /// <summary>메뉴 창 문구 테이블</summary>
        static readonly TableReference Table = "Menu";

        /// <summary>메뉴 창 참조 (인스펙터에서만 연결)</summary>
        [SerializeField] GameMenuView _view;

        /// <summary>지금 만든 목록 항목</summary>
        readonly List<TutorialListItemView> _items = new();

        #region OnEnable - 목록 만들기
        void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[Menu] SpellCodexUIBinder에 GameMenuView 연결 안 됨", this);
                return;
            }
            Rebuild();
        }
        #endregion

        #region Rebuild - 코덱스 이름 머리글을 넣고, 목록 항목을 지우고 등록 순서대로 다시 만듦. 없으면 빈 글, 있으면 첫 주문 자세히
        void Rebuild()
        {
            foreach (TutorialListItemView item in _items)
                if (item != null) Destroy(item.gameObject);
            _items.Clear();

            // 이번 판 데이터 (없으면 null)
            RunEntry run = GameManager.Instance.Run;
            // 코덱스 이름 머리글 ({0} 플레이어 이름)
            _view.CodexName.StringReference.Arguments = new object[] { run != null ? run.PlayerName : string.Empty };
            _view.CodexName.RefreshString();
            // 이번 판 도감 등록 주문 (판이 없으면 null)
            IReadOnlyList<SpellSO> spells = run?.CollectedSpells;
            // 목록 첫 주문 (자세히를 처음 보일 것. 없으면 null)
            SpellSO first = null;
            if (spells != null)
                foreach (SpellSO spell in spells)
                {
                    if (spell == null) continue;
                    // 이 주문 항목
                    TutorialListItemView item = Instantiate(_view.TutorialItemPrefab, _view.SpellList);
                    item.Title.StringReference.SetReference(spell.Name.TableReference, spell.Name.TableEntryReference);
                    // 버튼이 누를 때 쓸 주문 (반복 변수를 붙잡지 않게)
                    SpellSO picked = spell;
                    item.Button.onClick.AddListener(() => ShowDetail(picked));
                    _items.Add(item);
                    if (first == null) first = spell;
                }
            _view.SpellsEmpty.SetActive(first == null);
            _view.SpellDetail.SetActive(first != null);
            if (first != null) ShowDetail(first);
        }
        #endregion

        #region ShowDetail - 고른 주문의 이름·종류·필요한 룬·설명 키를 넣고 그림을 그림
        /// <param name="spell">고른 주문</param>
        void ShowDetail(SpellSO spell)
        {
            _view.SpellTitle.StringReference.SetReference(spell.Name.TableReference, spell.Name.TableEntryReference);
            _view.SpellKind.StringReference.SetReference(Table, spell.Kind == SpellKind.Scroll ? ScrollKey : CombatKey);
            // 필요한 룬 글자의 String Reference
            var runes = _view.SpellRunes.StringReference;
            runes.SetReference(Table, RunesKey);
            runes.Arguments = new object[]
            {
                UIManager.Instance.JoinList(spell.Parts.Select(part => part.Rune != null ? part.Rune.Name.GetLocalizedString() : null)),
            };
            _view.SpellRunes.RefreshString();
            _view.SpellBody.StringReference.SetReference(spell.Description.TableReference, spell.Description.TableEntryReference);
            _view.SpellDrawing.SetStrokes(spell.Shape.PartStrokes.SelectMany(part => part), true);
        }
        #endregion
    }
}
