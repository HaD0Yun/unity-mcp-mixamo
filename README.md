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

## 🚀 설치 가이드

### Step 1: Unity 패키지 다운로드

[**📥 MixamoMcp-Unity.zip 다운로드**](https://github.com/HaD0Yun/unity-mcp-mixamo/releases/latest/download/MixamoMcp-Unity.zip)

---

### Step 2: Unity에 설치

1. 다운로드한 ZIP 압축 해제
2. `MixamoMcp` 폴더를 Unity 프로젝트의 `Assets/` 폴더에 드래그
3. Unity가 자동으로 컴파일 (잠시 대기)

---

### Step 3: Mixamo MCP 창 열기

1. Unity 상단 메뉴에서 **Window > Mixamo MCP** 클릭
2. 창이 열리면 **Download & Install** 버튼 클릭
3. 다운로드 완료되면 "Installed" 표시 확인

---

### Step 4: AI 도구 설정

사용하는 AI 도구의 **Configure** 버튼 클릭:

| AI 도구 | 버튼 |
|:--------|:-----|
| Claude Desktop | **Claude Desktop → Configure** |
| Cursor | **Cursor → Configure** |
| Windsurf | **Windsurf → Configure** |

> ⚠️ Configure 후 **AI 도구를 완전히 종료했다가 다시 시작**해야 합니다!

---

### Step 5: Mixamo 토큰 설정

1. 브라우저에서 [mixamo.com](https://www.mixamo.com) 접속 후 로그인
2. 키보드 `F12` 눌러서 개발자 도구 열기
3. **Console** 탭 클릭
4. 아래 코드 입력 후 Enter:
```javascript
copy(localStorage.access_token)
```
5. Unity의 Mixamo MCP 창에서 **Token 입력창**에 붙여넣기
6. **Save** 버튼 클릭

---

### ✅ 설치 완료!

이제 AI에게 말하세요:

```
"walk 애니메이션 다운로드해줘"
```

```
"idle, run, jump 애니메이션 한번에 다운로드"
```

---

## 🎬 MCP 명령어

| 명령어 | 설명 | 예시 |
|:-------|:-----|:-----|
| `mixamo-search` | 애니메이션 검색 | `mixamo-search keyword="walk"` |
| `mixamo-download` | 단일 다운로드 | `mixamo-download animationIdOrName="idle"` |
| `mixamo-batch` | 배치 다운로드 | `mixamo-batch animations="idle,walk,run"` |

---

## 🏷️ 지원 키워드

| 카테고리 | 키워드 |
|:--------:|--------|
| 🚶 **이동** | `idle` `walk` `run` `jump` `crouch` `climb` |
| ⚔️ **전투** | `attack` `punch` `kick` `sword` `block` `death` |
| 😀 **감정** | `wave` `bow` `clap` `cheer` `laugh` `talk` |
| 💃 **댄스** | `dance` `hip hop` `salsa` `breakdance` |

---

## 🎮 Unity 자동화 기능

### 자동 Humanoid 설정
`Animations` 또는 `Mixamo` 폴더에 FBX 파일 넣으면 자동으로 Humanoid 리그 설정됨

### Animator Controller 생성
1. 애니메이션 폴더 선택
2. **Tools > Mixamo Helper > Create Animator from Selected Folder**

---

## ❓ 문제 해결

| 문제 | 해결 |
|:-----|:-----|
| Window > Mixamo MCP 메뉴가 없음 | ZIP 다시 다운로드 후 Assets 폴더에 복사 |
| Configure 버튼이 비활성화 | 먼저 Download & Install 실행 |
| AI에서 mixamo 도구가 안 보임 | AI 도구 완전히 종료 후 재시작 |
| "Token expired" 에러 | mixamo.com에서 새 토큰 복사 |

---

## 📜 라이센스

MIT License

---

<div align="center">

**⭐ 유용했다면 Star 부탁드립니다! ⭐**

[Issues](https://github.com/HaD0Yun/unity-mcp-mixamo/issues) · [Releases](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)

</div>
