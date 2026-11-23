using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>룬 하나 (룬 주문. 다중 룬 마법의 재료). 캐릭터 생성에서 골라 익힘. 그림은 코덱스 룬 주문 장·연습 흐린 그림·판정 비교 그림에 씀</summary>
    [CreateAssetMenu(fileName = "Rune", menuName = "Mage/Rune")]
    public sealed class RuneSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (프로토타입 spell-core.js와 같음: fire, condense, line, block, water, spread, wall)</summary>
        [SerializeField] string _id;

        /// <summary>룬 이름 (Rune 테이블 rune.{id}.name)</summary>
        [SerializeField] LocalizedString _name;

        /// <summary>룬 설명 (Rune 테이블 rune.{id}.desc)</summary>
        [SerializeField] LocalizedString _description;

        /// <summary>카드에 보일 그림 (룬 그림 모양). 비면 안 보임</summary>
        [SerializeField] Sprite _icon;

        /// <summary>룬 그림 획 (y 위가 +, 대략 -1~1. 프로토타입 spell-core.js 룬 그림의 y를 뒤집음)</summary>
        [SerializeField] List<StrokeEntry> _strokes = new();

        /// <summary>이 룬 하나만 그렸을 때 현장 효과. 불·물처럼 만드는 룬만 정하고, 나머지는 None (혼자서는 효과 없음)</summary>
        [SerializeField] SpellFieldEffect _soloEffect;

        /// <summary>혼자 던질 때 마나 (임시)</summary>
        [SerializeField, Min(0)] int _soloManaCost;

        /// <summary>혼자 던질 때 위력 (뜻은 SpellFieldEffect, 임시)</summary>
        [SerializeField, Min(0f)] float _soloPower;

        /// <summary>혼자 던질 때 범위 (뜻은 SpellFieldEffect, 임시)</summary>
        [SerializeField, Min(0f)] float _soloArea;

        /// <summary>인식기에 넘길 그림 (처음 쓸 때 만듦. 인스펙터에서 고치면 비움)</summary>
        [NonSerialized] SpellShapeEntry _shape;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>룬 이름</summary>
        public LocalizedString Name => _name;

        /// <summary>룬 설명</summary>
        public LocalizedString Description => _description;

        /// <summary>카드에 보일 그림. 비면 null</summary>
        public Sprite Icon => _icon;

        /// <summary>룬 그림 획</summary>
        public IReadOnlyList<StrokeEntry> Strokes => _strokes;

        /// <summary>이 룬 하나만 그렸을 때 현장 효과 (None = 혼자서는 효과 없음)</summary>
        public SpellFieldEffect SoloEffect => _soloEffect;

        /// <summary>혼자 던질 때 마나</summary>
        public int SoloManaCost => _soloManaCost;

        /// <summary>혼자 던질 때 위력</summary>
        public float SoloPower => _soloPower;

        /// <summary>혼자 던질 때 범위</summary>
        public float SoloArea => _soloArea;

        /// <summary>인식기에 넘길 그림 (부분 하나 = 이 룬)</summary>
        public SpellShapeEntry Shape => _shape ??= BuildShape();

        #region OnValidate - 인스펙터에서 고치면 만들어 둔 그림을 비움
        void OnValidate() => _shape = null;
        #endregion

        #region BuildShape - 이 룬 하나로 된 인식기용 그림 (id = 룬 id, 부분 하나)
        SpellShapeEntry BuildShape() =>
            new(_id, new[] { _id ?? string.Empty }, new[] { _strokes.Select(stroke => stroke.Points.ToArray()).ToArray() });
        #endregion
    }
}
