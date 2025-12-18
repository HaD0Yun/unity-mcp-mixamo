# Mixamo MCP Server

Standalone MCP (Model Context Protocol) server for downloading Mixamo animations. Works with **any MCP client** - Claude Desktop, VS Code extensions, or custom integrations.

## Features

- **mixamo-auth**: Store and validate Mixamo authentication token
- **mixamo-search**: Search animations by keyword with smart mapping
- **mixamo-download**: Download single animations as FBX
- **mixamo-batch**: Download multiple animations at once
- **mixamo-keywords**: List all available animation keywords

## Installation

### Using pip

```bash
pip install mixamo-mcp
```

### From source

```bash
cd server
pip install -e .
```

## Configuration

### Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "mixamo": {
      "command": "mixamo-mcp"
    }
  }
}
```

Or if installed from source:

```json
{
  "mcpServers": {
    "mixamo": {
      "command": "python",
      "args": ["-m", "mixamo_mcp.server"]
    }
  }
}
```

### VS Code (with MCP extension)

Add to your settings:

```json
{
  "mcp.servers": {
    "mixamo": {
      "command": "mixamo-mcp"
    }
  }
}
```

## Authentication

1. Go to [mixamo.com](https://www.mixamo.com) and log in with your Adobe account
2. Open browser DevTools (F12)
3. In Console, run: `localStorage.access_token`
4. Copy the token value (without quotes)
5. Use `mixamo-auth` tool to store the token

```
mixamo-auth accessToken="your_token_here"
```

The token is stored in `~/.mixamo_mcp_token` and persists across sessions.

## Usage Examples

### Search for animations

```
mixamo-search keyword="run"
mixamo-search keyword="attack" limit=20
```

### Download single animation

```
mixamo-download animationIdOrName="idle" outputDir="C:/MyProject/Assets/Animations"
mixamo-download animationIdOrName="Walking" outputDir="./animations" fileName="player_walk"
```

### Batch download

```
mixamo-batch animations="idle,walk,run,jump" outputDir="./animations" characterName="Player"
```

### List keywords

```
mixamo-keywords
mixamo-keywords filter="combat"
```

## For Unity Projects

Download animations directly to your Unity project's Assets folder:

```
mixamo-batch animations="idle,walk,run,jump,attack" outputDir="D:/MyUnityProject/Assets/Animations" characterName="Player"
```

Then in Unity:
1. FBX files will auto-import
2. Set Rig to Humanoid in import settings
3. Create Animator Controller and add states

For automated Humanoid setup and Animator creation, see the Unity Helper package.

## API Reference

### mixamo-auth

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| accessToken | string | No | Token to store |
| clear | boolean | No | Clear stored token |

### mixamo-search

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| keyword | string | Yes | Search keyword |
| limit | integer | No | Max results (default: 10) |
| showQueries | boolean | No | Show search queries used |

### mixamo-download

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| animationIdOrName | string | Yes | Animation ID or keyword |
| outputDir | string | Yes | Output directory path |
| characterId | string | No | Character ID for rigging |
| fileName | string | No | Custom file name |

### mixamo-batch

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| animations | string | Yes | Comma-separated keywords |
| outputDir | string | Yes | Output directory path |
| characterName | string | No | Folder name (default: Character) |
| characterId | string | No | Character ID for rigging |

### mixamo-keywords

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| filter | string | No | Filter by category |

## License

Apache-2.0
