#if UNITY_EDITOR
using System.Globalization;

namespace Mage
{
    /// <summary>튜토리얼 담당 디버그 진입점 (에디터 전용, 빌드에 안 들어감): 이 PC 튜토리얼을 전부 배움으로·전부 안 배움으로 (기기 설정에 넣고 파일 저장. 매니저라 다른 매니저를 직접 씀)</summary>
    public sealed partial class TutorialManager
    {
        #region DebugLearnAll - 튜토리얼을 전부 배움으로 저장하고 떠 있던 안내·대기열을 비움
        [DebugControl("튜토리얼", "전부 배움으로 (이 PC)")]
        string DebugLearnAll()
        {
            // 기기 설정 담당·튜토리얼 목록 담당 (Boot 상주)
            SettingsManager settings = SettingsManager.Instance;
            DataManager data = DataManager.Instance;
            if (settings == null || data == null) return "설정·데이터 담당 없음";
            // 새로 배운 수
            int count = 0;
            foreach (TutorialSO tutorial in data.Tutorials)
                if (tutorial != null && settings.Device.AddLearnedTutorial(tutorial.Id)) count++;
            settings.Save();
            _service.Clear();
            return string.Format(CultureInfo.InvariantCulture, "새로 배움 {0}개 (12시 안내 지움)", count);
        }
        #endregion

        #region DebugForgetAll - 이 PC 배운 튜토리얼 기록을 다 지우고 저장 (다음 화면 진입부터 안내가 다시 나옴)
        [DebugControl("튜토리얼", "전부 안 배움으로 (이 PC)")]
        string DebugForgetAll()
        {
            // 기기 설정 담당 (Boot 상주)
            SettingsManager settings = SettingsManager.Instance;
            if (settings == null) return "설정 담당 없음";
            settings.Device.DebugClearLearnedTutorials();
            settings.Save();
            return "배운 기록 지움 (다음 화면 진입부터 안내가 다시 나옴)";
        }
        #endregion
    }
}
#endif
