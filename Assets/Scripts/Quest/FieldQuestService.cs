using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>의뢰 캡슐의 숲 쪽 (MonoBehaviour 아님. FieldState가 만들고 이음): 의뢰 판 데이터를 받아, 받은 의뢰의 소탕 대상인 적을 따라가다 다 쓰러지면 목표를 끝냄(Cleared 알림). 판 시각을 받아 못 끝낸 의뢰의 기한이 지나면 의뢰를 비움(Expired 알림). 지도 목표 자리(남은 대상 집의 가운데)를 알려 줌</summary>
    public sealed class FieldQuestService
    {
        /// <summary>의뢰 판 데이터</summary>
        readonly QuestEntry _data;

        /// <summary>살아 있는 소탕 대상</summary>
        readonly List<EnemyController> _targets = new();

        /// <summary>이번 숲에서 기한 지남을 알렸는지</summary>
        bool _expired;

        /// <summary>받은 의뢰가 있는지</summary>
        public bool HasQuest => _data.Current != null;

        /// <summary>받은 의뢰의 목표를 끝냈는지 (의뢰가 없으면 false)</summary>
        public bool IsCleared => _data.Current != null && _data.Cleared;

        /// <summary>받은 의뢰가 있고 아직 목표를 안 끝냈는지</summary>
        bool IsActive => _data.Current != null && !_data.Cleared;

        /// <summary>소탕 대상을 다 쓰러뜨려 목표를 끝냄 (끝낸 의뢰)</summary>
        public event Action<QuestSO> Cleared;

        /// <summary>기한이 지나 의뢰가 실패함 (실패한 의뢰. 게시판에 다시 붙음)</summary>
        public event Action<QuestSO> Expired;

        #region FieldQuestService - 의뢰 판 데이터를 받음
        /// <param name="data">의뢰 판 데이터</param>
        public FieldQuestService(QuestEntry data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }
        #endregion

        #region Track - 받은 의뢰의 소탕 대상이면 따라감 (아니면 무시)
        /// <param name="enemy">놓인 적</param>
        public void Track(EnemyController enemy)
        {
            if (!IsActive || enemy == null || _data.Current.Target == null || enemy.Monster != _data.Current.Target || _targets.Contains(enemy)) return;
            _targets.Add(enemy);
        }
        #endregion

        #region Untrack - 쓰러진 적을 뺌. 마지막 소탕 대상이었으면 목표를 끝내고 알림
        /// <param name="enemy">쓰러진 적</param>
        public void Untrack(EnemyController enemy)
        {
            if (!_targets.Remove(enemy) || _targets.Count > 0 || !IsActive) return;
            _data.Cleared = true;
            Cleared?.Invoke(_data.Current);
        }
        #endregion

        #region SettleIfEmpty - 받은 의뢰가 진행 중인데 남은 소탕 대상이 없으면 알림 없이 목표를 끝냄 (들어올 때·리셋 뒤)
        public void SettleIfEmpty()
        {
            if (IsActive && _targets.Count == 0) _data.Cleared = true;
        }
        #endregion

        #region Tick - 판 시각을 받아, 못 끝낸 의뢰의 기한이 지났으면 한 번만 의뢰·기한·소탕 대상을 비우고 알림
        /// <param name="now">지금 판 시각 (초)</param>
        public void Tick(float now)
        {
            if (_expired || !IsActive || _data.Deadline <= 0f || now < _data.Deadline) return;
            _expired = true;
            // 실패한 의뢰
            QuestSO quest = _data.Current;
            _data.Current = null;
            _data.Deadline = 0f;
            _targets.Clear();
            Expired?.Invoke(quest);
        }
        #endregion

        #region TargetCenter - 남은 소탕 대상 집의 가운데 (없으면 null)
        public Vector3? TargetCenter()
        {
            // 남은 대상 집의 합과 수
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (EnemyController enemy in _targets)
            {
                if (enemy == null) continue;
                sum += enemy.Home;
                count++;
            }
            return count > 0 ? sum / count : null;
        }
        #endregion
    }
}
