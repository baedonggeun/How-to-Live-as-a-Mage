# PaulowniaSky Localization Kit

Unity Localization(`com.unity.localization`) 위에 얹는 다국어 키트.

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

## 무시해도 되는 경고
- `Group '...' has bundle naming style 'OnlyHash' ...` (Localization이 띄움): Addressables가 Windows 경로 길이 제한을 피하려고 일부러 해시만 쓴 짧은 이름(OnlyHash)을 씀. 경고 문구의 "긴 파일 이름"은 OnlyHash에 해당 안 됨

## 설치
지금은 How-to-Live-as-a-Mage 안에서 embedded 패키지로 개발 중. 분리 후엔 git URL로 설치.
폰트가 Git LFS라서 받는 PC에 git-lfs 필요.

## 폰트 라이선스
`Fonts/`의 Noto 폰트는 SIL Open Font License 1.1. 라이선스 파일 동봉.
