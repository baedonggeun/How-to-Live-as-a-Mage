using UnityEngine;
using UnityEngine.Playables;

namespace Mage
{
    /// <summary>정해진 길을 따라 걷고 끝 점에 닿으면 사라짐. 연출이 정한 시각을 넘기거나 연출이 멈추면(끝남·건너뜀) 지금 자리에서 걷기 시작 (동승 용병이 마차에서 내려 여관으로 걸어감)</summary>
    public sealed class NpcWalkController : MonoBehaviour
    {
        /// <summary>걸어갈 점 (차례대로. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform[] _path;

        /// <summary>걷는 속도 (m/s)</summary>
        [SerializeField, Min(0f)] float _speed = 1.6f;

        /// <summary>몸을 돌리는 속도 (도/초)</summary>
        [SerializeField, Min(0f)] float _turnSpeed = 360f;

        /// <summary>걷기 시작을 맞출 연출 디렉터. 비면 켜지자마자 걸음 (인스펙터에서만 연결)</summary>
        [SerializeField] PlayableDirector _director;

        /// <summary>연출이 이 시각(초)을 넘기면 걷기 시작. 연출이 멈춰 있으면(끝남·건너뜀) 바로 (둘째 용병은 조금 늦게)</summary>
        [SerializeField, Min(0f)] float _startTime;

        /// <summary>다음에 갈 점 번호</summary>
        int _index;

        #region Awake - 연결 확인
        void Awake()
        {
            if (_path == null || _path.Length == 0) Debug.LogError("[Npc] 걸어갈 점 연결 안 됨", this);
        }
        #endregion

        #region Update - 게임 시간이 멈췄거나 연출이 시작 시각 전이면 안 움직임. 다음 점으로 걸으며 몸을 돌리고, 끝 점에 닿으면 끔
        void Update()
        {
            if (_path == null || _index >= _path.Length) return;
            // 이번 프레임 시간 (초)
            float dt = Time.deltaTime;
            if (dt <= 0f || IsWaiting()) return;
            // 다음 점 (높이는 지금 높이)
            Vector3 target = _path[_index].position;
            target.y = transform.position.y;
            // 다음 점까지 방향
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), _turnSpeed * dt);
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * dt);
            if ((transform.position - target).sqrMagnitude > 0.0004f) return;
            _index++;
            if (_index >= _path.Length) gameObject.SetActive(false);
        }
        #endregion

        #region IsWaiting - 연출이 돌고 있고 아직 시작 시각 전인지 (일시정지 중엔 연출 시간도 멈춰서 계속 기다림)
        bool IsWaiting() => _director != null && _director.state == PlayState.Playing && _director.time < _startTime;
        #endregion
    }
}
