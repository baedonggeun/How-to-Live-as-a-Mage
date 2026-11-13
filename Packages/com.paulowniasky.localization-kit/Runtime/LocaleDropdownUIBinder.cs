using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PaulowniaSky.LocalizationKit
{
    /// <summary>
    /// 같은 오브젝트의 TMP 드롭다운에 언어 목록을 채우고, 고르면 언어를 바꿈.
    /// 다른 곳(저장값, 다른 UI)에서 언어가 바뀌어도 드롭다운 표시를 맞춤
    /// </summary>
    [DisallowMultipleComponent]
    public class LocaleDropdownUIBinder : MonoBehaviour
    {
        /// <summary>언어 목록을 보여 줄 드롭다운 (같은 오브젝트)</summary>
        TMP_Dropdown m_Dropdown;

        #region Awake - 드롭다운 찾기
        void Awake()
        {
            m_Dropdown = GetComponent<TMP_Dropdown>();
            if (m_Dropdown == null)
            {
                Debug.LogError($"[{nameof(LocaleDropdownUIBinder)}] {name}에 TMP 드롭다운이 없음", this);
                enabled = false;
            }
        }
        #endregion

        #region OnEnable - Localization이 준비되면 목록 채우기
        void OnEnable()
        {
            // 준비 전엔 못 누르게
            m_Dropdown.interactable = false;

            // 현재 언어를 얻는 작업 (끝나면 언어 목록도 준비된 상태)
            var operation = LocalizationSettings.SelectedLocaleAsync;
            if (operation.IsDone)
                Setup();
            else
                operation.Completed += OnLocalizationReady;
        }
        #endregion

        #region OnDisable - 구독 해제
        void OnDisable()
        {
            if (m_Dropdown != null)
                m_Dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }
        #endregion

        #region OnLocalizationReady - 초기화가 끝나면 목록 채우기
        /// <param name="operation">끝난 현재 언어 작업</param>
        void OnLocalizationReady(AsyncOperationHandle<Locale> operation)
        {
            // 기다리는 사이 꺼졌거나 지워졌으면 무시 (다시 켜지면 OnEnable이 처리함)
            if (this == null || !isActiveAndEnabled)
                return;

            Setup();
        }
        #endregion

        #region Setup - 언어 목록 채우고 현재 언어 표시, 구독
        void Setup()
        {
            // 프로젝트에 등록된 언어 (Localization Settings 순서)
            var locales = LocalizationSettings.AvailableLocales.Locales;
            // 드롭다운에 보일 언어 이름
            var names = new List<string>(locales.Count);
            foreach (var locale in locales)
                names.Add(locale.LocaleName);

            m_Dropdown.ClearOptions();
            m_Dropdown.AddOptions(names);
            ShowLocale(LocalizationSettings.SelectedLocale);

            // 두 번 불려도 한 번만 구독되게 먼저 뺌
            m_Dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            m_Dropdown.onValueChanged.AddListener(OnDropdownChanged);
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

            // 언어가 하나뿐이면 고를 게 없음
            m_Dropdown.interactable = locales.Count > 1;
        }
        #endregion

        #region OnDropdownChanged - 고른 언어로 바꾸기
        /// <param name="index">고른 항목 번호</param>
        void OnDropdownChanged(int index)
        {
            // 프로젝트에 등록된 언어
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (index < 0 || index >= locales.Count)
                return;

            LocalizationSettings.SelectedLocale = locales[index];
        }
        #endregion

        #region OnSelectedLocaleChanged - 다른 곳에서 바뀐 언어를 표시에 반영
        /// <param name="locale">바뀐 언어</param>
        void OnSelectedLocaleChanged(Locale locale)
        {
            ShowLocale(locale);
        }
        #endregion

        #region ShowLocale - 드롭다운 표시를 언어에 맞춤 (이벤트 안 냄)
        /// <param name="locale">표시할 언어</param>
        void ShowLocale(Locale locale)
        {
            // 목록에서 이 언어 위치 (없으면 -1)
            var index = LocalizationSettings.AvailableLocales.Locales.IndexOf(locale);
            if (index >= 0)
                m_Dropdown.SetValueWithoutNotify(index);
        }
        #endregion
    }
}
