# Asset Forge for Unity

Unity Editor 안에서 **2D 이미지/레이아웃 편집**과 **간단한 3D 프리미티브 모델링**을 할 수 있는 에디터 툴입니다.

외부 패키지 없이 Unity Editor 기본 API를 사용합니다.

## 설치

프로젝트에 아래 폴더를 복사하세요.

```text
Assets/Editor/AssetForge
```

컴파일이 끝나면 Unity 상단 메뉴에서 실행합니다.

```text
Tools > Asset Forge > 2D Editor
Tools > Asset Forge > 3D Builder
```

---

# Asset Forge 2D

범용 2D 이미지/레이아웃 제작용 에디터입니다.

## 기능

- Image / Text / Shape 레이어
- Rectangle / Rounded Rectangle / Ellipse / Pill / Diamond / Triangle
- 마우스 이동 / 8방향 리사이즈 / 회전
- Grid / Snap / Smart Guide
- 레이어 앞뒤 순서 / 잠금 / 숨김 / 복제
- 가운데/좌우/상하 정렬
- 색상 / 외곽선 / 둥근 모서리 / 그림자
- 폰트 / 크기 / 정렬 / 색상
- Sprite / Texture2D 드래그 추가
- JSON 저장/불러오기
- 투명 PNG Export
- 큰 캔버스용 빠른 미리보기

## 주요 단축키

- `V` 선택
- `H` 화면 이동
- `T` 텍스트 추가
- `F` 캔버스 맞춤
- `Ctrl/Cmd + D` 복제
- `Ctrl/Cmd + C / V` 레이어 복사/붙여넣기
- 방향키 1px 이동
- `Shift + 방향키` 10px 이동
- `Delete` 삭제
- `Ctrl/Cmd + S` 저장

---

# Asset Forge 3D Builder

Blender처럼 정교한 버텍스 편집을 하는 툴이 아니라, **기본 도형을 빠르게 조합해서 게임용 간단 모델을 만드는 가벼운 빌더**입니다.

## 프리미티브

- Cube
- Sphere
- Capsule
- Cylinder
- Plane
- Cone

## 모델 편집

- Move / Rotate / Scale 핸들
- Local / Global 핸들
- Position / Rotation / Scale 숫자 직접 입력
- Position / Rotation / Scale Snap
- Duplicate / Delete
- Mirror X / Y / Z
- Reset Transform
- X/Y/Z +90도 빠른 회전
- Object 표시/숨김
- Object 잠금
- Object 리스트 선택
- 뷰포트 클릭 선택

## 재질

- Color
- Metallic
- Smoothness
- Emission

URP Lit → HDRP Lit → Standard 순서로 사용 가능한 Shader를 자동 선택합니다.

## 카메라

- RMB Drag: Orbit
- MMB Drag: Pan
- Wheel: Zoom
- Front / Back / Left / Right / Top / Bottom
- Perspective / Orthographic
- 선택 오브젝트 Focus
- 전체 모델 Frame

## 주요 단축키

- `W` Move
- `E` Rotate
- `R` Scale
- `F` 선택 오브젝트 Focus
- `Ctrl/Cmd + D` 복제
- `Delete` 삭제
- `Ctrl/Cmd + S` 3D 프로젝트 JSON 저장

## 출력

### Prefab

현재 조합을 실제 Unity Prefab으로 저장합니다.

- 파츠별 Material Asset 자동 생성
- Cone / Plane처럼 Asset Forge가 생성한 Mesh도 자동 저장
- 게임 Scene에서 바로 사용할 수 있는 Prefab 생성

### Combined Mesh

보이는 파츠를 `Mesh.CombineMeshes`로 하나의 Mesh Asset으로 합칩니다.

재질 구성이 필요한 경우 Combined Mesh보다 Prefab Export를 권장합니다.

### PNG

현재 3D 카메라 구도를 투명 배경 PNG로 렌더합니다.

기본값은 `512 x 512`이며 Inspector에서 최대 `4096 x 4096`까지 설정할 수 있습니다.

---

# 가볍게 만든 방식

3D Builder는 일반 Scene 안에 작업용 오브젝트를 계속 생성하지 않습니다.

- Unity Preview Scene 사용
- 프리뷰 GameObject / Material / 생성 Mesh는 저장하지 않음
- 변경 시 전체 모델을 매 프레임 재생성하지 않고 선택 파츠만 동기화
- 지속 Repaint는 카메라 드래그 중에만 사용
- 외부 3D 패키지 없음
- 버텍스/엣지/페이스 편집, Sculpt, UV, Bone, Animation 제외

그래서 간단한 소품, Low-poly 조립 모델, Blockout, 아이콘용 3D 오브젝트 제작에 맞춰져 있습니다.

## 예제

```text
Examples/Sample2DLayout.json
Examples/Sample3DModel.af3d.json
```

3D 예제는 `Asset Forge 3D > 열기`에서 JSON 파일을 선택하면 됩니다.
