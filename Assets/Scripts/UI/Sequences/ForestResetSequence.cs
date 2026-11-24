using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mage
{
    /// <summary>숲 리셋 연출 (약 6초, 넘기기 없음): 연출 카메라로 차원문을 봄 → 차원문 맥동 3번 → 마력 구가 퍼짐 → 차원문에서 시체마다 마력 줄기가 뻗어 스며듦 → 가장 가까운 시체 쪽 카메라에서 시체들이 일어남 → 카메라를 끄고 알림. 게임 규칙은 안 건드림 (일어난 적의 AI는 FieldState가 켬)</summary>
    public sealed class ForestResetSequence : MonoBehaviour
    {
        /// <summary>맥동 시간 (초)</summary>
        const float PulseSeconds = 1.5f;

        /// <summary>맥동 횟수</summary>
        const int PulseCount = 3;

        /// <summary>맥동 크기 (배율 더하기)</summary>
        const float PulseScale = 0.15f;

        /// <summary>마력 구가 퍼지는 시간 (초)</summary>
        const float BurstSeconds = 1f;

        /// <summary>마력 줄기가 뻗는 시간 (초)</summary>
        const float StreamSeconds = 1.5f;

        /// <summary>일어나는 시간 (초)</summary>
        const float RiseSeconds = 1f;

        /// <summary>줄기를 그릴 시체 최대 수 (많으면 앞에서부터)</summary>
        const int MaxStreams = 40;

        /// <summary>차원문 가운데 높이 (m)</summary>
        const float PortalCenterHeight = 4f;

        /// <summary>연출 카메라 (꺼 둠, 플레이어 카메라보다 Depth 높음, AudioListener 없음. 인스펙터에서만 연결)</summary>
        [SerializeField] Camera _camera;

        /// <summary>차원문을 볼 자리 (차원문 기준 월드 오프셋)</summary>
        [SerializeField] Vector3 _portalView = new(0f, 10f, -24f);

        /// <summary>시체를 볼 자리 (시체 기준 월드 오프셋)</summary>
        [SerializeField] Vector3 _corpseView = new(0f, 4f, -9f);

        /// <summary>퍼지는 마력 구 (꺼 둔 자식, 지름 1. 인스펙터에서만 연결)</summary>
        [SerializeField] Transform _burst;

        /// <summary>마력 구가 퍼지는 반지름 (m)</summary>
        [SerializeField, Min(1f)] float _burstRadius = 60f;

        /// <summary>마력 줄기 프리팹 (ManaStream.prefab, 점 2개. 인스펙터에서만 연결)</summary>
        [SerializeField] LineRenderer _streamPrefab;

        /// <summary>그리는 줄기 (끝나면 지움)</summary>
        readonly List<LineRenderer> _streams = new();

        /// <summary>도는 연출. 없으면 null</summary>
        Coroutine _routine;

        /// <summary>연출 중인지</summary>
        public bool IsPlaying => _routine != null;

        #region Awake - 연결 확인, 카메라·마력 구를 끔
        void Awake()
        {
            if (_camera == null || _burst == null || _streamPrefab == null) Debug.LogError("[Reset] 카메라·마력 구·줄기 연결 안 됨", this);
            if (_camera != null) _camera.gameObject.SetActive(false);
            if (_burst != null) _burst.gameObject.SetActive(false);
        }
        #endregion

        #region Play - 차원문·시체를 받아 연출 시작, 끝나면 onDone (도는 중이면 무시)
        /// <param name="portal">차원문</param>
        /// <param name="corpses">일어날 시체 (누운 적)</param>
        /// <param name="focus">카메라가 볼 기준 (플레이어. 가장 가까운 시체를 봄)</param>
        /// <param name="onDone">끝나면 할 일</param>
        public void Play(Transform portal, IReadOnlyList<EnemyController> corpses, Transform focus, Action onDone)
        {
            if (portal == null) throw new ArgumentNullException(nameof(portal));
            if (corpses == null) throw new ArgumentNullException(nameof(corpses));
            if (_routine != null) return;
            _routine = StartCoroutine(Run(portal, corpses, focus, onDone));
        }
        #endregion

        #region Run - 맥동 → 마력 구 → 줄기 → 일어남 → 정리
        /// <param name="portal">차원문</param>
        /// <param name="corpses">일어날 시체</param>
        /// <param name="focus">카메라가 볼 기준</param>
        /// <param name="onDone">끝나면 할 일</param>
        IEnumerator Run(Transform portal, IReadOnlyList<EnemyController> corpses, Transform focus, Action onDone)
        {
            // 차원문 가운데
            Vector3 center = portal.position + Vector3.up * PortalCenterHeight;
            _camera.gameObject.SetActive(true);
            _camera.transform.position = portal.position + _portalView;
            _camera.transform.LookAt(center);
            yield return Pulse(portal);
            yield return Burst(center);
            yield return Streams(center, corpses);
            // 카메라가 볼 시체 (기준에서 가장 가까운 것)
            EnemyController nearest = Nearest(corpses, focus != null ? focus.position : center);
            if (nearest != null)
            {
                _camera.transform.position = nearest.transform.position + _corpseView;
                _camera.transform.LookAt(nearest.transform.position);
            }
            yield return Rise(corpses);
            _camera.gameObject.SetActive(false);
            _routine = null;
            onDone?.Invoke();
        }
        #endregion

        #region Pulse - 차원문 크기를 맥동 횟수만큼 키웠다 줄임
        /// <param name="portal">차원문</param>
        IEnumerator Pulse(Transform portal)
        {
            // 원래 크기
            Vector3 baseScale = portal.localScale;
            for (float t = 0f; t < PulseSeconds; t += Time.deltaTime)
            {
                portal.localScale = baseScale * (1f + PulseScale * Mathf.Abs(Mathf.Sin(t / PulseSeconds * PulseCount * Mathf.PI)));
                yield return null;
            }
            portal.localScale = baseScale;
        }
        #endregion

        #region Burst - 차원문 가운데에서 마력 구가 퍼짐
        /// <param name="center">차원문 가운데</param>
        IEnumerator Burst(Vector3 center)
        {
            _burst.position = center;
            _burst.gameObject.SetActive(true);
            for (float t = 0f; t < BurstSeconds; t += Time.deltaTime)
            {
                _burst.localScale = Vector3.one * Mathf.Lerp(1f, _burstRadius * 2f, t / BurstSeconds);
                yield return null;
            }
            _burst.gameObject.SetActive(false);
        }
        #endregion

        #region Streams - 차원문에서 시체마다 줄기를 뻗음 (끝이 시체까지 감), 다 뻗으면 지움
        /// <param name="center">차원문 가운데</param>
        /// <param name="corpses">시체</param>
        IEnumerator Streams(Vector3 center, IReadOnlyList<EnemyController> corpses)
        {
            for (int i = 0; i < corpses.Count && _streams.Count < MaxStreams; i++)
            {
                if (corpses[i] == null) continue;
                // 줄기 하나
                LineRenderer stream = Instantiate(_streamPrefab, transform);
                stream.positionCount = 2;
                stream.SetPosition(0, center);
                stream.SetPosition(1, center);
                _streams.Add(stream);
            }
            for (float t = 0f; t < StreamSeconds; t += Time.deltaTime)
            {
                // 뻗은 비율
                float k = t / StreamSeconds;
                for (int i = 0, s = 0; i < corpses.Count && s < _streams.Count; i++)
                {
                    if (corpses[i] == null) continue;
                    _streams[s++].SetPosition(1, Vector3.Lerp(center, corpses[i].transform.position, k));
                }
                yield return null;
            }
            foreach (LineRenderer stream in _streams)
                if (stream != null) Destroy(stream.gameObject);
            _streams.Clear();
        }
        #endregion

        #region Rise - 시체들이 누운 모습에서 선 모습으로
        /// <param name="corpses">시체</param>
        IEnumerator Rise(IReadOnlyList<EnemyController> corpses)
        {
            for (float t = 0f; t < RiseSeconds; t += Time.deltaTime)
            {
                foreach (EnemyController corpse in corpses)
                    if (corpse != null) corpse.SetRise(t / RiseSeconds);
                yield return null;
            }
            foreach (EnemyController corpse in corpses)
                if (corpse != null) corpse.SetRise(1f);
        }
        #endregion

        #region Nearest - 기준점에서 가장 가까운 시체 (없으면 null)
        /// <param name="corpses">시체</param>
        /// <param name="from">기준점</param>
        static EnemyController Nearest(IReadOnlyList<EnemyController> corpses, Vector3 from)
        {
            // 찾은 시체와 거리 제곱
            EnemyController best = null;
            float bestSq = float.MaxValue;
            foreach (EnemyController corpse in corpses)
            {
                if (corpse == null) continue;
                // 거리 제곱
                float sq = (corpse.transform.position - from).sqrMagnitude;
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = corpse;
            }
            return best;
        }
        #endregion

        #region OnDisable - 상태를 나가면 연출을 멈추고 카메라·구·줄기를 정리
        void OnDisable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (_camera != null) _camera.gameObject.SetActive(false);
            if (_burst != null) _burst.gameObject.SetActive(false);
            foreach (LineRenderer stream in _streams)
                if (stream != null) Destroy(stream.gameObject);
            _streams.Clear();
        }
        #endregion
    }
}
