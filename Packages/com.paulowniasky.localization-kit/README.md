# PaulowniaSky Localization Kit

Unity Localization(`com.unity.localization`) 위에 얹는 다국어 키트.

자세한 사용법: [설명서](Documentation~/index.md)

## 기능 (v0.1)
- 셋업 메뉴: 설정·언어·테이블·폰트 에셋·TMP 설정·텍스트 프리팹 한 번에 생성
- 언어별 폰트 교체
- 언어 선택 드롭다운 (샘플 프리팹)
- 텍스트 시트(CSV) 내보내기·가져오기
- 누락 번역·누락 글자 검사
- 언어 순서는 Project Settings > Localization > Available Locales에서 드래그로 바꿈. 셋업 메뉴는 새로 만드는 언어만 기존 언어 뒤에 붙임
- Dynamic 폰트 에셋은 저장 직전에 아틀라스를 비움 (Clear Dynamic Data On Build가 켜진 것만). 에디터에서 그린 글자가 파일에 안 남아서 git diff가 생기지 않음

## 메뉴 (Tools > PaulowniaSky > Localization Kit)
| 메뉴 | 하는 일 |
|---|---|
| Setup Project | 설정·언어·테이블·폰트 에셋·TMP 설정·텍스트 프리팹 생성. 다시 실행해도 없는 것만 만듦 |
| Export Tables to CSV | String Table 전부를 `{출력 폴더}/CSV/{테이블 이름}.csv`로 내보냄 |
| Import Tables from CSV | 같은 경로의 CSV를 테이블에 가져옴 |
| Check Missing Translations | 언어마다 값이 없거나 빈 키를 콘솔에 보고 |
| Check Missing Glyphs | 언어마다 번역문 글자가 그 언어 폰트·폴백 폰트에 있는지 보고 (없으면 화면에 □) |

## 텍스트 작업 흐름 (CSV)
1. Export Tables to CSV → 엑셀·Google 시트에서 편집 → 저장 → Import Tables from CSV
2. 열: `Key`, `Comment`(번역가용 설명), 언어 코드(`ko`, `en`, `ja` …). 머리글 이름으로 찾아서 열 순서는 상관없음
3. 새 키는 행을 추가하면 됨. CSV에 없는 키는 테이블에서 안 지워짐
4. 빈 칸은 그 언어 번역을 지움 → 실행 중엔 폴백 언어로 보이고, Check Missing Translations에 잡힘
5. 엑셀은 "CSV UTF-8(쉼표로 분리)"로 저장. 그냥 "CSV"는 한글이 깨짐
6. 가져온 뒤 Check Missing Glyphs로 폰트에 없는 글자 확인

## 샘플: 언어 선택 드롭다운
1. Package Manager > PaulowniaSky Localization Kit > Samples > Language Dropdown > Import
2. `Assets/Samples/PaulowniaSky Localization Kit/{버전}/Language Dropdown/LanguageDropdown.prefab`을 Canvas 아래에 놓음
3. 스프라이트·색·크기·폰트를 게임 UI에 맞게 바꿈. 같은 오브젝트의 `LocaleDropdownUIBinder`만 남기면 됨
- 언어 이름은 그 언어로 표시 (Locale의 Native Name 메타데이터)
- 폰트는 비워 둠 → 프로젝트의 TMP 기본 폰트를 씀. 목록의 다른 문자는 TMP 전역 폴백 폰트로 그림
- 씬에 EventSystem 필요 (Input System이면 Input System UI Input Module)

## 무시해도 되는 경고
- `Group '...' has bundle naming style 'OnlyHash' ...` (Localization이 띄움): Addressables가 Windows 경로 길이 제한을 피하려고 일부러 해시만 쓴 짧은 이름(OnlyHash)을 씀. 경고 문구의 "긴 파일 이름"은 OnlyHash에 해당 안 됨

## 설치
1. git-lfs 설치 (폰트가 Git LFS). 비공개 레포라 이 레포에 접근되는 GitHub 계정으로 git 로그인돼 있어야 함
2. Package Manager > + > Install package from git URL:
   `https://github.com/baedonggeun/paulowniasky-localization-kit.git#v0.1.3`
   (또는 `Packages/manifest.json` dependencies에 `"com.paulowniasky.localization-kit": "https://github.com/baedonggeun/paulowniasky-localization-kit.git#v0.1.3"`)
3. Tools > PaulowniaSky > Localization Kit > Setup Project. TMP 기본 리소스가 없으면 먼저 임포트하니까 끝나면 한 번 더 실행

## 개발
원본은 How-to-Live-as-a-Mage 레포의 `Packages/com.paulowniasky.localization-kit` (embedded). 고친 뒤 그 레포에서 `git subtree push --prefix=Packages/com.paulowniasky.localization-kit kit main` → 새 버전 태그.
이 레포에 직접 커밋하면 subtree push가 막힘.

## 폰트 라이선스
`Fonts/`의 Noto 폰트는 SIL Open Font License 1.1. 라이선스 파일 동봉.
