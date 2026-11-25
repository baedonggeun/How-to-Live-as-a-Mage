#if UNITY_EDITOR
using System;

namespace Mage
{
    /// <summary>디버그 패널(DebugWindow) 조작 표시 (에디터 전용, 빌드에 안 들어감): 기능 쪽 Debug partial의 인스턴스 프로퍼티·메서드에 붙임. 붙인 곳에 따라 모양이 정해짐 — 읽고 쓰는 bool = 켜기/끄기(켠 건 기억해 Play 때 화면마다 다시 켬), 읽고 쓰는 int·float = 숫자 칸 + 넣기, 읽기만 하는 프로퍼티 = 값 줄, 인자 없는 메서드(void·string) = 버튼(돌려준 글은 한 일 칸에). 같은 묶음·이름이 여러 곳에 있으면 패널은 지금 화면에 붙은 곳 하나로 보임</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DebugControlAttribute : Attribute
    {
        /// <summary>묶음 이름 (코덱스·의뢰·숲…)</summary>
        public string Group { get; }

        /// <summary>조작 이름</summary>
        public string Label { get; }

        #region DebugControlAttribute - 묶음 이름·조작 이름을 받음
        /// <param name="group">묶음 이름</param>
        /// <param name="label">조작 이름</param>
        public DebugControlAttribute(string group, string label)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }
        #endregion
    }
}
#endif
