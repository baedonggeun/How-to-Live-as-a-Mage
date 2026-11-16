# Changelog

## [0.1.3] - 2026-11-16
### 고침
- 에디터를 닫으면 Dynamic 폰트 에셋에 커닝·합자 기록이 남아 git diff가 생기던 문제. TMP가 종료 때 글자표·아틀라스만 비워서, 저장 직전 비우기가 이미 빈 걸로 보고 건너뛰었음 (`DynamicFontSaveHandler`)

## [0.1.2] - 2026-11-14
### 추가
- 설명서 맨 앞에 '처음 쓰는 법' (설치부터 언어 바꾸기까지 7단계)

## [0.1.1] - 2026-11-14
### 추가
- 설명서 (`Documentation~/index.md`)

## [0.1.0] - 2026-11-13
### 추가
- 패키지 뼈대, Noto 폰트 5종
- 셋업 메뉴: 설정·13개 언어·테이블·폰트 에셋·TMP 설정·텍스트 프리팹 (`LocalizationSetupDirector`)
- 언어별 폰트 교체 (`LocaleFontSetSO`, `LocaleFontUIBinder`)
- 언어 선택 드롭다운, 언어 이름은 그 언어로 표시 (`LocaleDropdownUIBinder`)
- 저장 직전 Dynamic 폰트 아틀라스 비우기 (`DynamicFontSaveHandler`)
- CSV 내보내기·가져오기 (`LocalizationCsvDirector`)
- 누락 번역·누락 글자 검사 (`MissingTranslationDetector`, `GlyphCoverageDetector`)
- 샘플: 언어 선택 드롭다운 프리팹 (`Samples~/LanguageDropdown`)
