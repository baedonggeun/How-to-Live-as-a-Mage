using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

namespace Mage
{
    /// <summary>씬을 어떻게 옮길지 담당: 로딩창(덮개·바·팁·넘기기 안내), 켤 때 로고, 내용 씬 언로드·Additive 로드, 글자 준비 대기, 덮개 동안 게임 멈춤. 언제 옮길지는 GameManager가 정함</summary>
    public sealed class TransitionManager : MonoSingleton<TransitionManager>
    {
        /// <summary>글자가 다 채워지길 기다리는 최대 시간 (초). 넘으면 에러를 남기고 그냥 진행 (로딩창에 갇히지 않게)</summary>
        const float TextTimeoutSeconds = 10f;

        /// <summary>넘기기 안내가 뜬 뒤 입력을 안 세는 시간 (초). 로딩 중 연타하던 키로 바로 넘어가지 않게</summary>
        const float ContinueGraceSeconds = 0.3f;

        /// <summary>씬 읽기가 끝났을 때 AsyncOperation.progress 값 (Unity가 정함). 그 뒤 1까지는 활성화</summary>
        const float SceneReadProgress = 0.9f;

        /// <summary>로딩창 (CoverCanvas/Cover). 늘 켜 두고 투명도로 숨김 → 숨긴 동안에도 팁 글자를 읽음</summary>
        [SerializeField] LoadingView _loading;

        /// <summary>켤 때 로고 화면 (CoverCanvas/Logo, 로딩창보다 위)</summary>
        [SerializeField] LogoSequence _logo;

        /// <summary>게임을 켤 때 로고를 틀었는지 (Title부터 시작). 첫 진입 로딩창에 팁을 깔지 정할 때 씀</summary>
        public bool LogoShown { get; private set; }

        /// <summary>지금 떠 있는 내용 씬. Boot만 있을 땐 비어 있음</summary>
        Scene _contentScene;

        /// <summary>로고 연출 코루틴. 안 틀었거나 끝났으면 null</summary>
        Coroutine _logoRoutine;

        /// <summary>로딩을 끝내고 로고가 끝나길 기다리는 중인지 (WaitForLogo 안). 그러면 로고가 끝나도 로고 화면(검은 바탕)을 그대로 둠 → 로딩창이 한 프레임 비치지 않고 바로 공개</summary>
        bool _waitingForLogo;

        /// <summary>바로 전에 보인 팁 위치 (DataManager 팁 목록 순서). 아직 없으면 -1</summary>
        int _lastTipIndex = -1;

        /// <summary>프로젝트 설정 Run In Background 값. 덮개가 걷히면 이 값으로 되돌림 (빌드만)</summary>
        bool _runInBackgroundDefault;

        #region InitSingleton - 로딩창·로고 연결 확인, Run In Background 원래 값 기억, 처음엔 둘 다 숨김
        protected override void InitSingleton()
        {
            if (_loading == null) Debug.LogError("[Transition] 로딩창 연결 안 됨");
            if (_logo == null) Debug.LogError("[Transition] 로고 연결 안 됨");
            // HideCover가 이 값으로 되돌려서 먼저 기억
            _runInBackgroundDefault = Application.runInBackground;
            HideCover();
        }
        #endregion

        #region ShowLogo - 로고 화면을 켜고 연출 시작 (게임을 켤 때 한 번)
        public void ShowLogo()
        {
            if (_logo == null) return;
            LogoShown = true;
            _logo.gameObject.SetActive(true);
            _logoRoutine = StartCoroutine(PlayLogo());
        }
        #endregion

        #region PlayLogo - 로고 연출을 끝까지 틂. 그동안 넘기기 버튼이 오면 남은 로고를 넘김. 끝났을 때 로딩이 남았으면 로고 화면을 꺼서 밑의 로딩창을 보임
        IEnumerator PlayLogo()
        {
            // 로고가 도는 동안만 듣는 넘기기 구독 (로딩 중에도 넘김). 블록을 나가면 해제
            using (InputSystem.onAnyButtonPress.Where(IsContinueControl).CallOnce(_ => _logo.Skip()))
            {
                yield return _logo.Play();
            }
            // 로딩이 다 끝나 기다리는 중이면 로고 화면을 둔 채 HideCover가 같이 끔
            if (!_waitingForLogo) _logo.gameObject.SetActive(false);
            _logoRoutine = null;
        }
        #endregion

        #region WaitForLogo - 로고 연출이 끝날(또는 넘길) 때까지 기다림. 안 틀었거나 끝났으면 바로 끝
        public IEnumerator WaitForLogo()
        {
            _waitingForLogo = true;
            while (_logoRoutine != null) yield return null;
            _waitingForLogo = false;
        }
        #endregion

        #region ShowCover - 팁 없이 로딩창 켜기 (게임을 켤 때 로고 밑, 첫 진입, 키 입력을 안 기다리는 전환)
        public void ShowCover()
        {
            if (_loading == null) return;
            _loading.TipRoot.SetActive(false);
            Open();
        }
        #endregion

        #region ShowLoading - 팁을 하나 골라 글자를 다 읽은 뒤 로딩창 켜기 (읽는 동안 팁은 투명, 이전 화면 그대로)
        public IEnumerator ShowLoading()
        {
            if (_loading == null) yield break;
            // 이번에 보일 팁. 목록이 비었거나 빈 칸이면 null
            TipSO tip = PickTip();
            if (tip != null)
            {
                // 팁 묶음은 HideCover가 꺼 둠. 꺼진 채 키를 넣고 켜야 이전 팁 키를 다시 안 읽음
                _loading.TipTitle.StringReference.SetReference(tip.Title.TableReference, tip.Title.TableEntryReference);
                _loading.TipBody.StringReference.SetReference(tip.Body.TableReference, tip.Body.TableEntryReference);
                _loading.TipPicture.sprite = tip.Picture;
                _loading.TipPicture.gameObject.SetActive(tip.Picture != null);
                // 덮개가 이미 보일 때(첫 진입) 자리 글자가 안 비치게 다 읽을 때까지 투명
                _loading.TipGroup.alpha = 0f;
                _loading.TipRoot.SetActive(true);
                yield return WaitForLocalizedTexts();
                _loading.TipGroup.alpha = 1f;
            }
            Open();
        }
        #endregion

        #region Open - 로딩창 보이기·클릭 막기, 바 비우기, 넘기기 안내 끄기, 게임 시간 멈춤, (빌드) 창이 뒤로 가도 계속 돎
        void Open()
        {
            SetProgress(0f);
            _loading.Prompt.enabled = false;
            _loading.Group.alpha = 1f;
            _loading.Group.blocksRaycasts = true;
            // 덮개 밑에서 물리·애니메이션·적이 안 돌게. 로딩창 연출은 실제 시간이라 그대로 돎
            Time.timeScale = 0f;
            // Alt+Tab 해도 로딩이 이어지게. 에디터는 MCP·사용자 설정과 안 싸우게 빌드에서만
            if (!Application.isEditor) Application.runInBackground = true;
        }
        #endregion

        #region HideCover - 게임 시간·Run In Background 되돌림, 로딩창(투명도 0·클릭 통과)과 로고 화면 숨김, 팁·넘기기 안내 끄기
        public void HideCover()
        {
            Time.timeScale = 1f;
            if (!Application.isEditor) Application.runInBackground = _runInBackgroundDefault;
            if (_logo != null) _logo.gameObject.SetActive(false);
            if (_loading == null) return;
            _loading.Group.alpha = 0f;
            _loading.Group.blocksRaycasts = false;
            _loading.Prompt.enabled = false;
            _loading.TipRoot.SetActive(false);
        }
        #endregion

        #region SetProgress - 로딩 바 채우기 (0~1, 범위 밖은 자름)
        /// <param name="progress">채울 비율</param>
        public void SetProgress(float progress)
        {
            if (_loading != null) _loading.ProgressFill.fillAmount = Mathf.Clamp01(progress);
        }
        #endregion

        #region WaitForLocalizedTexts - 켜진 다국어 글자가 다 채워질 때까지 기다림 (프레임마다 다시 찾아 늦게 켜진 글자도 잡음). 시간을 넘기면 에러 남기고 끝냄
        public IEnumerator WaitForLocalizedTexts()
        {
            // 이 시각이 지나면 그만 기다림 (실제 시간)
            float deadline = Time.realtimeSinceStartup + TextTimeoutSeconds;
            // 아직 테이블을 읽는 글자. 없으면 null
            LocalizeStringEvent pending;
            while ((pending = FindLoadingText()) != null)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Debug.LogError($"[Transition] {TextTimeoutSeconds}초 안에 글자를 못 채움: {pending.name}. 그냥 진행", pending);
                    yield break;
                }
                yield return null;
            }
        }
        #endregion

        #region FindLoadingText - 켜진 글자 중 테이블을 아직 읽는 것 하나. 없으면 null
        static LocalizeStringEvent FindLoadingText()
        {
            foreach (var text in Object.FindObjectsByType<LocalizeStringEvent>(FindObjectsSortMode.None))
            {
                // 이 글자의 테이블 읽기 작업. 키가 비었거나 꺼져 있으면 빈 핸들
                var load = text.StringReference.CurrentLoadingOperationHandle;
                if (load.IsValid() && !load.IsDone) return text;
            }
            return null;
        }
        #endregion

        #region WaitForContinue - 끝 소리를 내고 넘기기 안내를 깜빡이며, 0.3초 뒤부터 넘기기 버튼(IsContinueControl)을 새로 누를 때까지 기다림
        public IEnumerator WaitForContinue()
        {
            if (_loading == null) yield break;
            // 비어 있으면 AudioManager가 무시함
            AudioManager.Instance.Play(_loading.ReadySound);
            _loading.Prompt.enabled = true;
            // 로딩 중 연타하던 키가 안내가 뜨자마자 들어오지 않게 잠깐 안 셈 (덮개 동안 timeScale 0이라 실제 시간)
            yield return new WaitForSecondsRealtime(ContinueGraceSeconds);
            // 버튼이 눌렸는지 (아래 구독 콜백이 켬)
            bool pressed = false;
            // 다음에 눌리는 넘기기 버튼 하나만 듣는 구독. 블록을 나가면 해제
            using (InputSystem.onAnyButtonPress.Where(IsContinueControl).CallOnce(_ => pressed = true))
            {
                while (!pressed) yield return null;
            }
        }
        #endregion

        #region IsContinueControl - 넘기기로 셀 버튼인지. 마우스·패드 버튼은 다 셈. 키보드는 수정자 키(Shift·Ctrl·Alt·Win), 수정자를 누른 채 누른 키(Alt+Tab, Shift+Tab = Steam 오버레이), 스크린샷 키(F12 = Steam, PrintScreen)를 뺌
        /// <param name="control">새로 눌린 버튼</param>
        public static bool IsContinueControl(InputControl control)
        {
            // 키보드 키가 아니면(마우스·패드 버튼) 셈
            if (control is not KeyControl key) return true;
            if (key.keyCode is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt or Key.LeftMeta or Key.RightMeta
                or Key.F12 or Key.PrintScreen) return false;
            // 이 키가 붙은 키보드. 키보드가 아니면 수정자를 못 봐서 그냥 셈
            if (key.device is not Keyboard keyboard) return true;
            return !(keyboard.shiftKey.isPressed || keyboard.ctrlKey.isPressed || keyboard.altKey.isPressed
                || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed);
        }
        #endregion

        #region PickTip - 팁 목록에서 무작위 하나. 둘 이상이면 바로 전 팁은 뺌. 목록이 비면 null
        TipSO PickTip()
        {
            // 팁 목록 (DataManager 연결 순서)
            IReadOnlyList<TipSO> tips = DataManager.Instance.Tips;
            if (tips.Count == 0) return null;
            // 바로 전 팁을 뺄지 (팁이 하나뿐이면 못 뺌)
            bool skipLast = tips.Count > 1 && _lastTipIndex >= 0 && _lastTipIndex < tips.Count;
            // 고른 위치. 바로 전 팁을 빼면 한 칸 적게 뽑고, 그 자리부터는 한 칸 뒤로 밂
            int index = Random.Range(0, skipLast ? tips.Count - 1 : tips.Count);
            if (skipLast && index >= _lastTipIndex) index++;
            _lastTipIndex = index;
            return tips[index];
        }
        #endregion

        #region CanLoad - 씬을 읽을 수 있는지 확인 (빌드 목록 기준)
        /// <param name="sceneName">확인할 씬 이름</param>
        public bool CanLoad(string sceneName) => Application.CanStreamedLevelBeLoaded(sceneName);
        #endregion

        #region LoadScene - 지금 내용 씬을 내리고 새 씬을 Additive로 올린 뒤 활성 씬으로 지정. 올리는 동안 바를 0 → progressEnd로 채움
        /// <param name="sceneName">올릴 씬 이름</param>
        /// <param name="progressEnd">씬이 다 올라왔을 때 바 위치 (0~1)</param>
        public IEnumerator LoadScene(string sceneName, float progressEnd)
        {
            // 먼저 내려서 두 씬이 동시에 메모리에 있는 순간을 없앰 (덮개가 덮인 상태. 그동안 바는 0)
            if (_contentScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(_contentScene);
                // 언로드는 에셋을 안 지워서 직접 정리 (Resources 폴더와 상관없는 API)
                yield return Resources.UnloadUnusedAssets();
            }

            // 씬 올리기 작업. progress 0~0.9가 읽기, 0.9 → 1이 활성화(Awake·OnEnable, 한 프레임)
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"[Transition] {sceneName} 씬 로드 시작 실패");
                yield break;
            }
            while (!load.isDone)
            {
                SetProgress(load.progress / SceneReadProgress * progressEnd);
                yield return null;
            }
            SetProgress(progressEnd);
            _contentScene = SceneManager.GetSceneByName(sceneName);
            // 조명·스카이박스·런타임 생성 오브젝트가 내용 씬을 따르게
            SceneManager.SetActiveScene(_contentScene);
        }
        #endregion
    }
}
