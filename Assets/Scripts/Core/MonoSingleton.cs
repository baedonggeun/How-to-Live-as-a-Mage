using UnityEngine;

namespace Mage
{
    /// <summary>전역 매니저 공통 베이스. Boot 씬에 놓인 자신을 등록하고, 중복이면 지움</summary>
    /// <typeparam name="T">매니저 자신의 타입</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        /// <summary>등록된 인스턴스</summary>
        static T _instance;

        /// <summary>앱이 끝나는 중이면 true. 종료 중엔 없는 게 정상이라 에러를 안 냄</summary>
        static bool _isQuitting;

        /// <summary>등록된 인스턴스. 없으면 에러 로그 후 null (Boot 씬을 거치지 않았다는 뜻)</summary>
        public static T Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                    Debug.LogError($"[Singleton] {typeof(T).Name} 없음. Boot 씬에서 시작했는지 확인");
                return _instance;
            }
        }

        #region Awake - 첫 인스턴스면 등록, 이미 있으면 자기를 지움
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name} 중복이라 지움: {name}");
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;
            InitSingleton();
        }
        #endregion

        #region InitSingleton - 등록 직후 한 번. 매니저별 초기화 자리 (다른 매니저는 아직 없을 수 있음)
        protected virtual void InitSingleton() { }
        #endregion

        #region OnApplicationQuit - 종료 시작 표시
        protected virtual void OnApplicationQuit() => _isQuitting = true;
        #endregion

        #region OnDestroy - 등록된 인스턴스면 등록 해제
        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion
    }
}
