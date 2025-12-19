<div align="center">

# Mixamo MCP

### AI로 Mixamo 애니메이션을 자동 다운로드

[![MCP](https://img.shields.io/badge/MCP-Protocol-00D4AA?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xMiAyQzYuNDggMiAyIDYuNDggMiAxMnM0LjQ4IDEwIDEwIDEwIDEwLTQuNDggMTAtMTBTMTcuNTIgMiAxMiAyem0wIDE4Yy00LjQxIDAtOC0zLjU5LTgtOHMzLjU5LTggOC04IDggMy41OSA4IDgtMy41OSA4LTggOHoiLz48L3N2Zz4=)](https://modelcontextprotocol.io)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-000000?style=for-the-badge&logo=unity)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)
[![Release](https://img.shields.io/github/v/release/HaD0Yun/unity-mcp-mixamo?style=for-the-badge&color=brightgreen)](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)

**Claude Desktop • Cursor • Windsurf** 지원

한국어 | [English](README_EN.md)

</div>

---

## 설치 (Git URL)

### Step 1: Package Manager에서 설치

1. Unity에서 **Window > Package Manager** 열기
2. 좌측 상단 **+** 버튼 클릭
3. **Add package from git URL...** 선택
4. 아래 URL 붙여넣기:

```
https://github.com/HaD0Yun/unity-mcp-mixamo.git?path=unity-helper
```

5. **Add** 클릭

---

### Step 2: 자동 설정

설치 즉시 자동으로:

- MCP 서버 다운로드 (`mixamo-mcp.exe`)
- Claude Desktop 설정 자동 구성
- 설정 창 자동 열림

> **설치 후 Claude Desktop을 재시작하세요!**

---

### Step 3: Mixamo 토큰 설정

1. **Window > Mixamo MCP** 메뉴 클릭
2. 브라우저에서 [mixamo.com](https://www.mixamo.com) 로그인
3. `F12` → Console 탭에서 실행:
```javascript
copy(localStorage.access_token)
```
4. 복사된 토큰을 창에 붙여넣고 **Save** 클릭

---

### 설치 완료!

이제 AI에게 말하세요:

```
"walk 애니메이션 다운로드해줘"
```

```
"idle, run, jump 애니메이션 한번에 다운로드"
```

---

## 수동 설치 (선택사항)

자동 설치가 안 될 경우:

1. **Window > Mixamo MCP** 열기
2. **Download & Install** 버튼 클릭
3. 사용할 AI 도구의 **Configure** 버튼 클릭

---

## MCP 명령어

| 명령어 | 설명 | 예시 |
|:-------|:-----|:-----|
| `mixamo-search` | 애니메이션 검색 | `mixamo-search keyword="walk"` |
| `mixamo-download` | 단일 다운로드 | `mixamo-download animationIdOrName="idle"` |
| `mixamo-batch` | 배치 다운로드 | `mixamo-batch animations="idle,walk,run"` |

---

## 지원 키워드

| 카테고리 | 키워드 |
|:--------:|--------|
| **이동** | `idle` `walk` `run` `jump` `crouch` `climb` |
| **전투** | `attack` `punch` `kick` `sword` `block` `death` |
| **감정** | `wave` `bow` `clap` `cheer` `laugh` `talk` |
| **댄스** | `dance` `hip hop` `salsa` `breakdance` |

---

## Unity 자동화 기능

### 자동 Humanoid 설정
`Animations` 또는 `Mixamo` 폴더에 FBX 파일 넣으면 자동으로 Humanoid 리그 설정됨

### Animator Controller 생성
1. 애니메이션 폴더 선택
2. **Tools > Mixamo Helper > Create Animator from Selected Folder**

---

## 설치 구조

```
%LOCALAPPDATA%/MixamoMCP/
└── mixamo-mcp.exe          ← MCP 서버 (자동 다운로드)

%APPDATA%/Claude/
└── claude_desktop_config.json  ← 자동 구성
```

---

## 문제 해결

| 문제 | 해결 |
|:-----|:-----|
| 자동 설치가 안 됨 | Window > Mixamo MCP에서 수동 설치 |
| AI에서 mixamo 도구가 안 보임 | AI 도구 완전히 종료 후 재시작 |
| "Token expired" 에러 | mixamo.com에서 새 토큰 복사 |
| 다운로드 실패 | 방화벽/VPN 확인, GitHub 접근 가능한지 확인 |
| 재설치 필요 | Window > Mixamo MCP > Reset Installation |

---

## 라이센스

MIT License

---

<div align="center">

**Star 부탁드립니다!**

[Issues](https://github.com/HaD0Yun/unity-mcp-mixamo/issues) · [Releases](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)

</div>
