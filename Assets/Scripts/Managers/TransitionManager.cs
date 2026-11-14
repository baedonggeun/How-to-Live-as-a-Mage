using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mage
{
    /// <summary>씬을 어떻게 옮길지 담당: 덮개, 내용 씬 언로드·Additive 로드. 언제 옮길지는 GameManager가 정함</summary>
    public sealed class TransitionManager : MonoSingleton<TransitionManager>
    {
        /// <summary>전체 화면을 가리고 클릭을 막는 검은 이미지 (정렬 순서 맨 위 Canvas 자식)</summary>
        [SerializeField] GameObject _cover;

        /// <summary>지금 떠 있는 내용 씬. Boot만 있을 땐 비어 있음</summary>
        Scene _contentScene;

        #region InitSingleton - 덮개 연결 확인
        protected override void InitSingleton()
        {
            if (_cover == null) Debug.LogError("[Transition] 덮개 연결 안 됨");
        }
        #endregion

        #region ShowCover - 덮개 켜기 (페이드 넣을 때 코루틴으로 바뀜)
        public void ShowCover()
        {
            if (_cover != null) _cover.SetActive(true);
        }
        #endregion

        #region HideCover - 덮개 끄기
        public void HideCover()
        {
            if (_cover != null) _cover.SetActive(false);
        }
        #endregion

        #region CanLoad - 씬을 읽을 수 있는지 확인 (빌드 목록 기준)
        /// <param name="sceneName">확인할 씬 이름</param>
        public bool CanLoad(string sceneName) => Application.CanStreamedLevelBeLoaded(sceneName);
        #endregion

        #region LoadScene - 지금 내용 씬을 내리고 새 씬을 Additive로 올린 뒤 활성 씬으로 지정
        /// <param name="sceneName">올릴 씬 이름</param>
        public IEnumerator LoadScene(string sceneName)
        {
            // 먼저 내려서 두 씬이 동시에 메모리에 있는 순간을 없앰 (덮개가 덮인 상태)
            if (_contentScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(_contentScene);
                // 언로드는 에셋을 안 지워서 직접 정리 (Resources 폴더와 상관없는 API)
                yield return Resources.UnloadUnusedAssets();
            }

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            _contentScene = SceneManager.GetSceneByName(sceneName);
            // 조명·스카이박스·런타임 생성 오브젝트가 내용 씬을 따르게
            SceneManager.SetActiveScene(_contentScene);
        }
        #endregion
    }
}
