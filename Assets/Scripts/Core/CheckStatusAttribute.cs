#if UNITY_EDITOR
using System;

namespace Mage
{
    /// <summary>확인 패널(CheckWindow) 상태 줄 표시 (에디터 전용, 빌드에 안 들어감): 기능 쪽 Debug partial의 인자 없이 string을 돌려주는 인스턴스 메서드에 붙임. Play 중 1초에 10번 읽어 '이름: 값'으로 보임</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CheckStatusAttribute : Attribute
    {
        /// <summary>상태 줄 이름</summary>
        public string Label { get; }

        #region CheckStatusAttribute - 상태 줄 이름을 받음
        /// <param name="label">상태 줄 이름</param>
        public CheckStatusAttribute(string label)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }
        #endregion
    }
}
#endif
