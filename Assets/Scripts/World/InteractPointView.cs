using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>E로 쓸 수 있는 곳 표지 (씬 오브젝트, 로직 없음). 무엇을 할지는 상태가 Id로 정함. 콜라이더가 같은 오브젝트나 자식에 있어야 바라볼 때 잡힘</summary>
    public sealed class InteractPointView : MonoBehaviour
    {
        /// <summary>상태가 구분할 id (전초기지: recruit, board, depart). 인스펙터에서만 정함</summary>
        [SerializeField] string _id;

        /// <summary>안내에 보일 이름 ("[E] 출발"의 출발). 인스펙터에서만 정함</summary>
        [SerializeField] LocalizedString _label;

        /// <summary>상태가 구분할 id</summary>
        public string Id => _id;

        /// <summary>안내에 보일 이름</summary>
        public LocalizedString Label => _label;
    }
}
