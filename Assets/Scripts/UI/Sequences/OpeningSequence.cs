using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Mage
{
    /// <summary>전초기지 첫 도착 역마차 연출: Timeline을 틀고 끝나면 알림. 연출 동안만 연출 카메라를 켬. 새로 누른 키를 정해진 시간 누르고 있으면 끝으로 건너뜀 (누르면 안내 글이 보임)</summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        /// <summary>연출 Timeline을 트는 디렉터 (Play On Awake 끔, Wrap Mode None. 인스펙터에서만 연결)</summary>
        [SerializeField] PlayableDirector _director;

        /// <summary>연출 카메라 (Camera + CinemachineBrain + AudioListener). 연출 동안만 켬 (인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _cinematicCamera;

        /// <summary>건너뛰기 안내 글 (연출 중 키를 누르면 보임. 인스펙터에서만 연결)</summary>
        [SerializeField] GameObject _skipHint;

        /// <summary>건너뛰려면 키를 누르고 있어야 하는 시간 (초, 게임 시간)</summary>
        [SerializeField, Min(0f)] float _skipHoldSeconds = 1f;

        /// <summary>연출이 끝남 (끝까지 봤거나 건너뜀). 알릴 땐 연출 카메라가 이미 꺼져 있음</summary>
        public event Action Finished;

        /// <summary>연출 중인지</summary>
        bool _playing;

        /// <summary>건너뛰기 키를 세기 시작했는지. 연출을 시작할 때 눌려 있던 키(로딩창 넘기기)를 다 뗀 뒤부터 셈</summary>
        bool _armed;

        /// <summary>건너뛰기 키를 이어서 누르고 있던 시간 (초)</summary>
        float _held;

        #region Awake - 연결 확인, 끝 알림 구독, 연출 카메라·안내 글 끄고 처음엔 꺼 둠 (Play에서 켬)
        void Awake()
        {
            if (_director == null || _cinematicCamera == null || _skipHint == null)
                Debug.LogError("[Opening] 디렉터·연출 카메라·건너뛰기 안내 연결 안 됨", this);
            if (_director != null) _director.stopped += OnStopped;
            if (_cinematicCamera != null) _cinematicCamera.SetActive(false);
            if (_skipHint != null) _skipHint.SetActive(false);
            enabled = false;
        }
        #endregion

        #region OnDestroy - 끝 알림 구독 해제
        void OnDestroy()
        {
            if (_director != null) _director.stopped -= OnStopped;
        }
        #endregion

        #region Play - 연출 카메라를 켜고 Timeline을 처음부터 틂. 첫 프레임(검은 화면)을 바로 적용. 디렉터가 없으면 바로 끝 알림
        public void Play()
        {
            _playing = true;
            if (_director == null)
            {
                Finish();
                return;
            }
            _armed = false;
            _held = 0f;
            enabled = true;
            if (_cinematicCamera != null) _cinematicCamera.SetActive(true);
            _director.time = 0d;
            _director.Play();
            _director.Evaluate();
        }
        #endregion

        #region Cancel - 끝 알림 없이 연출을 멈춤 (연출 중에 다른 상태로 갈 때. 씬이 내려가며 멈춤 알림이 와도 무시)
        public void Cancel()
        {
            if (!_playing) return;
            _playing = false;
            enabled = false;
            if (_director != null) _director.Stop();
        }
        #endregion

        #region Update - 게임 시간이 멈췄으면(일시정지) 안 셈. 시작 때 눌려 있던 키를 다 뗀 뒤부터, 키를 누르면 안내 글을 보이고 이어서 누른 시간이 차면 건너뜀
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            // 지금 키보드 키가 하나라도 눌려 있는지
            bool pressed = Keyboard.current != null && Keyboard.current.anyKey.isPressed;
            if (!_armed)
            {
                _armed = !pressed;
                return;
            }
            if (!pressed)
            {
                _held = 0f;
                return;
            }
            if (_skipHint != null) _skipHint.SetActive(true);
            _held += Time.deltaTime;
            if (_held >= _skipHoldSeconds) Skip();
        }
        #endregion

        #region Skip - 끝 프레임을 적용하고 멈춤 (멈춤 알림으로 끝 처리)
        void Skip()
        {
            _director.time = _director.duration;
            _director.Evaluate();
            _director.Stop();
        }
        #endregion

        #region OnStopped - Timeline이 끝나거나 멈춤 → 끝 처리
        /// <param name="director">멈춘 디렉터</param>
        void OnStopped(PlayableDirector director) => Finish();
        #endregion

        #region Finish - 연출 중이었으면 안내 글·연출 카메라를 끄고 끝 알림
        void Finish()
        {
            if (!_playing) return;
            _playing = false;
            enabled = false;
            if (_skipHint != null) _skipHint.SetActive(false);
            if (_cinematicCamera != null) _cinematicCamera.SetActive(false);
            Finished?.Invoke();
        }
        #endregion
    }
}
