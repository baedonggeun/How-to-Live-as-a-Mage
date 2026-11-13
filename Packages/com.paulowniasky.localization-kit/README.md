# PaulowniaSky Localization Kit

Unity Localization(`com.unity.localization`) 위에 얹는 다국어 키트.

## 기능 (v0.1)
- 셋업 메뉴: 설정·언어·테이블·폰트 에셋·TMP 설정·텍스트 프리팹 한 번에 생성
- 언어별 폰트 교체
- 언어 선택 드롭다운 (샘플 프리팹)
- 누락 번역·누락 글자 검사
- Dynamic 폰트 에셋은 저장 직전에 아틀라스를 비움 (Clear Dynamic Data On Build가 켜진 것만). 에디터에서 그린 글자가 파일에 안 남아서 git diff가 생기지 않음

## 설치
지금은 How-to-Live-as-a-Mage 안에서 embedded 패키지로 개발 중. 분리 후엔 git URL로 설치.
폰트가 Git LFS라서 받는 PC에 git-lfs 필요.

## 폰트 라이선스
`Fonts/`의 Noto 폰트는 SIL Open Font License 1.1. 라이선스 파일 동봉.
