# PaulowniaSky Localization Kit 설명서

Unity Localization(`com.unity.localization`) 위에 얹는 다국어 키트 사용법.

- 필요: Unity 6000.3 이상. Localization 1.5.14와 uGUI(TextMeshPro) 2.0.0은 의존성으로 같이 들어옴
- 메뉴는 전부 `Tools > PaulowniaSky > Localization Kit` 아래
- 경로는 출력 폴더 기본값(`Assets/Localization`) 기준
- 처음이면 '처음 쓰는 법'만 따라 하면 됨. 1~10장은 자세한 설명

## 처음 쓰는 법
"확인" 버튼 글자 하나를 처음부터 끝까지 만드는 순서. 이것만 따라 하면 기본은 다 됨.

**이 키트가 하는 일**: 화면 글자마다 이름표(키)를 붙이고, 이름표마다 언어별 번역을 적어 둠. 그러면 게임이 고른 언어에 맞게 글자와 폰트를 알아서 바꿈.

### 처음 한 번만
**1. 설치**
- 먼저 git-lfs를 설치하고 GitHub에 로그인 (키트 레포가 비공개라서)
- Window > Package Manager → 왼쪽 위 **+** → **Install package from git URL** → 아래 주소 넣고 Install
  `https://github.com/baedonggeun/paulowniasky-localization-kit.git#v0.1.3`

**2. 셋업**
- 메뉴 **Tools > PaulowniaSky > Localization Kit > Setup Project** 클릭
- 중간에 멈추면 한 번 더 클릭 (처음엔 TMP 기본 파일부터 깔고 멈춤)
- 끝나면 `Assets/Localization` 폴더가 생김. 13개 언어와 언어별 폰트까지 준비됨

### 글자 하나 넣어 보기
**3. 번역 적기**
- 같은 메뉴의 **Export Tables to CSV** → `Assets/Localization/CSV/UI.csv`가 생김
- 엑셀로 열어서 줄 하나 추가

  | Key | ko | en | ja |
  |---|---|---|---|
  | common.confirm | 확인 | Confirm | 確認 |

  - Key는 글자 이름표. 소문자로 `어디.무엇` 모양 (예: `common.confirm`, `shop.title`)
  - 모르는 언어 칸은 비워 둬도 됨. 그 언어에선 영어로 보임
  - Comment 칸은 번역하는 사람용 메모라 비워도 됨
- **CSV UTF-8**로 저장 (그냥 CSV로 저장하면 한글이 깨짐) → 메뉴 **Import Tables from CSV**
- 구글 시트에서 작업하면: 파일 > 다운로드 > CSV로 받고, 이름을 `UI.csv`로 바꿔 `Assets/Localization/CSV/`에 덮어씀 → Import

**4. 화면에 놓기**
- `Assets/Localization/Prefabs/LocalizedText`를 Canvas 안에 끌어다 놓음
- Inspector의 **Localize String Event** > String Reference에서 `UI` → `common.confirm` 고름

**5. 확인**
- Play 누르면 글자가 나옴
- Game 뷰 위쪽 툴바의 언어 메뉴에서 **English (en)**을 고르면 "Confirm"으로 바뀜. 폰트도 같이 바뀜

### 플레이어가 언어 고르게 하기
**6. 언어 선택 드롭다운**
- Package Manager → PaulowniaSky Localization Kit → **Samples** → Language Dropdown → **Import**
- 가져온 `LanguageDropdown` 프리팹을 Canvas 안에 놓으면 끝
- 처음 켜면 컴퓨터 언어로 시작함. 플레이어가 고른 언어는 다음 실행 때도 기억함

### 번역 고칠 때마다
**7. 검사**
- **Check Missing Translations**: 번역이 빠진 칸을 알려 줌
- **Check Missing Glyphs**: 폰트에 없어서 □로 나올 글자를 알려 줌

### 지킬 것 3개
- 화면 글자는 직접 치지 말고 항상 LocalizedText 프리팹과 키로 넣음
- 번역 원본은 CSV. CSV를 고쳤으면 Import
- 문장에 숫자 같은 값이 들어가면 `{0}`을 씀 (예: `{0} 골드`). 문장을 `+`로 이어 붙이지 않음. 언어마다 어순이 달라서

### 헷갈리기 쉬운 것
- 프리팹 **파일**을 복사하는 게 아님. 같은 LocalizedText를 씬에 여러 번 놓고, 놓은 것마다 키만 다르게 고름 (씬에서 Ctrl+D로 복제해도 됨)
- CSV 파일 하나 = 테이블 하나. 새 파일(`Item.csv` 등)은 같은 이름의 테이블을 먼저 만들어야 Import에 들어감 (3장 "테이블 추가")
- 구글 시트는 탭 하나 = 테이블 하나. 탭 이름을 테이블 이름으로 맞춰 두면 안 헷갈림

자세한 건 아래 1~10장.

## 1. 설치·셋업

### 설치
1. git-lfs 설치 (폰트가 Git LFS)
2. 키트 레포가 비공개라, 이 레포에 접근되는 GitHub 계정으로 git 로그인
3. Package Manager > + > Install package from git URL에 입력:
   `https://github.com/baedonggeun/paulowniasky-localization-kit.git#v0.1.3`
   - 또는 `Packages/manifest.json`의 dependencies에 `"com.paulowniasky.localization-kit": "https://github.com/baedonggeun/paulowniasky-localization-kit.git#v0.1.3"`
   - 끝의 `#v0.1.3`이 버전 태그. 새 버전으로 올릴 땐 이 부분만 바꿈

### 셋업
1. Setup Project 실행
2. TMP 기본 리소스(TMP Essential Resources)가 없으면 그것부터 임포트하고 멈춤 → 임포트가 끝나면 한 번 더 실행
3. 콘솔에 `[Localization Kit] 셋업 완료`와 만든 것 목록이 나오면 끝

만드는 것:

| 경로 | 내용 |
|---|---|
| `LocalizationSetup.asset` | 셋업 값 (언어 목록, 원문·폴백 언어, 출력 폴더, 테이블 이름) |
| `Localization Settings.asset` | Localization 설정 (이미 있으면 그걸 씀). 폴백 켬, 시작 언어 순서 지정 |
| `Locales/` | 언어마다 Locale. 그 언어 글자 이름·폴백 정보 포함 |
| `Tables/` | String Table `UI` (언어마다 한 장) |
| `Fonts/` | 폰트 파일마다 Dynamic TMP 폰트 에셋 |
| `FontSets/Body.asset` | 본문 폰트 세트 (언어 → 폰트) |
| `Prefabs/LocalizedText.prefab` | 텍스트 프리팹 |

TMP Settings도 맞춤: 기본 폰트 = 원문 언어 폰트, 전역 폴백 = 나머지 폰트 전부, 한국어 어절 줄바꿈 켬.

다시 실행해도 안전함. 없는 것만 만들고 값이 다른 것만 고침. 바뀐 게 없으면 `셋업: 바뀐 것 없음`.

### 언어·원문·폴백 바꾸기
기본값: 13개 언어(ko en ja zh-Hans zh-Hant ru de fr es pt-BR it pl tr), 원문 ko, 폴백 en.

다르게 하려면 **처음 Setup Project 전에** 셋업 에셋을 먼저 만들어 고침:
1. Project 창 > Create > PaulowniaSky > Localization Kit > Localization Setup (기본값이 채워짐. 비어 있으면 Inspector ⋮ > Reset)
2. 아래 표대로 값 고침
3. Setup Project 실행

| 항목 | 뜻 |
|---|---|
| Source Locale Code | 원문 언어. 번역 기준이고 마지막 폴백 |
| Fallback Locale Code | 번역 없는 언어가 먼저 볼 언어. OS 언어로 못 정할 때 시작 언어도 이것 |
| Use System Locale | 처음 실행 때(고른 언어가 없을 때) OS 언어로 시작. 끄면 폴백 언어로 시작 (고른 언어는 계속 기억) |
| Locales | 언어마다 Code, Native Name(드롭다운에 보일 그 언어 글자 이름), Font(폰트 파일) |
| Output Folder | 만들 위치 (Assets/ 아래) |
| String Table Name | 만들 String Table 이름 |

- 셋업 메뉴는 프로젝트에 있는 셋업 에셋을 찾아 씀 (위치 상관없음)
- 언어는 나중에 추가해도 됨: Locales에 줄 추가 → Setup Project. 새 언어는 기존 언어 뒤에 붙음
- 나중에 추가한 언어는 셋업 테이블(`UI`)에만 테이블이 생김. 직접 만든 테이블은 그 테이블 묶음 에셋의 Inspector > Missing Tables에서 Create
- 셋업 메뉴는 언어를 안 지움 (번역이 같이 지워져서). 목록은 처음에 정해 두는 게 편함

## 2. 화면 글자 넣기

### 새 텍스트
1. `Prefabs/LocalizedText.prefab`을 Canvas 아래에 놓음
2. Localize String Event > String Reference에서 테이블과 키를 고름

| 컴포넌트 | 하는 일 |
|---|---|
| TextMeshPro - Text (UI) | 글자 표시 |
| Localize String Event | 키 → 지금 언어 글자. Update String 이벤트로 TMP text에 넣음 |
| Locale Font UI Binder | 언어 → 폰트. Font Set에서 그 언어 폰트를 골라 바꿈 |

- 크기·정렬·색은 TMP에서 평소처럼 바꿈. 폰트는 Font Set이 정하니까 TMP의 Font Asset은 안 바꿈 (Play 중에 덮어써짐)
- String Reference 칸에서 새 키를 만들고 언어별 값을 바로 넣을 수도 있음. 그랬으면 Export Tables to CSV로 CSV도 맞춤 (3장)

### 이미 있는 TMP 텍스트
1. TextMeshPro - Text (UI) 컴포넌트 ⋮ > Localize → Localize String Event가 붙고 text에 연결됨
2. 폰트도 언어별로 바꾸려면 Locale Font UI Binder 추가 → Font Set에 `FontSets/Body` (역할별 세트는 6장)

## 3. 텍스트 추가·번역 (CSV)
텍스트 원본은 CSV. String Table 하나 = 파일 하나 (`CSV/{테이블 이름}.csv`).

### 흐름
1. 처음엔 Export Tables to CSV로 CSV를 만듦
2. 엑셀·Google 시트에서 편집 → 저장
3. Import Tables from CSV
4. Check Missing Translations → Check Missing Glyphs

### 열

| 열 | 내용 |
|---|---|
| `Key` | 키. `{영역}.{대상}[.{속성}]`, 소문자·`_` (예: `common.confirm`, `item.heal_potion.name`) |
| `Comment` | 번역가용 설명 (어디에 쓰는 글자인지) |
| `ko`, `en`, `ja` … | 언어 코드별 번역 |

- 머리글 이름으로 열을 찾아서 열 순서는 상관없음
- 새 키는 행을 추가하면 됨. CSV에 없는 키는 테이블에서 안 지워짐
- 빈 칸은 그 언어 번역을 지움 → 실행 중엔 폴백 언어로 보이고, Check Missing Translations에 잡힘
- 엑셀은 "CSV UTF-8(쉼표로 분리)"로 저장. 그냥 "CSV"로 저장하면 한글이 깨짐
- 내보내기는 BOM 붙은 UTF-8. 가져오기는 BOM 있든 없든 UTF-8로 읽음

### 덮어쓰기 주의
메뉴 방향 = 덮어쓰는 방향.
- Export: 테이블 → CSV. CSV에서 고친 게 있으면 사라짐
- Import: CSV → 테이블. 테이블 창·Inspector에서 고친 칸이 CSV에도 있으면 CSV 값으로 바뀜

고친 쪽에서 반대쪽으로 맞춤: CSV를 고쳤으면 Import, 테이블을 고쳤으면 Export.

### 테이블 추가
1. Window > Asset Management > Localization Tables > New Table Collection에서 String Table Collection 만듦 (언어 전부 체크)
2. Export Tables to CSV → `{테이블 이름}.csv`가 생김

- CSV·검사 메뉴는 테이블 전부를 처리함. CSV 파일이 없는 테이블은 가져오기에서 건너뜀

## 4. 코드·SO에서 쓰기
표시용 문자열 필드는 `string` 대신 `LocalizedString`. Inspector에서 테이블·키를 고름.

```csharp
using UnityEngine.Localization;

[SerializeField] LocalizedString m_Title;   // 값 예: "상점"
[SerializeField] LocalizedString m_Price;   // 값 예: "{0} 골드"

// 지금 언어로 한 번 읽기
var title = m_Title.GetLocalizedString();
var price = m_Price.GetLocalizedString(gold);   // {0} 채움 → "120 골드"
```

언어가 바뀔 때마다 다시 받기. 구독하면 준비되는 대로 한 번 불리고, 언어가 바뀔 때마다 또 불림:

```csharp
void OnEnable()  => m_Title.StringChanged += OnTitleChanged;
void OnDisable() => m_Title.StringChanged -= OnTitleChanged;
void OnTitleChanged(string value) => m_Label.text = value;
```

값이 들어가는 문자열은 구독 전에 `m_Price.Arguments = new object[] { gold };`를 넣고, 값이 바뀌면 `m_Price.RefreshString()`.

LocalizedText 프리팹(Localize String Event)에 값 넣기:

```csharp
m_LocalizeEvent.StringReference.Arguments = new object[] { gold };
m_LocalizeEvent.RefreshString();
```

- 문장은 이어 붙이지 말고 자리표시자로 (언어마다 어순이 다름): `"골드: " + gold` 대신 값을 `골드: {0}`로
- `{0}`은 그냥 칸에서도 됨. 이름 자리표시자(`{gold}`)·복수형은 테이블 창에서 그 칸 Smart를 켬
- `GetLocalizedString()`은 동기 로드. WebGL처럼 동기 로드가 안 되는 플랫폼에선 `GetLocalizedStringAsync()`
- 비교·파싱·키 조합은 `StringComparison.Ordinal`, `CultureInfo.InvariantCulture` (터키어 i 같은 문화권 차이 방지)

## 5. 언어 고르기

### 시작 언어
위에서부터 처음 정해지는 언어로 시작:
1. 명령줄 인자 `-language=ja`
2. 지난번 고른 언어 (PlayerPrefs. 언어를 바꿀 때마다 자동 저장)
3. OS 언어 (프로젝트에 있는 언어면. 셋업 값 Use System Locale을 끄면 건너뜀)
4. 폴백 언어 (기본 en)

### 번역 없는 칸
그 언어 → 폴백 언어 → 원문 언어 순으로 찾음 (기본값이면 ja → en → ko).

### 언어 선택 드롭다운 (샘플)
1. Package Manager > PaulowniaSky Localization Kit > Samples > Language Dropdown > Import
2. `Assets/Samples/PaulowniaSky Localization Kit/{버전}/Language Dropdown/LanguageDropdown.prefab`을 Canvas 아래에 놓음
3. 스프라이트·색·크기·폰트를 게임 UI에 맞게 바꿈. 같은 오브젝트의 Locale Dropdown UI Binder만 남기면 됨

- 언어 이름은 그 언어 글자로 보임 (한국어, English, 日本語 …). 셋업 에셋의 Native Name
- 폰트는 비워 둠 → TMP 기본 폰트를 쓰고, 다른 문자는 TMP 전역 폴백으로 그림
- 다른 곳(저장값, 다른 UI)에서 언어가 바뀌어도 드롭다운 표시가 따라감
- 씬에 EventSystem 필요 (Input System이면 Input System UI Input Module)

### 코드로 바꾸기

```csharp
using UnityEngine.Localization.Settings;

LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ja");
```

### 언어 순서
Project Settings > Localization > Available Locales에서 드래그. 드롭다운 순서가 이걸 따름. 셋업 메뉴는 새로 만드는 언어만 뒤에 붙이고 기존 순서는 안 건드림.

### 에디터에서 확인
- Play 중: Game 뷰의 언어 메뉴로 바꿈 (Edit > Preferences > Localization > Locale Game View Menu, 기본 켜짐)
- 에디터 모드: Window > Asset Management > Localization Scene Controls에서 언어를 고름. 글자만 바뀌고 폰트는 Play 중에만 바뀜

## 6. 폰트

### 구조
- **Font Set** (`LocaleFontSetSO`): 폰트 역할(본문, 제목 …) 하나에 에셋 하나. 언어 코드마다 TMP 폰트 에셋과 머티리얼 프리셋. 목록에 없는 언어는 Default Font
- **Locale Font UI Binder**: 같은 오브젝트 TMP 텍스트의 폰트를 언어에 맞게 바꿈. Play 중에만 동작 (에디터 모드에서 바꾸면 언어 미리보기 때마다 씬 파일이 바뀌어서)
- **Body** (`FontSets/Body.asset`, 셋업이 만듦): ko·ja·zh-Hans·zh-Hant는 Noto Sans KR·JP·SC·TC, 나머지는 Noto Sans. Default Font는 원문 언어 폰트
- Noto 폰트는 SIL Open Font License 1.1. 라이선스 파일은 키트 `Fonts/`에 있음

### 본문 폰트 바꾸기 (Body)
1. 폰트 파일(ttf/otf)을 Assets 아래에 넣음
2. 셋업 에셋 > Locales에서 그 언어 Font를 새 파일로 바꿈
3. Setup Project 다시 실행 → `Fonts/{파일 이름} SDF.asset` 생성, Body 갱신, TMP 전역 폴백에 추가

- Body.asset의 폰트를 직접 바꾸면 다음 Setup Project 때 셋업 에셋 값으로 되돌아감
- 줄마다 있는 Material(외곽선·그림자 같은 머티리얼 프리셋)은 직접 넣어도 유지됨. 프리셋은 폰트 아틀라스마다 따로라서 그 줄 폰트로 만든 프리셋을 넣음
- 원문 언어 폰트를 바꾸면 TMP 기본 폰트와 Body의 Default Font도 같이 바뀜

### 역할별 폰트 (제목 등)
1. TMP 폰트 에셋 만듦: 폰트 파일 우클릭 > Create > TextMeshPro > Font Asset > SDF
   - 한글·한자 폰트면 Inspector에서 Atlas Width·Atlas Height 2048, Multi Atlas Textures 켬 (셋업이 만드는 것과 같게)
   - Clear Dynamic Data On Build 켬 (아래 Dynamic 폰트 아틀라스 참고)
2. Project 창 > Create > PaulowniaSky > Localization Kit > Locale Font Set → Default Font와 언어별 줄을 채움
3. 그 텍스트의 Locale Font UI Binder > Font Set에 넣음

- 셋업 메뉴는 Body 말고 다른 세트는 안 건드림

### Dynamic 폰트 아틀라스
Dynamic 폰트 에셋은 그린 글자를 아틀라스에 쌓고, 저장하면 그게 파일에 들어가서 git diff가 생김. 키트가 저장 직전에 아틀라스를 비워서 막음.
- 대상: Dynamic(DynamicOS 포함)이고 Clear Dynamic Data On Build가 켜진 폰트 에셋. 셋업이 만든 폰트 에셋은 켜져 있음
- TMP Settings의 Clear Dynamic Data On Build를 켜 두면 새로 만드는 Dynamic 폰트 에셋에 기본으로 켜짐
- 빌드에도 빈 아틀라스로 들어가고, 실행 중에 필요한 글자를 그림

### 글자 검사
폰트를 바꾸거나 번역을 가져온 뒤 Check Missing Glyphs. Font Set 전부 × 언어 전부에서, 번역문 글자가 TMP가 글자를 찾는 순서(그 언어 폰트 → 그 폰트의 폴백 → TMP 전역 폴백 → TMP 기본 폰트) 안에 있는지 봄. 어디에도 없는 글자는 화면에 □로 나옴.

## 7. 메뉴 한눈에

| 메뉴 | 하는 일 |
|---|---|
| Setup Project | 설정·언어·테이블·폰트 에셋·TMP 설정·텍스트 프리팹 생성. 다시 실행해도 없는 것만 만듦 |
| Export Tables to CSV | String Table 전부를 `{출력 폴더}/CSV/{테이블 이름}.csv`로 내보냄 |
| Import Tables from CSV | 같은 경로의 CSV를 테이블에 가져옴 |
| Check Missing Translations | 언어마다 값이 없거나 빈 키를 콘솔에 보고 (언어마다 키 10개까지 보여 줌) |
| Check Missing Glyphs | 언어마다 번역문 글자가 Font Set 폰트·폴백에 있는지 보고 |

Create 메뉴 (Project 창 > Create > PaulowniaSky > Localization Kit):

| 메뉴 | 만드는 것 |
|---|---|
| Localization Setup | 셋업 값 에셋 (1장) |
| Locale Font Set | 폰트 세트 (6장) |

## 8. 프로젝트 규칙 (복사해서 씀)
새 게임의 CLAUDE.md나 팀 규칙에 붙여 씀.

```markdown
## 다국어 (PaulowniaSky Localization Kit)
- 화면에 보이는 글자 하드코딩 금지 (코드·프리팹·SO). String Table 키로만 넣음
- UI 텍스트: `Assets/Localization/Prefabs/LocalizedText.prefab` (키 연결 + 언어별 폰트 교체 포함)
- SO·컴포넌트의 표시용 문자열 필드: `string` 대신 `LocalizedString`
- 키: `{영역}.{대상}[.{속성}]`, 소문자·`_` (예: `common.confirm`, `item.heal_potion.name`, `quest.forest_01.desc`)
- 문장 속 값은 자리표시자(`{0}`)로. 문자열 이어 붙이기 금지 (언어마다 어순이 다름). 이름 자리표시자(`{gold}`)·복수형은 그 항목 Smart 켬
- 비교·파싱·키 조합: `StringComparison.Ordinal`, `CultureInfo.InvariantCulture` (터키어 i 같은 문화권 차이 방지)
- 텍스트 원본: `Assets/Localization/CSV/{테이블 이름}.csv`. 고친 뒤 `Tools > PaulowniaSky > Localization Kit > Import Tables from CSV` → Check Missing Translations·Check Missing Glyphs. 테이블 창에서 직접 고쳤으면 Export Tables to CSV로 CSV도 맞춤
- 언어 선택 UI: 키트 샘플(Package Manager > Samples > Language Dropdown)을 가져와서 꾸밈
- 키트 사용법: 패키지 `com.paulowniasky.localization-kit`의 `Documentation~/index.md`
```

## 9. 문제 해결

| 콘솔·증상 | 원인 | 조치 |
|---|---|---|
| `셋업 중단: …` | 셋업 에셋 값 문제 (언어 목록 빔, 코드 겹침, 폰트 빔, 원문·폴백 언어가 목록에 없음, 출력 폴더가 Assets/ 밖, 테이블 이름 빔) | 메시지대로 셋업 에셋 고치고 다시 실행 |
| `TMP 기본 리소스를 임포트함…` | TMP Essential Resources가 없었음 | 임포트가 끝나면 Setup Project 한 번 더 |
| `폰트 에셋을 못 만듦: …` | 폰트 파일에서 글자 데이터를 못 읽음 | 폰트 파일 Inspector에서 Include Font Data 켬 → 다시 실행 |
| 화면에 □ | 그 글자가 폰트·폴백 어디에도 없음 | Check Missing Glyphs로 찾고, 그 글자가 있는 폰트로 바꿈 |
| 번역 대신 다른 언어가 보임 | 그 칸 번역이 비어서 폴백 언어로 보임 | Check Missing Translations로 찾아 채움 |
| `… Font Set이 비어 있음` | Locale Font UI Binder에 Font Set을 안 넣음 | Font Set 넣음 |
| `… TMP 텍스트가 없음`, `… TMP 드롭다운이 없음` | Binder를 TMP 없는 오브젝트에 붙임 | TMP 텍스트·드롭다운과 같은 오브젝트에 붙임 |
| `Locale Font Set 에셋이 없음…` | 셋업 전에 글자 검사를 돌림 | Setup Project 먼저 |
| `가져올 CSV 없음: …` | CSV 파일이 없음 | Export Tables to CSV로 먼저 만듦 |
| `Group '...' has bundle naming style 'OnlyHash' ...` | Localization이 띄우는 경고. Addressables가 Windows 경로 길이 제한을 피하려고 일부러 짧은 해시 이름(OnlyHash)을 씀. 경고 문구의 "긴 파일 이름"은 OnlyHash에 해당 안 됨 | 무시 |

## 10. 키트 고치기 (개발)
원본은 How-to-Live-as-a-Mage 레포의 `Packages/com.paulowniasky.localization-kit` (embedded). 키트 레포에 직접 커밋하면 subtree push가 막힘.

1. 원본 레포에서 패키지를 고침
2. `package.json`의 version과 `CHANGELOG.md`를 올림
3. 패키지 경로만 커밋 (게임 변경과 섞지 않아야 키트 레포 히스토리가 패키지 커밋만으로 유지됨)
4. `git subtree push --prefix=Packages/com.paulowniasky.localization-kit kit main` (`kit` = 키트 레포 remote)
5. 키트 레포에 새 태그 (예: `v0.1.1`). GitHub Releases에서 만들어도 됨
6. 키트를 쓰는 게임의 git URL 끝을 새 태그로 바꿈
