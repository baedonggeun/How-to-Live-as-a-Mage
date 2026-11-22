using UnityEngine;

namespace Mage
{
    /// <summary>드나드는 문 표지 (같은 오브젝트의 InteractPointView와 같이 씀, 로직 없음). 쓰면 화면이 잠깐 어두워졌다가 도착 지점으로 옮김</summary>
    public sealed class DoorView : MonoBehaviour
    {
        /// <summary>문을 쓰면 서는 곳 (위치·좌우 방향. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _destination;

        /// <summary>문을 쓰면 서는 곳</summary>
        public Transform Destination => _destination;
    }
}
