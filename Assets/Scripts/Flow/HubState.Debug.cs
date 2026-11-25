#if UNITY_EDITOR
namespace Mage
{
    /// <summary>전초기지 상태 확인 진입점 (에디터 전용, 빌드에 안 들어감): 어디서든 정문 경비병과 말하기 (출발 검사·출발 경로 그대로)</summary>
    public sealed partial class HubState
    {
        /// <summary>확인 버튼을 막을지 (씬 준비 전·첫 도착 연출 중)</summary>
        public override bool CheckBusy => _view == null || _arriving;

        #region CheckTalkGuard - 정문 경비병과 말하기 (모자란 게 있으면 막는 말, 아니면 출발 선택지) (3-4·6-3)
        [CheckButton("forest-dungeon", "3-4,6-3", "경비병과 말하기")]
        string CheckTalkGuard()
        {
            if (CheckBusy) return "지금은 안 됨";
            TalkGuard();
            return "경비병 대화 (출발을 고르면 숲으로)";
        }
        #endregion
    }
}
#endif
