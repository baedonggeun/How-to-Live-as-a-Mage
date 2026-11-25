#if UNITY_EDITOR
using System;

namespace Mage
{
    /// <summary>확인 패널(CheckWindow) 버튼 표시 (에디터 전용, 빌드에 안 들어감): 기능 쪽 Debug partial의 인자 없는 인스턴스 메서드에 붙임. 돌려준 글(string)은 패널 아래 '한 일' 칸에 보임 (void면 버튼 글)</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CheckButtonAttribute : Attribute
    {
        /// <summary>plan 이름 (docs/plan-{Plan}.md의 '## 확인 목록')</summary>
        public string Plan { get; }

        /// <summary>이 버튼을 쓰는 확인 목록 번호 (예: "6-1", "7-1")</summary>
        public string[] Items { get; }

        /// <summary>버튼 글 (확인 목록 글의 [ ] 안과 같게)</summary>
        public string Label { get; }

        #region CheckButtonAttribute - plan 이름·확인 목록 번호(쉼표로 이음)·버튼 글을 받음
        /// <param name="plan">plan 이름</param>
        /// <param name="items">확인 목록 번호 (쉼표로 이음, 예: "6-1,6-2")</param>
        /// <param name="label">버튼 글</param>
        public CheckButtonAttribute(string plan, string items, string label)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (items == null) throw new ArgumentNullException(nameof(items));
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Items = items.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Items.Length; i++) Items[i] = Items[i].Trim();
        }
        #endregion
    }
}
#endif
