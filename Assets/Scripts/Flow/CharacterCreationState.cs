namespace Mage
{
    /// <summary>캐릭터 생성. 이름을 받아 판 데이터에 넣고 오프닝으로</summary>
    public sealed class CharacterCreationState : FlowStateBase
    {
        /// <summary>이 상태의 씬 이름</summary>
        public const string Scene = "CharacterCreation";

        /// <summary>이름 최대 글자 수</summary>
        public const int NameMaxLength = 12;

        /// <summary>이 상태의 씬 이름 (빌드 목록과 같음)</summary>
        public override string SceneName => Scene;

        /// <summary>이 씬의 화면 참조</summary>
        CharacterCreationView _view;

        #region OnSceneReady - 이름 입력 규칙 지정, 확정 버튼 연결
        protected override void OnSceneReady()
        {
            _view = FindView<CharacterCreationView>();
            if (_view == null) return;
            _view.NameInput.characterLimit = NameMaxLength;
            _view.NameInput.onValidateInput = FilterNameChar;
            _view.ConfirmButton.onClick.AddListener(OnConfirm);
        }
        #endregion

        #region OnConfirm - 이름 앞뒤 공백을 지우고, 비었으면 토스트, 아니면 저장 후 오프닝으로
        void OnConfirm()
        {
            // 앞뒤 공백을 지운 이름
            string playerName = _view.NameInput.text.Trim();
            if (playerName.Length == 0)
            {
                UIManager.Instance.ShowToast("creation.name_empty");
                return;
            }
            Game.Run.PlayerName = playerName;
            Game.ChangeState(new OpeningState());
        }
        #endregion

        #region FilterNameChar - 이름에 TMP 서식 태그 글자(<, >)가 들어가지 않게 막음
        /// <param name="text">지금까지 입력된 이름</param>
        /// <param name="charIndex">넣을 위치</param>
        /// <param name="addedChar">넣으려는 글자</param>
        static char FilterNameChar(string text, int charIndex, char addedChar) =>
            addedChar == '<' || addedChar == '>' ? '\0' : addedChar;
        #endregion
    }
}
