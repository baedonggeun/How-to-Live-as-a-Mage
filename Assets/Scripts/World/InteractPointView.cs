using UnityEngine;
using UnityEngine.Localization;

namespace Mage
{
    /// <summary>상호작용 키(기본 F)로 쓸 수 있는 곳 표지 (씬 오브젝트, 로직 없음). 무엇을 할지는 상태가 Id로 정함 (문이면 같은 오브젝트의 DoorView). 콜라이더가 같은 오브젝트나 자식에 있어야 바라볼 때 잡힘</summary>
    public sealed class InteractPointView : MonoBehaviour
    {
        /// <summary>상태가 구분할 id (전초기지: innkeeper, clerk, board, guard, room_door, 문들). 인스펙터에서만 정함</summary>
        [SerializeField] string _id;

        /// <summary>안내에 보일 이름 ("[F] 여관"의 여관). 인스펙터에서만 정함</summary>
        [SerializeField] LocalizedString _label;

        /// <summary>상태가 구분할 id</summary>
        public string Id => _id;

        /// <summary>안내에 보일 이름</summary>
        public LocalizedString Label => _label;
    }
}
