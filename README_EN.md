# Mixamo MCP

[![MCP](https://img.shields.io/badge/MCP-Enabled-green)](https://modelcontextprotocol.io)
[![Windows](https://img.shields.io/badge/Windows-x64-blue)](https://github.com/HaD0Yun/unity-mcp-mixamo/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Auto-download Mixamo animations with AI** - A standalone MCP server.

Works with Claude Desktop, Cursor, VS Code, Windsurf, and any MCP-compatible client.

[한국어](README.md) | English

---

## Installation (2 min)

### Step 1: Download

[**Download mixamo-mcp.exe**](https://github.com/HaD0Yun/unity-mcp-mixamo/releases/latest)

Save to any folder (e.g., `C:\Tools\mixamo-mcp.exe`)

### Step 2: Configure Your MCP Client

Choose your AI tool:

<details>
<summary><b>Claude Desktop</b></summary>

Open config file:
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **Mac**: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "mixamo": {
      "command": "C:\\Tools\\mixamo-mcp.exe"
    }
  }
}
```
</details>

<details>
<summary><b>Cursor</b></summary>

Settings → MCP → Add Server

```json
{
  "mcpServers": {
    "mixamo": {
      "command": "C:\\Tools\\mixamo-mcp.exe"
    }
  }
}
```
</details>

<details>
<summary><b>VS Code (Copilot MCP)</b></summary>

Create `.vscode/mcp.json`:

```json
{
  "servers": {
    "mixamo": {
      "command": "C:\\Tools\\mixamo-mcp.exe"
    }
  }
}
```
</details>

<details>
<summary><b>Windsurf</b></summary>

Edit `~/.codeium/windsurf/mcp_config.json`:

```json
{
  "mcpServers": {
    "mixamo": {
      "command": "C:\\Tools\\mixamo-mcp.exe"
    }
  }
}
```
</details>

<details>
<summary><b>Other MCP Clients</b></summary>

Most MCP clients use a similar format:

```json
{
  "mcpServers": {
    "mixamo": {
      "command": "C:\\Tools\\mixamo-mcp.exe"
    }
  }
}
```

Refer to your tool's MCP documentation.
</details>

> ⚠️ Use `\\` for backslashes in paths!

### Step 3: Restart Your AI Tool

Fully quit and relaunch.

### Step 4: Set Mixamo Token

1. Log in to [mixamo.com](https://www.mixamo.com)
2. Press F12 → Console tab
3. Run this command (copies token to clipboard):
   ```javascript
   copy(localStorage.access_token)
   ```
4. Tell your AI:
   ```
   mixamo-auth accessToken="paste_here"
   ```

### Done!

---

## Usage

### Search Animations
```
mixamo-search keyword="run"
```

### Download Single Animation
```
mixamo-download animationIdOrName="idle" outputDir="D:/MyGame/Assets/Animations"
```

### Batch Download (Recommended)
```
mixamo-batch animations="idle,walk,run,jump" outputDir="D:/MyGame/Assets/Animations" characterName="Player"
```

### List Keywords
```
mixamo-keywords
```

---

## Animation Keywords

| Category | Keywords |
|----------|----------|
| **Movement** | idle, walk, run, jump, crouch, climb, swim |
| **Combat** | attack, punch, kick, sword, block, dodge, death |
| **Emotion** | wave, bow, clap, cheer, laugh, sit, talk |
| **Dance** | dance, hip hop, salsa, robot, breakdance |

Use `mixamo-keywords` for the full list.

---

## Tips for Unity Users

After downloading:
1. FBX files are saved to your `Assets/` folder
2. Unity auto-imports them
3. In Inspector: Rig → Change to Humanoid
4. Create an Animator Controller and use

**Want automation?** Install [Unity Helper](#unity-helper-optional)

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Tools not showing in AI | Fully restart your AI tool |
| "Token expired" error | Get new token from mixamo.com |
| Download failed | Check internet, reset token |
| exe won't run | Allow in Windows Defender |

---

## Advanced Setup

### For Developers (Install from Source)

```bash
git clone https://github.com/HaD0Yun/unity-mcp-mixamo.git
cd unity-mcp-mixamo/server
pip install -e .
```

MCP client config:
```json
{
  "mcpServers": {
    "mixamo": {
      "command": "mixamo-mcp"
    }
  }
}
```

### Build exe Yourself

```bash
cd server
pip install pyinstaller
python build.py
# Output: dist/mixamo-mcp.exe
```

---

## Unity Helper (Optional)

Auto Humanoid rig setup + Animator Controller generation utility.

In Unity Package Manager:
```
https://github.com/HaD0Yun/unity-mcp-mixamo.git?path=unity-helper
```

Features:
- Auto Humanoid rig on FBX import
- Select folder → Tools → Mixamo Helper → Auto-generate Animator

---

## Project Structure

```
unity-mcp-mixamo/
├── server/           # Python MCP server
│   ├── dist/         # Built exe
│   └── src/          # Source code
└── unity-helper/     # Unity utilities (optional)
```

---

## License

MIT License

---

## Credits

- [Mixamo](https://www.mixamo.com) by Adobe
- [MCP](https://modelcontextprotocol.io) by Anthropic
