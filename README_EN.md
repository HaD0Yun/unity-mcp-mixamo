<div align="center">

# 🎭 Mixamo MCP

### Auto-download Mixamo Animations with AI

[![MCP](https://img.shields.io/badge/MCP-Protocol-00D4AA?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xMiAyQzYuNDggMiAyIDYuNDggMiAxMnM0LjQ4IDEwIDEwIDEwIDEwLTQuNDggMTAtMTBTMTcuNTIgMiAxMiAyem0wIDE4Yy00LjQxIDAtOC0zLjU5LTgtOHMzLjU5LTggOC04IDggMy41OSA4IDgtMy41OSA4LTggOHoiLz48L3N2Zz4=)](https://modelcontextprotocol.io)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-000000?style=for-the-badge&logo=unity)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)
[![Release](https://img.shields.io/github/v/release/HaD0Yun/unity-mcp-mixamo?style=for-the-badge&color=brightgreen)](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)

Works with **Claude Desktop • Cursor • Windsurf**

[한국어](README.md) | English

</div>

---

## ✨ Features

| | Feature | Description |
|:---:|:---|:---|
| 🚀 | **One-Click Setup** | Just click Configure in Unity Editor |
| 🤖 | **AI Integration** | Request animations in natural language |
| 📦 | **Batch Download** | Download multiple animations at once |
| 🎮 | **Unity Automation** | Auto FBX Humanoid setup |
| 🔌 | **Universal MCP** | Compatible with all MCP clients |

---

## 📥 Installation (1 min)

### Step 1: Install Unity Package

**Package Manager (Git URL):**

```
https://github.com/HaD0Yun/unity-mcp-mixamo.git?path=unity-helper
```

Or download `.unitypackage` from [Releases](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)

### Step 2: Configure MCP

1. In Unity, open **Window > Mixamo MCP**
2. Click **Download & Install** (installs MCP server)
3. Click **Configure** for your AI tool
4. Restart your AI tool

### Step 3: Set Mixamo Token

1. Login to [mixamo.com](https://www.mixamo.com)
2. Press `F12` → Console tab
3. Type: `copy(localStorage.access_token)`
4. Paste in Unity window and click **Save**

### ✅ Done!

---

## 🎬 Usage

Tell your AI:

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

## 🏷️ Animation Keywords

| Category | Keywords |
|:--------:|----------|
| 🚶 **Movement** | `idle` `walk` `run` `jump` `crouch` `climb` |
| ⚔️ **Combat** | `attack` `punch` `kick` `sword` `block` `death` |
| 😀 **Emotion** | `wave` `bow` `clap` `cheer` `laugh` `talk` |
| 💃 **Dance** | `dance` `hip hop` `salsa` `breakdance` |

---

## 🎮 Unity Features

### Auto Humanoid Setup

Drop FBX files into `Animations` or `Mixamo` folders → automatically configured as Humanoid rig

### Animator Controller Generation

1. Select animation folder
2. **Tools > Mixamo Helper > Create Animator from Selected Folder**

---

## ❓ Troubleshooting

| Problem | Solution |
|:--------|:---------|
| Configure button disabled | Run Download & Install first |
| Tools not showing in AI | Fully restart your AI tool |
| "Token expired" error | Get new token from mixamo.com |

---

## 📁 Project Structure

```
unity-mcp-mixamo/
├── 📂 server/           # Python MCP server
│   ├── 📂 dist/         # Built exe
│   └── 📂 src/          # Source code
└── 📂 unity-helper/     # Unity package
    └── 📂 Editor/       # Editor scripts
```

---

## 📜 License

MIT License

---

<div align="center">

**⭐ If you found this useful, please Star! ⭐**

[Issues](https://github.com/HaD0Yun/unity-mcp-mixamo/issues) · [Releases](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)

</div>
