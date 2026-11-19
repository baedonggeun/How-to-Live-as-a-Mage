using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>정적 데이터 목록 담당 (요소·주문·몬스터·의뢰 SO). 지금은 요소만</summary>
    public sealed class DataManager : MonoSingleton<DataManager>
    {
        /// <summary>모든 요소 (Boot에서 연결. 이 순서대로 생성 화면 카드가 나옴)</summary>
        [SerializeField] ElementSO[] _elements;

        /// <summary>모든 요소 (연결 순서, 읽기 전용)</summary>
        public IReadOnlyList<ElementSO> Elements => _elements;

        #region InitSingleton - 요소 목록 검사: 비었는지, 빈 칸, 빈 id, id 겹침
        protected override void InitSingleton()
        {
            if (_elements == null || _elements.Length == 0)
            {
                Debug.LogError("[Data] 요소 목록 비어 있음");
                _elements = Array.Empty<ElementSO>();
                return;
            }

            // 지금까지 본 id (대소문자 구분)
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _elements.Length; i++)
            {
                // 검사할 요소
                ElementSO element = _elements[i];
                if (element == null)
                    Debug.LogError($"[Data] 요소 {i}번 칸 비어 있음");
                else if (string.IsNullOrWhiteSpace(element.Id))
                    Debug.LogError($"[Data] {element.name}의 id 비어 있음");
                else if (!ids.Add(element.Id))
                    Debug.LogError($"[Data] 요소 id 겹침: {element.Id} ({element.name})");
            }
        }
        #endregion
    }
}
