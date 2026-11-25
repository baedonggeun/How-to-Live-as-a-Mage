#if UNITY_EDITOR
namespace Mage
{
    /// <summary>기기 설정 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 배운 튜토리얼 기록 비우기 (파일 저장은 SettingsManager가 함)</summary>
    public sealed partial class DeviceSettingsEntry
    {
        #region DebugClearLearnedTutorials - 이 PC 배운 튜토리얼 기록을 다 지움
        public void DebugClearLearnedTutorials() => _learnedTutorials.Clear();
        #endregion
    }
}
#endif
