namespace Mage
{
    /// <summary>현장에서 시전할 주문 하나 (판정 뒤 만듦): 효과·마나·위력·범위·시간·재사용, 이름(지금 언어)·일치율. 정확(90% 이상)이면 위력·방호막 지속 ×1.4 (v1)</summary>
    public readonly struct FieldCastSpec
    {
        /// <summary>정확할 때 배율 (v1)</summary>
        const float ExactScale = 1.4f;

        /// <summary>현장 효과</summary>
        public SpellFieldEffect Effect { get; }

        /// <summary>마나</summary>
        public int ManaCost { get; }

        /// <summary>위력 (뜻은 SpellFieldEffect)</summary>
        public float Power { get; }

        /// <summary>범위 (뜻은 SpellFieldEffect)</summary>
        public float Area { get; }

        /// <summary>지속 시간 (초)</summary>
        public float Duration { get; }

        /// <summary>다시 쓸 수 있을 때까지 (초)</summary>
        public float Cooldown { get; }

        /// <summary>이름 (지금 언어, 조준 글에 씀)</summary>
        public string Name { get; }

        /// <summary>전체 일치율 (0~1)</summary>
        public float Accuracy { get; }

        /// <summary>정확하게 그렸는지 (일치율 90% 이상)</summary>
        public bool IsExact => Accuracy >= SpellRecognitionCalculator.ExactAccuracy;

        /// <summary>위력·지속에 곱할 배율 (정확 1.4, 아니면 1)</summary>
        public float Scale => IsExact ? ExactScale : 1f;

        #region FieldCastSpec - 값을 받아 만듦
        /// <param name="effect">현장 효과</param>
        /// <param name="manaCost">마나</param>
        /// <param name="power">위력</param>
        /// <param name="area">범위</param>
        /// <param name="duration">지속 시간 (초)</param>
        /// <param name="cooldown">재사용 (초)</param>
        /// <param name="name">이름</param>
        /// <param name="accuracy">전체 일치율</param>
        public FieldCastSpec(SpellFieldEffect effect, int manaCost, float power, float area, float duration, float cooldown, string name, float accuracy)
        {
            Effect = effect;
            ManaCost = manaCost;
            Power = power;
            Area = area;
            Duration = duration;
            Cooldown = cooldown;
            Name = name ?? string.Empty;
            Accuracy = accuracy;
        }
        #endregion

        #region FromSpell - 다중 룬 마법의 현장 값으로 만듦
        /// <param name="spell">마법</param>
        /// <param name="name">이름 (지금 언어)</param>
        /// <param name="accuracy">전체 일치율</param>
        public static FieldCastSpec FromSpell(SpellSO spell, string name, float accuracy) =>
            new(spell.FieldEffect, spell.ManaCost, spell.Power, spell.Area, spell.Duration, spell.Cooldown, name, accuracy);
        #endregion

        #region FromRune - 룬 하나를 혼자 던질 때 값으로 만듦 (지속·재사용 없음)
        /// <param name="rune">룬</param>
        /// <param name="name">이름 (지금 언어)</param>
        /// <param name="accuracy">전체 일치율</param>
        public static FieldCastSpec FromRune(RuneSO rune, string name, float accuracy) =>
            new(rune.SoloEffect, rune.SoloManaCost, rune.SoloPower, rune.SoloArea, 0f, 0f, name, accuracy);
        #endregion
    }
}
