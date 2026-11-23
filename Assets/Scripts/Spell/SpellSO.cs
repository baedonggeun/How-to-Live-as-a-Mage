using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>주문 종류</summary>
    public enum SpellKind
    {
        /// <summary>현장에서 그려서 쓰는 전투 주문</summary>
        Combat,

        /// <summary>두루마리 (주문 그림 + 차단)</summary>
        Scroll,
    }

    /// <summary>주문 하나 (도감에 실린 그림). 그림은 룬 부분을 합친 것이고 인식기가 통째로 대조함</summary>
    [CreateAssetMenu(fileName = "Spell", menuName = "Mage/Spell")]
    public sealed class SpellSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (flame_shot, fireball …)</summary>
        [SerializeField] string _id;

        /// <summary>주문 이름 (Spell 테이블 spell.{id}.name)</summary>
        [SerializeField] LocalizedString _name;

        /// <summary>주문 설명 (Spell 테이블 spell.{id}.desc)</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>전투 주문인지 두루마리인지</summary>
        [SerializeField] SpellKind _kind;

        /// <summary>그림 부분 (룬마다 하나)</summary>
        [SerializeField] List<SpellPartEntry> _parts = new();

        /// <summary>인식기에 넘길 그림 (처음 쓸 때 만듦. 인스펙터에서 고치면 비움)</summary>
        [NonSerialized] SpellShapeEntry _shape;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>주문 이름</summary>
        public LocalizedString Name => _name;

        /// <summary>주문 설명</summary>
        public LocalizedString Description => _description;

        /// <summary>전투 주문인지 두루마리인지</summary>
        public SpellKind Kind => _kind;

        /// <summary>그림 부분</summary>
        public IReadOnlyList<SpellPartEntry> Parts => _parts;

        /// <summary>인식기에 넘길 그림 (룬 id와 획 점)</summary>
        public SpellShapeEntry Shape => _shape ??= BuildShape();

        #region OnValidate - 인스펙터에서 고치면 만들어 둔 그림을 비움
        void OnValidate() => _shape = null;
        #endregion

        #region BuildShape - 부분마다 룬 id와 획 점 배열을 모아 인식기용 그림을 만듦 (룬이 비면 빈 id)
        SpellShapeEntry BuildShape()
        {
            // 부분마다 룬 id
            var runeIds = new string[_parts.Count];
            // 부분마다 획 → 점
            var strokes = new Vector2[_parts.Count][][];
            for (int i = 0; i < _parts.Count; i++)
            {
                // 이 부분
                SpellPartEntry part = _parts[i];
                runeIds[i] = part.Rune != null ? part.Rune.Id : string.Empty;
                strokes[i] = part.Strokes.Select(stroke => stroke.Points.ToArray()).ToArray();
            }
            return new SpellShapeEntry(_id, runeIds, strokes);
        }
        #endregion
    }
}
