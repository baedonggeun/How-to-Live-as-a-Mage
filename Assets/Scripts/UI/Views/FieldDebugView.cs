using UnityEngine;
using UnityEngine.Localization.Components;

namespace Mage
{
    /// <summary>숲 디버그 화면 참조</summary>
    public sealed class FieldDebugView : MonoBehaviour
    {
        /// <summary>판 데이터 글자 (키 field.debug_info, {0} 이름·{1} 파티 수·{2} 의뢰)</summary>
        public LocalizeStringEvent InfoText;
    }
}
