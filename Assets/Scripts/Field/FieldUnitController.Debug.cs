#if UNITY_EDITOR
namespace Mage
{
    /// <summary>현장 몸 디버그 칸 (에디터 전용, 빌드에 안 들어감): 무적 (켜면 TakeDamage가 피해를 안 깎음. 받는 피해 배율·방호막과 따로). 숲 상태 디버그 진입점이 마법사 몸에만 켬</summary>
    public sealed partial class FieldUnitController
    {
        /// <summary>피해를 안 받는지 (디버그 패널 무적)</summary>
        public bool DebugInvincible { get; set; }
    }
}
#endif
