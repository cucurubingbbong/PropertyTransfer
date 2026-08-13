# UI Image Studio for Unity

Unity Editor 안에서 게임 UI용 이미지를 빠르게 조립하고 PNG로 내보내는 **가벼운 UI 이미지 에디터**입니다.

## 설치

`Assets/Editor/UIImageStudio` 폴더를 **프로젝트의 `Assets/Editor/` 아래에 그대로 복사**하세요.

Unity 컴파일이 끝나면:

`Tools > UI Image Studio`

에서 실행합니다.

## 이번 버전에서 추가된 점

- **가벼운 미리보기 모드** 기본 활성화
  - 큰 캔버스 / 줌아웃 상태에서 단순 프리뷰로 전환되어 Editor 렉을 줄임
  - 에셋 패널도 표시 개수를 자동 제한해서 가볍게 동작
- **도형 종류 선택 가능**
  - 둥근 사각형
  - 사각형
  - 원형/타원
  - 필(Pill)
  - 다이아
  - 삼각형
- **편집 편의 기능 추가**
  - 맨앞 / 맨뒤 보내기
  - 가운데 정렬 X / Y
  - 이미지 원본 크기로 복원
  - Smart Guides On/Off
  - 빠른 미리보기 On/Off

## 기본 조작

- Project의 `Sprite` / `Texture2D`를 캔버스로 드래그: 이미지 레이어 추가
- 아래 에셋 패널 더블클릭: 이미지 레이어 추가
- `T`: 텍스트 추가
- `V`: 선택 도구
- `H`: 손/이동 도구
- `F`: 캔버스 화면 맞춤
- 마우스 휠: 줌
- 가운데 마우스 드래그: 화면 이동
- 선택 후 드래그: 레이어 이동
- 흰색 핸들 드래그: 크기 조절
- 위쪽 원형 핸들 드래그: 회전
- `Shift` + 리사이즈: 비율 유지
- `Shift` + 회전: 15도 단위
- 방향키: 1px 이동
- `Shift` + 방향키: 10px 이동
- `Ctrl/Cmd + D`: 복제
- `Ctrl/Cmd + C/V`: 레이어 복사/붙여넣기
- `Delete`: 삭제
- `Ctrl/Cmd + S`: 문서 JSON 저장

## 포함 기능

- Shape / Text / Image 레이어
- **다중 도형 타입 지원**
- 레이어 순서, 표시, 잠금
- 위치/크기/회전/불투명도 Inspector
- Shape 채우기/외곽선/모서리 반경/그림자
- Text 폰트/크기/정렬/색상
- Image Tint/비율 유지/원본 크기 복원
- Grid + Snap + Smart Guide
- 1920×1080 / 1080×1920 / 1280×720 / Custom 캔버스
- Undo / Redo
- JSON 저장/불러오기
- 투명 배경 PNG Export
- 프로젝트 Sprite/Texture 에셋 브라우저

## 참고

PNG Export의 텍스트는 레이어에 프로젝트 `Font`가 지정되어 있으면 그 폰트를 사용합니다. 폰트가 비어 있으면 OS 폰트(Pretendard/SUIT/맑은 고딕 등)를 순서대로 시도합니다.

현재 코드는 Unity Editor용이며 `UnityEngine.UI`(uGUI)를 PNG 렌더링에 사용합니다. 프로젝트에서 Unity UI 패키지를 제거한 경우 다시 설치해야 합니다.
