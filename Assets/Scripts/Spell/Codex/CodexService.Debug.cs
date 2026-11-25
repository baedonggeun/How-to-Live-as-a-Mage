#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>코덱스 캡슐 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 룬 전부 익힘·다중 룬 마법 전부 등록 (끄면 켜기 전 목록으로. 켜기 전 목록은 코덱스 판 데이터에 둬서 화면이 바뀌어도 되돌림), 생략 가능 수. 바꾸면 판정 비교 그림을 다시 만듦 (자기 데이터와 받은 목록만 씀. 등록 토스트는 안 냄)</summary>
    public sealed partial class CodexService
    {
        #region DebugAllRunes - 룬을 다 익혔는지. 켜면 지금 익힌 룬을 적어 두고 전부 익힘, 끄면 적어 둔 룬으로 되돌림 (적어 둔 게 없으면 그대로)
        [DebugControl("코덱스", "룬 전부 익힘")]
        bool DebugAllRunes
        {
            get
            {
                foreach (RuneSO rune in _runes)
                    if (rune != null && !_data.LearnedRunes.Contains(rune)) return false;
                return true;
            }
            set
            {
                if (value == DebugAllRunes) return;
                if (value)
                {
                    _data.DebugRunesBefore = new List<RuneSO>(_data.LearnedRunes);
                    SetLearned(_runes);
                    return;
                }
                if (_data.DebugRunesBefore == null) return;
                SetLearned(_data.DebugRunesBefore);
                _data.DebugRunesBefore = null;
            }
        }
        #endregion

        #region DebugAllSpells - 다중 룬 마법을 다 등록했는지. 켜면 지금 등록한 마법을 적어 두고 안 한 것을 목록 순으로 더함, 끄면 적어 둔 마법으로 되돌림 (적어 둔 게 없으면 그대로)
        [DebugControl("코덱스", "다중 룬 마법 전부 등록")]
        bool DebugAllSpells
        {
            get
            {
                foreach (SpellSO spell in _spells)
                    if (spell != null && !_data.CollectedSpells.Contains(spell)) return false;
                return true;
            }
            set
            {
                if (value == DebugAllSpells) return;
                if (value)
                {
                    _data.DebugSpellsBefore = new List<SpellSO>(_data.CollectedSpells);
                    foreach (SpellSO spell in _spells)
                        if (spell != null && !_data.CollectedSpells.Contains(spell)) _data.CollectedSpells.Add(spell);
                    RebuildCandidates();
                    return;
                }
                if (_data.DebugSpellsBefore == null) return;
                _data.CollectedSpells.Clear();
                _data.CollectedSpells.AddRange(_data.DebugSpellsBefore);
                _data.DebugSpellsBefore = null;
                RebuildCandidates();
            }
        }
        #endregion

        #region DebugOmitLimit - 생략 가능 수 (0 이상. 넣으면 판정 비교 그림을 다시 만듦)
        [DebugControl("코덱스", "생략 가능 수")]
        int DebugOmitLimit
        {
            get => _data.OmitLimit;
            set
            {
                _data.OmitLimit = Mathf.Max(0, value);
                RebuildCandidates();
            }
        }
        #endregion
    }
}
#endif
