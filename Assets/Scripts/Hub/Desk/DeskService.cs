using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>책상 캡슐 (MonoBehaviour 아님. HubState가 만들고 이음): 책상 시점 연출·코덱스·머리·책상 시점 자리를 받아, 앉으면 커서를 풀고 머리를 책상 시점으로 옮긴 뒤 코덱스를 엶(덮인 채로). 일어나면 코덱스를 덮고(펼쳐져 있으면 덮는 연출) 커서를 잠근 뒤 머리를 되돌리고 Left로 알림. 조작·화면 글자는 상태가 맡음</summary>
    public sealed class DeskService
    {
        /// <summary>책상 시점 연출</summary>
        readonly DeskSequence _sequence;

        /// <summary>코덱스</summary>
        readonly CodexController _codex;

        /// <summary>옮길 머리 (플레이어 카메라)</summary>
        readonly Transform _head;

        /// <summary>책상 시점 자리</summary>
        readonly Transform _seat;

        /// <summary>책상 시점인지 (앉는·일어나는 연출 중 포함)</summary>
        public bool IsActive { get; private set; }

        /// <summary>일어나기 연출까지 끝남 (상태가 조작·화면 글자를 돌려줌)</summary>
        public event Action Left;

        #region DeskService - 책상 시점 연출·코덱스·머리·책상 시점 자리를 받음
        /// <param name="sequence">책상 시점 연출</param>
        /// <param name="codex">코덱스</param>
        /// <param name="head">옮길 머리 (플레이어 카메라)</param>
        /// <param name="seat">책상 시점 자리</param>
        public DeskService(DeskSequence sequence, CodexController codex, Transform head, Transform seat)
        {
            _sequence = sequence != null ? sequence : throw new ArgumentNullException(nameof(sequence));
            _codex = codex != null ? codex : throw new ArgumentNullException(nameof(codex));
            _head = head != null ? head : throw new ArgumentNullException(nameof(head));
            _seat = seat != null ? seat : throw new ArgumentNullException(nameof(seat));
        }
        #endregion

        #region Sit - 책상에 앉음: 커서를 풀고 머리를 책상 시점으로 옮긴 뒤 코덱스를 엶 (판 데이터·마법·룬 목록을 넘김, 일어나기 버튼은 TryLeave). 이미 앉았으면 무시
        /// <param name="run">이번 판 데이터 (코덱스가 씀)</param>
        /// <param name="spells">다중 룬 마법 목록</param>
        /// <param name="runes">룬 목록</param>
        public void Sit(RunEntry run, IReadOnlyList<SpellSO> spells, IReadOnlyList<RuneSO> runes)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (spells == null) throw new ArgumentNullException(nameof(spells));
            if (runes == null) throw new ArgumentNullException(nameof(runes));
            if (IsActive) return;
            IsActive = true;
            Cursor.lockState = CursorLockMode.None;
            _sequence.Enter(_head, _seat, () => _codex.Open(run, spells, runes, TryLeave));
        }
        #endregion

        #region TryLeave - 일어나기 시작: 코덱스를 덮음 (머리·코덱스 연출 중이거나 아직 앉는 중이면 무시)
        public void TryLeave()
        {
            if (!IsActive || _sequence.IsPlaying || _codex.IsBusy || !_codex.IsOpen) return;
            _codex.Close(OnCodexClosed);
        }
        #endregion

        #region HandleCancel - Esc: 책상이면 그리는 중엔 그리기 취소, 아니면 일어남. 책상이면 true (상태가 일시정지 메뉴를 안 엶)
        public bool HandleCancel()
        {
            if (!IsActive) return false;
            if (_codex.IsCasting) _codex.CancelCast();
            else TryLeave();
            return true;
        }
        #endregion

        #region ForceStop - 상태를 나갈 때: 책상이면 코덱스를 바로 덮고 머리를 바로 되돌림 (알림 없음)
        public void ForceStop()
        {
            if (!IsActive) return;
            _codex.ForceClose();
            _sequence.Stop();
            IsActive = false;
        }
        #endregion

        #region OnCodexClosed - 코덱스를 다 덮음: 커서를 잠그고 머리를 원래 자리로 되돌림
        void OnCodexClosed()
        {
            if (!IsActive) return;
            Cursor.lockState = CursorLockMode.Locked;
            _sequence.Exit(OnStood);
        }
        #endregion

        #region OnStood - 일어나기 연출이 끝남: 책상 시점을 끝내고 알림
        void OnStood()
        {
            IsActive = false;
            Left?.Invoke();
        }
        #endregion
    }
}
