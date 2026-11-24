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

    /// <summary>현장에서 주문이 내는 효과 (프로토타입 field-spells.html). 위력·범위·시간·재사용 값의 뜻이 효과마다 다름</summary>
    public enum SpellFieldEffect
    {
        /// <summary>현장 효과 없음 (두루마리, 혼자서는 효과가 없는 룬)</summary>
        None,

        /// <summary>화염탄 한 발. 위력 = 피해</summary>
        Bolt,

        /// <summary>화염탄 + 폭발. 위력 = 피해, 범위 = 폭발 반지름 (m, 맞은 적 둘레는 절반 피해)</summary>
        Ball,

        /// <summary>물탄. 위력 = 피해, 범위 = 맞은 적을 밀쳐내는 거리 (m)</summary>
        Water,

        /// <summary>앞쪽 부채꼴 세 발. 위력 = 한 발 피해, 범위 = 가운데 발과 양쪽 발 사이 각도 (도)</summary>
        Fan,

        /// <summary>조준점 불벽. 위력 = 초당 피해, 범위 = 벽 길이 (m), 시간 = 지속 (초)</summary>
        Wall,

        /// <summary>방호막 (조준 없이 바로). 위력 = 받는 피해 배율, 시간 = 지속 (초), 재사용 = 다시 펼칠 수 있을 때까지 (초)</summary>
        Shield,
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

        /// <summary>현장 효과 (두루마리·현장 값이 없는 주문은 None)</summary>
        [SerializeField] SpellFieldEffect _fieldEffect;

        /// <summary>현장 마나 (임시)</summary>
        [SerializeField, Min(0)] int _manaCost;

        /// <summary>현장 위력 (뜻은 SpellFieldEffect)</summary>
        [SerializeField, Min(0f)] float _power;

        /// <summary>현장 범위 (뜻은 SpellFieldEffect)</summary>
        [SerializeField, Min(0f)] float _area;

        /// <summary>현장 지속 시간 (초. 벽·방호막)</summary>
        [SerializeField, Min(0f)] float _duration;

        /// <summary>다시 쓸 수 있을 때까지 (초. 방호막)</summary>
        [SerializeField, Min(0f)] float _cooldown;

        /// <summary>성공 사운드 (시전이 받아들여진 순간 2D로 남. 비면 무음)</summary>
        [SerializeField] SoundDataSO _successSound;

        /// <summary>효과 프리팹 (투사체·불벽·방호막에 입힘. 비면 임시 도형)</summary>
        [SerializeField] GameObject _effectPrefab;

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

        /// <summary>현장 효과</summary>
        public SpellFieldEffect FieldEffect => _fieldEffect;

        /// <summary>현장 마나</summary>
        public int ManaCost => _manaCost;

        /// <summary>현장 위력</summary>
        public float Power => _power;

        /// <summary>현장 범위</summary>
        public float Area => _area;

        /// <summary>현장 지속 시간 (초)</summary>
        public float Duration => _duration;

        /// <summary>다시 쓸 수 있을 때까지 (초)</summary>
        public float Cooldown => _cooldown;

        /// <summary>성공 사운드 (없으면 null)</summary>
        public SoundDataSO SuccessSound => _successSound;

        /// <summary>효과 프리팹 (없으면 null)</summary>
        public GameObject EffectPrefab => _effectPrefab;

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
