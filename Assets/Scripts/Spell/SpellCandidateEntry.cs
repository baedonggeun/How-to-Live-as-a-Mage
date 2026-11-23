using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>판정에 쓸 비교 그림 하나 (주문 그림 또는 부분을 뺀 생략본). 점 구름·부분 점은 만들 때 한 번 계산. SpellRecognitionCalculator.BuildCandidates가 만듦</summary>
    public sealed class SpellCandidateEntry
    {
        /// <summary>BuildCandidates에 넘긴 주문 그림 목록 번호</summary>
        public int ShapeIndex { get; }

        /// <summary>원래 주문 그림</summary>
        public SpellShapeEntry Shape { get; }

        /// <summary>뺀 부분 번호 (오름차순. 생략이 없으면 빈 목록)</summary>
        public IReadOnlyList<int> Omit { get; }

        /// <summary>주문의 룬을 다 익혔는지 (안 익혀도 비교는 함: 이유를 보여 주려고)</summary>
        public bool Learned { get; }

        /// <summary>전체 비교용 정규화 점 구름 (CloudPoints개)</summary>
        public Vector2[] Cloud { get; }

        /// <summary>남긴 부분의 룬 id (부분 순서). 부분 일치율 순서와 같음</summary>
        public IReadOnlyList<string> PartRuneIds { get; }

        /// <summary>부분 검사용 촘촘한 템플릿 점</summary>
        public Vector2[] Dense { get; }

        /// <summary>촘촘한 점마다 남긴 부분 번호</summary>
        public int[] DenseParts { get; }

        /// <summary>맞추기용 성긴 템플릿 점</summary>
        public Vector2[] Sparse { get; }

        /// <summary>촘촘한 점 테두리 상자</summary>
        public Rect Bounds { get; }

        /// <summary>테두리 상자 긴 변 (0이면 1)</summary>
        public float Size { get; }

        #region SpellCandidateEntry - 계산해 둔 값을 받아 만듦 (인식기만 부름)
        /// <param name="shapeIndex">주문 그림 목록 번호</param>
        /// <param name="shape">원래 주문 그림</param>
        /// <param name="omit">뺀 부분 번호</param>
        /// <param name="learned">룬을 다 익혔는지</param>
        /// <param name="cloud">전체 비교용 점 구름</param>
        /// <param name="partRuneIds">남긴 부분의 룬 id</param>
        /// <param name="dense">촘촘한 템플릿 점</param>
        /// <param name="denseParts">촘촘한 점마다 부분 번호</param>
        /// <param name="sparse">성긴 템플릿 점</param>
        /// <param name="bounds">촘촘한 점 테두리 상자</param>
        /// <param name="size">테두리 상자 긴 변</param>
        internal SpellCandidateEntry(int shapeIndex, SpellShapeEntry shape, int[] omit, bool learned, Vector2[] cloud,
            string[] partRuneIds, Vector2[] dense, int[] denseParts, Vector2[] sparse, Rect bounds, float size)
        {
            ShapeIndex = shapeIndex;
            Shape = shape;
            Omit = omit;
            Learned = learned;
            Cloud = cloud;
            PartRuneIds = partRuneIds;
            Dense = dense;
            DenseParts = denseParts;
            Sparse = sparse;
            Bounds = bounds;
            Size = size;
        }
        #endregion
    }
}
