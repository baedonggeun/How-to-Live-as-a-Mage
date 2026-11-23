using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>튜토리얼에서 배우는 행동 (조작 안내가 사라지는 조건)</summary>
    public enum TutorialAction
    {
        /// <summary>이동 입력 (WASD)</summary>
        Move,

        /// <summary>시점 입력 (마우스)</summary>
        Look,

        /// <summary>움직이며 달리기 입력 (Shift)</summary>
        Sprint,

        /// <summary>상호작용 (E). 12시 안내 없이 게임 코드가 처음 쓸 때 배움으로 알림</summary>
        Interact,

        /// <summary>주문 그리기 (책상 연습). 12시 안내 없이 처음 판정까지 그리면 배움으로 알림</summary>
        Draw,
    }

    /// <summary>튜토리얼 하나: 화면 12시 조작 안내 문구와 튜토리얼 탭에 남는 제목·설명. 배우면 이 PC에 기록</summary>
    [CreateAssetMenu(fileName = "Tutorial", menuName = "Mage/Tutorial")]
    public sealed class TutorialSO : ScriptableObject
    {
        /// <summary>바뀌지 않는 id (게임 코드가 알릴 때 씀: move, look, sprint, interact). 배운 기록 파일에 저장됨</summary>
        [SerializeField] string _id;

        /// <summary>화면 12시에 띄울 짧은 안내 (Tutorial 테이블 tutorial.{id}.prompt). 비면 안 띄움 (배움만 기록)</summary>
        [SerializeField] LocalizedString _prompt;

        /// <summary>튜토리얼 탭 제목 (tutorial.{id}.title)</summary>
        [SerializeField] LocalizedString _title;

        /// <summary>튜토리얼 탭 설명 (tutorial.{id}.body)</summary>
        [SerializeField] LocalizedString _body;

        /// <summary>배웠다고 치는 행동</summary>
        [SerializeField] TutorialAction _action;

        /// <summary>안내를 띄우기 전에 달리지 않고 걸어야 하는 시간 (초). 0이면 바로 띄움</summary>
        [SerializeField, Min(0f)] float _walkSecondsBeforeShow;

        /// <summary>바뀌지 않는 id</summary>
        public string Id => _id;

        /// <summary>화면 12시 안내. 비면 안 띄움</summary>
        public LocalizedString Prompt => _prompt;

        /// <summary>튜토리얼 탭 제목</summary>
        public LocalizedString Title => _title;

        /// <summary>튜토리얼 탭 설명</summary>
        public LocalizedString Body => _body;

        /// <summary>배웠다고 치는 행동</summary>
        public TutorialAction Action => _action;

        /// <summary>안내를 띄우기 전에 걸어야 하는 시간 (초)</summary>
        public float WalkSecondsBeforeShow => _walkSecondsBeforeShow;
    }
}
