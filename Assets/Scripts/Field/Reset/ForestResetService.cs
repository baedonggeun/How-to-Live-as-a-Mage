using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 리셋 캡슐 (MonoBehaviour 아님. FieldState가 만들고 이음): 숲이 70% 넘게 빈 채 나갔으면(판 데이터 Pending) 이번 진입에 시체를 받아 모으고 연출을 틀어, 끝나면 시체를 원래 적 자리를 집으로 살린 뒤 Finished로 살린 적을 알림. 숲을 나갈 때 쓰러뜨린 비율을 보고 다음 리셋을 정함</summary>
    public sealed class ForestResetService
    {
        /// <summary>숲 적 자리 대비 쓰러뜨린 적이 이 비율 이상이면 리셋 (기획 70%)</summary>
        const float ResetRatio = 0.7f;

        /// <summary>숲 리셋 판 데이터</summary>
        readonly ForestResetEntry _data;

        /// <summary>리셋 연출</summary>
        readonly ForestResetSequence _sequence;

        /// <summary>현장 규칙 (살린 적이 씀)</summary>
        readonly FieldService _field;

        /// <summary>일어날 시체 (받은 순서)</summary>
        readonly List<EnemyController> _corpses = new();

        /// <summary>시체마다 돌아갈 집 (원래 적 자리)</summary>
        readonly Dictionary<EnemyController, Vector3> _homes = new();

        /// <summary>이번 진입에 리셋하는지 (만들 때 판 데이터로 정하고, 다 살리면 꺼짐)</summary>
        public bool IsResetting { get; private set; }

        /// <summary>연출이 끝나 시체가 다 일어남 (살린 적. 받는 쪽이 쓰러짐 구독·소탕 대상을 다시 셈)</summary>
        public event Action<IReadOnlyList<EnemyController>> Finished;

        #region ForestResetService - 판 데이터·연출·현장 규칙을 받고 이번 진입에 리셋하는지 정함
        /// <param name="data">숲 리셋 판 데이터</param>
        /// <param name="sequence">리셋 연출</param>
        /// <param name="field">현장 규칙</param>
        public ForestResetService(ForestResetEntry data, ForestResetSequence sequence, FieldService field)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _sequence = sequence != null ? sequence : throw new ArgumentNullException(nameof(sequence));
            _field = field ?? throw new ArgumentNullException(nameof(field));
            IsResetting = _data.Pending;
        }
        #endregion

        #region AddCorpse - 리셋하는 진입이면 시체와 돌아갈 집을 받아 둠 (아니면 무시)
        /// <param name="corpse">누운 적</param>
        /// <param name="home">살아나면 돌아갈 집 (원래 적 자리)</param>
        public void AddCorpse(EnemyController corpse, Vector3 home)
        {
            if (!IsResetting || corpse == null) return;
            _corpses.Add(corpse);
            _homes[corpse] = home;
        }
        #endregion

        #region Play - 리셋하는 진입이면 연출 시작 (끝나면 시체를 살리고 Finished). 아니면 무시
        /// <param name="portal">차원문</param>
        /// <param name="focus">카메라가 볼 기준 (플레이어. 가장 가까운 시체를 봄)</param>
        public void Play(Transform portal, Transform focus)
        {
            if (!IsResetting) return;
            _sequence.Play(portal, _corpses, focus, OnSequenceDone);
        }
        #endregion

        #region MarkLeaving - 숲을 나갈 때: 숲 적 자리 대비 쓰러뜨린 적이 70% 이상이면 다음 진입에 리셋
        /// <param name="deadCount">쓰러뜨린 숲 적 수</param>
        /// <param name="total">숲 적 자리 수 합</param>
        public void MarkLeaving(int deadCount, int total)
        {
            if (total > 0 && (float)deadCount / total >= ResetRatio) _data.Pending = true;
        }
        #endregion

        #region OnSequenceDone - 연출이 끝남: 시체를 집으로 살리고 리셋 표시를 지운 뒤 살린 적을 알림
        void OnSequenceDone()
        {
            // 살린 적
            var revived = new List<EnemyController>(_corpses.Count);
            foreach (EnemyController corpse in _corpses)
            {
                if (corpse == null) continue;
                corpse.Revive(_field, _homes.TryGetValue(corpse, out Vector3 home) ? home : corpse.transform.position);
                revived.Add(corpse);
            }
            _corpses.Clear();
            _homes.Clear();
            _data.Pending = false;
            IsResetting = false;
            Finished?.Invoke(revived);
        }
        #endregion
    }
}
