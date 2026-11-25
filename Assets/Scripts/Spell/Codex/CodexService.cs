using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>코덱스 캡슐 (MonoBehaviour 아님. 상태가 만들어 책상 코덱스·숲 시전에 넘김): 코덱스 판 데이터와 다중 룬 마법·룬 목록을 받아, 익힌 룬·등록한 마법·생략 가능 수로 판정 비교 그림을 만들고 그린 획을 판정함. 판정 그림 번호의 이름·마법·룬·성공 사운드, 룬 id의 이름을 알려 주고, 생략 없이 처음 시전한 다중 룬 마법을 등록해(비교 그림을 다시 만듦) Registered로 알림. 캐릭터 생성에서 고른 룬을 익힘</summary>
    public sealed class CodexService
    {
        /// <summary>룬 주문 이름 키 ({0} 룬 이름)</summary>
        const string RuneSpellKey = "codex.rune_spell";

        /// <summary>코덱스 문구 테이블</summary>
        static readonly TableReference Table = "Codex";

        /// <summary>코덱스 판 데이터</summary>
        readonly CodexEntry _data;

        /// <summary>다중 룬 마법 전부 (DataManager 순서)</summary>
        readonly IReadOnlyList<SpellSO> _spells;

        /// <summary>룬 전부 (DataManager 순서)</summary>
        readonly IReadOnlyList<RuneSO> _runes;

        /// <summary>판정 비교 그림 원본 (다중 룬 마법 순서 뒤에 룬 순서, 빈 칸은 null). 판정 ShapeIndex가 이 번호</summary>
        readonly SpellShapeEntry[] _shapes;

        /// <summary>익힌 룬 id (비교 그림을 만들 때 채움)</summary>
        readonly HashSet<string> _learnedIds = new(StringComparer.Ordinal);

        /// <summary>판정 비교 그림 (익힌 룬·등록이 바뀌면 다시 만듦)</summary>
        List<SpellCandidateEntry> _candidates = new();

        /// <summary>다중 룬 마법을 새로 등록함 (등록한 마법)</summary>
        public event Action<SpellSO> Registered;

        #region CodexService - 코덱스 판 데이터·다중 룬 마법·룬 목록을 받아 판정 비교 그림을 만듦
        /// <param name="data">코덱스 판 데이터</param>
        /// <param name="spells">다중 룬 마법 전부 (DataManager 순서)</param>
        /// <param name="runes">룬 전부 (DataManager 순서)</param>
        public CodexService(CodexEntry data, IReadOnlyList<SpellSO> spells, IReadOnlyList<RuneSO> runes)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _spells = spells ?? throw new ArgumentNullException(nameof(spells));
            _runes = runes ?? throw new ArgumentNullException(nameof(runes));
            _shapes = spells.Select(spell => spell != null ? spell.Shape : null)
                .Concat(runes.Select(rune => rune != null ? rune.Shape : null)).ToArray();
            RebuildCandidates();
        }
        #endregion

        #region Judge - 그린 획 판정 (기준은 기본값, 임시)
        /// <param name="strokes">그린 획</param>
        public SpellJudgeSpec Judge(IReadOnlyList<Vector2[]> strokes) =>
            SpellRecognitionCalculator.Judge(strokes, _candidates, _learnedIds, SpellRecognitionCalculator.DefaultPartMin, SpellRecognitionCalculator.DefaultAllMin);
        #endregion

        #region NameAt - 판정 그림 번호의 이름 (지금 언어): 다중 룬 마법 이름, 그 뒤 번호는 '불 룬 주문'
        /// <param name="index">판정 ShapeIndex</param>
        public string NameAt(int index)
        {
            if (index < 0) return string.Empty;
            if (index < _spells.Count) return _spells[index] != null ? _spells[index].Name.GetLocalizedString() : string.Empty;
            // 룬 번호
            int rune = index - _spells.Count;
            return rune < _runes.Count && _runes[rune] != null
                ? new LocalizedString(Table, RuneSpellKey).GetLocalizedString(_runes[rune].Name.GetLocalizedString())
                : string.Empty;
        }
        #endregion

        #region RuneName - 룬 id의 이름 (지금 언어, 모르는 id면 id 그대로)
        /// <param name="id">룬 id</param>
        public string RuneName(string id)
        {
            if (!string.IsNullOrEmpty(id))
                foreach (RuneSO rune in _runes)
                    if (rune != null && string.Equals(rune.Id, id, StringComparison.Ordinal)) return rune.Name.GetLocalizedString();
            return id;
        }
        #endregion

        #region SpellAt - 판정 그림 번호의 다중 룬 마법 (다중 룬 마법 번호가 아니거나 빈 칸이면 null)
        /// <param name="index">판정 ShapeIndex</param>
        public SpellSO SpellAt(int index) => index >= 0 && index < _spells.Count ? _spells[index] : null;
        #endregion

        #region RuneAt - 판정 그림 번호의 룬 (룬 주문 번호가 아니거나 빈 칸이면 null)
        /// <param name="index">판정 ShapeIndex</param>
        public RuneSO RuneAt(int index)
        {
            // 룬 번호
            int rune = index - _spells.Count;
            return index >= 0 && rune >= 0 && rune < _runes.Count ? _runes[rune] : null;
        }
        #endregion

        #region SuccessSoundAt - 판정 그림 번호 주문의 성공 사운드 (넣지 않았거나 없으면 null)
        /// <param name="index">판정 ShapeIndex</param>
        public SoundDataSO SuccessSoundAt(int index)
        {
            // 그 번호의 다중 룬 마법
            SpellSO spell = SpellAt(index);
            if (spell != null) return spell.SuccessSound;
            // 그 번호의 룬
            RuneSO rune = RuneAt(index);
            return rune != null ? rune.SuccessSound : null;
        }
        #endregion

        #region LearnedRunes - 익힌 룬 (룬 목록 순서, 복사본)
        public RuneSO[] LearnedRunes() => _runes.Where(rune => rune != null && _data.LearnedRunes.Contains(rune)).ToArray();
        #endregion

        #region CollectedSpells - 등록한 다중 룬 마법 (다중 룬 마법 목록 순서, 복사본)
        public SpellSO[] CollectedSpells() => _spells.Where(spell => spell != null && _data.CollectedSpells.Contains(spell)).ToArray();
        #endregion

        #region TryRegister - 생략 없이 시전한 다중 룬 마법이 아직 등록 안 됐으면 등록하고 비교 그림을 다시 만든 뒤 알림 (룬 주문은 등록 없음). 새로 등록했으면 true
        /// <param name="spec">판정 결과</param>
        public bool TryRegister(SpellJudgeSpec spec)
        {
            if (!spec.IsCast || spec.Candidate == null || spec.Candidate.Omit.Count > 0) return false;
            // 시전한 다중 룬 마법 (룬 주문이면 null)
            SpellSO spell = SpellAt(spec.Candidate.ShapeIndex);
            if (spell == null || _data.CollectedSpells.Contains(spell)) return false;
            _data.CollectedSpells.Add(spell);
            RebuildCandidates();
            Registered?.Invoke(spell);
            return true;
        }
        #endregion

        #region SetLearned - 익힌 룬을 이 룬들로 바꾸고(빈 칸은 건너뜀) 비교 그림을 다시 만듦 (캐릭터 생성)
        /// <param name="runes">익힐 룬</param>
        public void SetLearned(IEnumerable<RuneSO> runes)
        {
            if (runes == null) throw new ArgumentNullException(nameof(runes));
            _data.LearnedRunes.Clear();
            foreach (RuneSO rune in runes)
                if (rune != null) _data.LearnedRunes.Add(rune);
            RebuildCandidates();
        }
        #endregion

        #region RebuildCandidates - 익힌 룬·등록한 마법·생략 가능 수로 판정 비교 그림을 다시 만듦
        void RebuildCandidates()
        {
            _learnedIds.Clear();
            foreach (RuneSO rune in _data.LearnedRunes)
                if (rune != null) _learnedIds.Add(rune.Id);
            // 등록한 마법 id
            var collected = new HashSet<string>(StringComparer.Ordinal);
            foreach (SpellSO spell in _data.CollectedSpells)
                if (spell != null) collected.Add(spell.Id);
            _candidates = SpellRecognitionCalculator.BuildCandidates(_shapes, _learnedIds, collected, _data.OmitLimit);
        }
        #endregion
    }
}
