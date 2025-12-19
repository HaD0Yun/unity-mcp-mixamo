<div align="center">

# 🎭 Mixamo MCP

### AI로 Mixamo 애니메이션을 자동 다운로드

[![MCP](https://img.shields.io/badge/MCP-Protocol-00D4AA?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xMiAyQzYuNDggMiAyIDYuNDggMiAxMnM0LjQ4IDEwIDEwIDEwIDEwLTQuNDggMTAtMTBTMTcuNTIgMiAxMiAyem0wIDE4Yy00LjQxIDAtOC0zLjU5LTgtOHMzLjU5LTggOC04IDggMy41OSA4IDgtMy41OSA4LTggOHoiLz48L3N2Zz4=)](https://modelcontextprotocol.io)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-000000?style=for-the-badge&logo=unity)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)
[![Release](https://img.shields.io/github/v/release/HaD0Yun/unity-mcp-mixamo?style=for-the-badge&color=brightgreen)](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)

**Claude Desktop • Cursor • Windsurf** 지원

한국어 | [English](README_EN.md)

</div>

---

## ✨ 특징

| | 기능 | 설명 |
|:---:|:---|:---|
| 🚀 | **원클릭 설정** | Unity 에디터에서 Configure 버튼만 클릭 |
| 🤖 | **AI 통합** | 자연어로 애니메이션 요청 |
| 📦 | **배치 다운로드** | 여러 애니메이션 한번에 다운로드 |
| 🎮 | **Unity 자동화** | FBX Humanoid 자동 설정 |
| 🔌 | **범용 MCP** | 모든 MCP 클라이언트 호환 |

---

## 📥 설치 (1분)

### Step 1: Unity 패키지 설치

**Package Manager (Git URL):**

```
https://github.com/HaD0Yun/unity-mcp-mixamo.git?path=unity-helper
```

또는 [Releases](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)에서 `.unitypackage` 다운로드

### Step 2: MCP 설정

1. Unity에서 **Window > Mixamo MCP** 열기
2. **Download & Install** 클릭 (MCP 서버 설치)
3. 사용하는 AI 도구의 **Configure** 버튼 클릭
4. AI 도구 재시작

### Step 3: Mixamo 토큰 설정

1. [mixamo.com](https://www.mixamo.com) 로그인
2. `F12` → Console 탭
3. 입력: `copy(localStorage.access_token)`
4. Unity 창에서 토큰 붙여넣기 후 **Save**

### ✅ 완료!

---

## 🎬 사용법

AI에게 말하기:

```
mixamo-search keyword="walk"
```

```
mixamo-download animationIdOrName="idle" outputDir="Assets/Animations"
```

```
mixamo-batch animations="idle,walk,run,jump" outputDir="Assets/Animations"
```

---

## 🏷️ 애니메이션 키워드

| 카테고리 | 키워드 |
|:--------:|--------|
| 🚶 **이동** | `idle` `walk` `run` `jump` `crouch` `climb` |
| ⚔️ **전투** | `attack` `punch` `kick` `sword` `block` `death` |
| 😀 **감정** | `wave` `bow` `clap` `cheer` `laugh` `talk` |
| 💃 **댄스** | `dance` `hip hop` `salsa` `breakdance` |

---

## 🎮 Unity 기능

### 자동 Humanoid 설정

`Animations` 또는 `Mixamo` 폴더에 FBX 드롭 시 자동으로 Humanoid 리그 설정

### Animator Controller 생성

1. 애니메이션 폴더 선택
2. **Tools > Mixamo Helper > Create Animator from Selected Folder**

---

## ❓ 문제 해결

| 문제 | 해결 |
|:-----|:-----|
| Configure 버튼 비활성화 | 먼저 Download & Install 실행 |
| AI에서 도구가 안 보임 | AI 도구 완전 종료 후 재시작 |
| "Token expired" 에러 | mixamo.com에서 새 토큰 복사 |

---

## 📁 프로젝트 구조

```
unity-mcp-mixamo/
├── 📂 server/           # Python MCP 서버
│   ├── 📂 dist/         # 빌드된 exe
│   └── 📂 src/          # 소스 코드
└── 📂 unity-helper/     # Unity 패키지
    └── 📂 Editor/       # 에디터 스크립트
```

---

## 📜 라이센스

MIT License

---

<div align="center">

**⭐ 유용했다면 Star 부탁드립니다! ⭐**

[Issues](https://github.com/HaD0Yun/unity-mcp-mixamo/issues) · [Releases](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)

</div>
