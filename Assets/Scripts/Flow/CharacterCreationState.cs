using UnityEngine.Localization.Tables;

namespace Mage
{
    /// <summary>캐릭터 생성. 이름 칸·룬 카드는 생성 캡슐이 맡고, 여기선 캡슐을 만들어 확정 버튼을 이음: 확정 검사 결과로 토스트·확인창, 확정하면 이름은 판 데이터에, 고른 룬은 코덱스 캡슐로 넣고 전초기지로 (첫 도착이라 역마차 연출부터)</summary>
    public sealed class CharacterCreationState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "CharacterCreation";

        /// <summary>이름이 비었을 때 토스트 키</summary>
        const string NameEmptyKey = "creation.name_empty";

        /// <summary>룬이 모자랄 때 토스트 키 ({0} 골라야 할 수)</summary>
        const string RuneRequiredKey = "creation.rune_required";

        /// <summary>시작 확인창 문구 키</summary>
        const string StartConfirmKey = "creation.start_confirm";

        /// <summary>생성 화면 문구 테이블</summary>
        static readonly TableReference Table = "Creation";

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>이 씬의 화면 참조</summary>
        CharacterCreationView _view;

        /// <summary>캐릭터 생성 캡슐 (이름 칸·룬 카드)</summary>
        CharacterCreationController _creation;

        #region OnSceneReady - 생성 캡슐을 만들고(이름 칸 규칙·룬 카드·고른 수 표시) 확정 버튼을 이음
        protected override void OnSceneReady()
        {
            _view = FindView<CharacterCreationView>();
            if (_view == null) return;
            _creation = new CharacterCreationController(_view, DataManager.Instance.Runes);
            _view.ConfirmButton.onClick.AddListener(OnConfirm);
        }
        #endregion

        #region Reveal - 배경음 재생
        public override void Reveal()
        {
            // 비어 있으면 AudioManager가 무시함 (타이틀 곡이 이어짐)
            if (_view != null) AudioManager.Instance.PlayBgm(_view.Bgm);
        }
        #endregion

        #region OnConfirm - 생성 캡슐의 확정 검사: 이름 빈칸·룬 모자람이면 토스트, 다 갖췄으면 시작 확인창 (이름은 확인창을 띄울 때 정함)
        void OnConfirm()
        {
            switch (_creation.Check())
            {
                case CharacterCreationBlock.NameEmpty:
                    UIManager.Instance.ShowToast(Table, NameEmptyKey);
                    return;
                case CharacterCreationBlock.RunesMissing:
                    UIManager.Instance.ShowToast(Table, RuneRequiredKey, CharacterCreationController.StartRuneCount);
                    return;
            }
            // 앞뒤 공백을 지운 이름
            string playerName = _creation.PlayerName;
            UIManager.Instance.ShowConfirm(Table, StartConfirmKey, () => StartRun(playerName));
        }
        #endregion

        #region StartRun - 이름을 판 데이터에 넣고 고른 룬(카드 순서)을 코덱스 캡슐로 익힌 뒤 전초기지로 (첫 도착이라 역마차 연출부터)
        /// <param name="playerName">앞뒤 공백을 지운 이름</param>
        void StartRun(string playerName)
        {
            Game.Run.PlayerName = playerName;
            new CodexService(Game.Run.Codex, DataManager.Instance.Spells, DataManager.Instance.Runes).SetLearned(_creation.ChosenRunes());
            Game.ChangeState(new HubState());
        }
        #endregion
    }
}
