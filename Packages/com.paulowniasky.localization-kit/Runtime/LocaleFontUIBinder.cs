using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 언어가 바뀌면 같은 오브젝트의 TMP 텍스트 폰트를 LocaleFontSetSO에서 골라 바꿈.
    /// Play 중에만 동작. 에디터 모드에선 안 바꿔서 언어 미리보기 때마다 씬 파일이 바뀌지 않음
    /// </summary>
    [DisallowMultipleComponent]
    public class LocaleFontUIBinder : MonoBehaviour
    {
        /// <summary>이 텍스트가 쓸 폰트 세트 (본문, 제목 등)</summary>
        [SerializeField] private LocaleFontSetSO m_FontSet;

        /// <summary>폰트를 바꿀 TMP 텍스트 (같은 오브젝트)</summary>
        private TMP_Text m_Text;

        #region Awake - TMP 텍스트 찾기
        private void Awake()
        {
            m_Text = GetComponent<TMP_Text>();
            if (m_Text == null)
            {
                Debug.LogError($"[{nameof(LocaleFontUIBinder)}] {name}에 TMP 텍스트가 없음", this);
                enabled = false;
                return;
            }

            if (m_FontSet == null)
                Debug.LogWarning($"[{nameof(LocaleFontUIBinder)}] {name}의 Font Set이 비어 있음", this);
        }
        #endregion

        #region OnEnable - 언어 변경 구독, 현재 언어 적용
        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += Apply;

            // 현재 언어를 얻는 작업 (처음엔 Localization 초기화가 끝날 때까지 기다림)
            var operation = LocalizationSettings.SelectedLocaleAsync;
            if (operation.IsDone)
                Apply(operation.Result);
            else
                operation.Completed += OnSelectedLocaleReady;
        }
        #endregion

        #region OnDisable - 언어 변경 구독 해제
        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= Apply;
        }
        #endregion

        #region OnSelectedLocaleReady - 초기화가 끝나면 현재 언어 적용
        /// <param name="operation">끝난 현재 언어 작업</param>
        void OnSelectedLocaleReady(AsyncOperationHandle<Locale> operation)
        {
            // 기다리는 사이 꺼졌거나 지워졌으면 무시 (다시 켜지면 OnEnable이 적용함)
            if (this == null || !isActiveAndEnabled)
                return;

            Apply(operation.Result);
        }
        #endregion

        #region Apply - 언어에 맞는 폰트·머티리얼 적용
        /// <param name="locale">적용할 언어</param>
        void Apply(Locale locale)
        {
            if (locale == null || m_FontSet == null)
                return;

            // 언어 코드 (ko, ja 등)
            var code = locale.Identifier.Code;
            // 이 언어에 쓸 폰트
            var font = m_FontSet.GetFont(code);
            if (font == null)
                return;

            // 이 언어에 쓸 머티리얼 프리셋 (없으면 null)
            var material = m_FontSet.GetMaterial(code);
            m_Text.font = font;
            // 프리셋은 폰트 아틀라스마다 따로라서, 폰트를 바꾸면 머티리얼도 그 폰트 것으로 맞춤
            m_Text.fontSharedMaterial = material != null ? material : font.material;
        }
        #endregion
    }
}