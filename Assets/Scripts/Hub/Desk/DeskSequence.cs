using System;
using System.Collections;
using UnityEngine;

namespace Mage
{
    /// <summary>책상 시점 연출: 플레이어 머리(카메라)를 책상 시점 자리로 부드럽게 옮기고, 일어날 때 원래 자리로 되돌림. 게임 시간이라 일시정지 중엔 멈춤</summary>
    public sealed class DeskSequence : MonoBehaviour
    {
        /// <summary>옮기는 데 걸리는 시간 (초)</summary>
        [SerializeField, Min(0f)] float _seconds = 0.5f;

        /// <summary>옮긴 머리 (앉아 있는 동안만. 아니면 null)</summary>
        Transform _head;

        /// <summary>머리 원래 로컬 위치 (앉기 전)</summary>
        Vector3 _homePosition;

        /// <summary>머리 원래 로컬 회전 (앉기 전)</summary>
        Quaternion _homeRotation;

        /// <summary>도는 연출. 없으면 null</summary>
        Coroutine _routine;

        /// <summary>연출이 도는 중인지</summary>
        public bool IsPlaying => _routine != null;

        #region Enter - 머리 원래 자리를 적어 두고 책상 시점 자리로 옮김. 다 옮기면 onDone
        /// <param name="head">옮길 머리 (카메라)</param>
        /// <param name="seat">책상 시점 자리</param>
        /// <param name="onDone">다 옮긴 뒤 할 일</param>
        public void Enter(Transform head, Transform seat, Action onDone)
        {
            if (head == null) throw new ArgumentNullException(nameof(head));
            if (seat == null) throw new ArgumentNullException(nameof(seat));
            Stop();
            _head = head;
            _homePosition = head.localPosition;
            _homeRotation = head.localRotation;
            _routine = StartCoroutine(Move(() => new Pose(seat.position, seat.rotation), onDone));
        }
        #endregion

        #region Exit - 머리를 원래 자리로 되돌림. 다 되돌리면 로컬 자리를 그대로 맞추고 onDone (앉아 있지 않으면 바로 onDone)
        /// <param name="onDone">다 되돌린 뒤 할 일</param>
        public void Exit(Action onDone)
        {
            if (_head == null)
            {
                onDone?.Invoke();
                return;
            }
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Move(HomePose, () =>
            {
                Restore();
                onDone?.Invoke();
            }));
        }
        #endregion

        #region Stop - 연출을 멈추고 머리를 원래 로컬 자리로 바로 되돌림 (상태를 나갈 때)
        public void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            Restore();
        }
        #endregion

        #region HomePose - 머리 원래 자리 (월드. 부모가 움직여도 따라감)
        Pose HomePose()
        {
            // 머리 부모 (플레이어)
            Transform parent = _head.parent;
            return parent != null
                ? new Pose(parent.TransformPoint(_homePosition), parent.rotation * _homeRotation)
                : new Pose(_homePosition, _homeRotation);
        }
        #endregion

        #region Restore - 머리를 원래 로컬 자리로 맞추고 놓음
        void Restore()
        {
            if (_head == null) return;
            _head.localPosition = _homePosition;
            _head.localRotation = _homeRotation;
            _head = null;
        }
        #endregion

        #region Move - 지금 자리에서 목표 자리로 정해진 시간 동안 부드럽게 (처음·끝 느리게). 끝나면 onDone
        /// <param name="target">목표 자리 (매 프레임 다시 읽음)</param>
        /// <param name="onDone">다 옮긴 뒤 할 일</param>
        IEnumerator Move(Func<Pose> target, Action onDone)
        {
            // 출발 자리 (월드)
            Vector3 fromPosition = _head.position;
            Quaternion fromRotation = _head.rotation;
            // 진행 (0~1)
            float t = 0f;
            while (t < 1f)
            {
                t = _seconds > 0f ? Mathf.Min(1f, t + Time.deltaTime / _seconds) : 1f;
                // 부드러운 진행
                float k = Mathf.SmoothStep(0f, 1f, t);
                // 이번 프레임 목표
                Pose to = target();
                _head.SetPositionAndRotation(Vector3.Lerp(fromPosition, to.position, k), Quaternion.Slerp(fromRotation, to.rotation, k));
                yield return null;
            }
            _routine = null;
            onDone?.Invoke();
        }
        #endregion
    }
}
