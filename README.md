# Mixamo MCP

[![MCP Enabled](https://badge.mcpx.dev?status=on)](https://modelcontextprotocol.io)
[![Python 3.10+](https://img.shields.io/badge/Python-3.10+-blue.svg)](https://www.python.org)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-000000?style=flat&logo=unity)](https://unity.com)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

**Standalone MCP server** for downloading Mixamo animations. Works with **any MCP client** - Claude Desktop, VS Code, Cursor, or custom integrations.

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────┐
│  Any MCP Client │────▶│  Mixamo MCP     │────▶│   Unity     │
│  (Claude, etc)  │     │  (Python)       │     │  (Import)   │
└─────────────────┘     └─────────────────┘     └─────────────┘
```

## Why Standalone?

Unlike Unity-specific MCP plugins, this server:
- ✅ Works with **any** MCP client (not locked to one implementation)
- ✅ No Unity-MCP dependency required
- ✅ Can download to any folder (Unity, Unreal, Godot, etc.)
- ✅ Standard MCP protocol compliance

## Quick Start

### 1. Install the Server

```bash
pip install mixamo-mcp
```

Or from source:
```bash
cd server
pip install -e .
```

### 2. Configure Your MCP Client

**Claude Desktop** (`claude_desktop_config.json`):
```json
{
  "mcpServers": {
    "mixamo": {
      "command": "mixamo-mcp"
    }
  }
}
```

**VS Code / Cursor** (settings.json):
```json
{
  "mcp.servers": {
    "mixamo": {
      "command": "mixamo-mcp"
    }
  }
}
```

### 3. Authenticate

Get your token from [mixamo.com](https://www.mixamo.com):
1. Log in to Mixamo
2. Open DevTools (F12) → Console
3. Run: `localStorage.access_token`
4. Copy the token

Then use the `mixamo-auth` tool:
```
mixamo-auth accessToken="your_token_here"
```

### 4. Download Animations!

```
mixamo-batch animations="idle,walk,run,jump" outputDir="D:/MyGame/Assets/Animations" characterName="Player"
```

## MCP Tools

| Tool | Description |
|------|-------------|
| `mixamo-auth` | Store/validate authentication token |
| `mixamo-search` | Search animations by keyword |
| `mixamo-download` | Download single animation |
| `mixamo-batch` | Download multiple animations |
| `mixamo-keywords` | List available keywords |

### Examples

```bash
# Search for animations
mixamo-search keyword="attack" limit=10

# Download single animation
mixamo-download animationIdOrName="idle" outputDir="./animations"

# Batch download
mixamo-batch animations="idle,walk,run,jump,attack" outputDir="./animations" characterName="Hero"

# List keywords by category
mixamo-keywords filter="combat"
```

## Animation Keywords

| Category | Keywords |
|----------|----------|
| **Locomotion** | idle, walk, run, jump, crouch, climb, swim, fall |
| **Combat** | attack, punch, kick, sword, block, dodge, shoot, death |
| **Social** | wave, bow, clap, cheer, laugh, sit, talk |
| **Dance** | dance, hip hop, salsa, robot, breakdance |

Use `mixamo-keywords` for the full list.

---

## Unity Helper (Optional)

For Unity projects, we provide a lightweight helper package that:
- Auto-configures imported FBX as Humanoid rig
- Creates Animator Controllers from animation folders

### Installation

Via Unity Package Manager:
```
https://github.com/HaD0Yun/unity-mcp-mixamo.git?path=unity-helper
```

### Features

**Auto Humanoid Setup**: FBX files in `Animations/` folders are automatically configured with Humanoid rig.

**Animator Builder**: Select a folder → Tools → Mixamo Helper → Create Animator

See [unity-helper/README.md](unity-helper/README.md) for details.

---

## Repository Structure

```
unity-mcp-mixamo/
├── server/                 # Python MCP Server (main)
│   ├── src/mixamo_mcp/
│   ├── pyproject.toml
│   └── README.md
│
├── unity-helper/           # Unity utilities (optional)
│   ├── Editor/
│   ├── package.json
│   └── README.md
│
└── README.md               # This file
```

## Comparison

| Feature | This Package | Unity-MCP Plugins |
|---------|-------------|-------------------|
| MCP Client Support | Any | Specific implementation |
| Game Engine | Any | Unity only |
| Dependencies | Python only | Unity-MCP required |
| Animator Creation | Via Unity Helper | Built-in |
| Protocol | Standard MCP | Implementation-specific |

## Limitations

- **Unofficial API**: Mixamo API is reverse-engineered and may change
- **Token Expiration**: Tokens expire; re-authentication needed periodically
- **Humanoid Only**: Only humanoid animations supported
- **Rate Limits**: Built-in retry, but respect Mixamo's limits

This project is not affiliated with Adobe or Mixamo.

## License

Apache License 2.0 - See [LICENSE](LICENSE)

## Credits

- [Mixamo](https://www.mixamo.com) by Adobe - Animation source
- [MCP](https://modelcontextprotocol.io) by Anthropic - Protocol standard
