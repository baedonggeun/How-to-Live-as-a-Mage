using UnityEngine;

namespace Mage
{
    /// <summary>숲 차원문 표지 (Portal.prefab 루트, 로직 없음). 차원문 캡슐이 이 지역 고리 안에 자리를 정해 옮김. 발견 기록 키는 id</summary>
    public sealed class PortalView : MonoBehaviour
    {
        /// <summary>자리 id (씬 안에서 겹치지 않게. 판 데이터 발견 기록 키. 인스펙터에서만 정함)</summary>
        [SerializeField] string _id;

        /// <summary>놓을 지역 (1~3. 인스펙터에서만 정함)</summary>
        [SerializeField, Min(1)] int _region = 1;

        /// <summary>자리 id</summary>
        public string Id => _id;

        /// <summary>놓을 지역</summary>
        public int Region => _region;
    }
}
