# Unity MCP Mixamo Animation Tools

[![MCP Enabled](https://badge.mcpx.dev?status=on)](https://modelcontextprotocol.io)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-000000?style=flat&logo=unity&logoColor=blue)](https://unity.com)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

Automatically fetch and apply animations from **Mixamo** to humanoid characters in Unity using natural language commands through the **Model Context Protocol (MCP)**.

## Overview

This package extends [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) to enable seamless Mixamo integration:

```
AI Assistant: "Download idle, walk, run, and jump animations for Player"
      ↓
[MCP Tool] → [Mixamo API] → [Download FBX] → [Unity Import] → [Create Animator]
      ↓
Result: Character with configured Animator Controller ready to use!
```

## Features

- **MCP Tool Integration**: 8 dedicated tools for Mixamo operations
- **Smart Keyword Mapping**: "run" automatically searches "running", "jog", "sprint"
- **Auto-Configuration**: Humanoid rig setup, looping detection, root motion
- **Animator Controller Generation**: Complete state machine with transitions
- **Batch Download**: Fetch multiple animations in one command
- **Encrypted Token Storage**: Secure credential management

## Prerequisites

- **Unity**: 2021.3 LTS or newer
- **Unity-MCP**: v0.27.0+ ([OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/))
- **Adobe/Mixamo Account**: Free account at [mixamo.com](https://www.mixamo.com)

## Installation

### Via Unity Package Manager (Git URL)

1. Open Unity Package Manager (Window → Package Manager)
2. Click `+` → Add package from git URL
3. Enter: `https://github.com/YOUR_REPO/unity-mcp-mixamo.git?path=/UnityPackage`

### Via Local Folder

1. Clone this repository
2. In Unity Package Manager: `+` → Add package from disk
3. Select `UnityPackage/package.json`

## Authentication Setup

### Getting Your Mixamo Token

1. Go to [mixamo.com](https://www.mixamo.com) and log in
2. Open browser DevTools (F12)
3. In Console, run: `localStorage.access_token`
4. Copy the token

### Setting the Token

Use the MCP tool or Unity menu:

**Via MCP Tool:**
```
mixamo-auth accessToken="your_token_here"
```

**Via Unity Menu:**
Tools → Mixamo → 2. Set Token

## MCP Tools

| Tool | Description |
|------|-------------|
| `mixamo-auth` | Store/validate authentication token |
| `mixamo-search` | Search animations by keyword |
| `mixamo-download` | Download single animation |
| `mixamo-batch` | Batch download multiple animations |
| `mixamo-upload` | Upload character for auto-rigging |
| `mixamo-configure` | Create Animator Controller |
| `mixamo-apply` | Apply Animator to GameObject |
| `mixamo-keywords` | List available keywords |

## Usage Examples

### Search Animations
```
mixamo-search keyword="attack" limit=10
```

### Download Single Animation
```
mixamo-download animationIdOrName="idle" characterName="Player"
```

### Batch Download (Recommended)
```
mixamo-batch characterName="Player" animations="idle,walk,run,jump,attack"
```

### Create Animator Controller
```
mixamo-configure animationFolder="Assets/Animations/Player" defaultState="Idle"
```

## Animation Keywords

| Keyword | Searches For |
|---------|-------------|
| `idle` | idle, breathing idle, standing idle |
| `walk` | walking, walk, strut |
| `run` | running, run, jog, sprint |
| `jump` | jump, jumping, hop, leap |
| `attack` | attack, melee attack, swing, slash |
| `death` | death, dying, die |
| `dance` | dance, dancing, groove |

Use `mixamo-keywords` to see the full list.

## Output Structure

```
Assets/
└── Animations/
    └── {CharacterName}/
        ├── {CharacterName}_Idle.fbx
        ├── {CharacterName}_Walk.fbx
        ├── {CharacterName}_Run.fbx
        ├── {CharacterName}_Jump.fbx
        └── {CharacterName}_Animator.controller
```

## Animator Controller

Auto-generated controller includes:

**Parameters:**
- `Speed` (Float) - Locomotion blend
- `IsGrounded` (Bool) - Ground detection
- `Jump` (Trigger) - Jump action
- `Attack` (Trigger) - Attack action
- `Hit` (Trigger) - Hit reaction
- `Death` (Trigger) - Death animation

**Default Transitions:**
- Idle ↔ Walk (Speed threshold 0.1)
- Walk ↔ Run (Speed threshold 0.5)
- Any State → Jump (Jump trigger)
- Any State → Attack (Attack trigger)
- Any State → Death (Death trigger)

## Unity Editor Menu

Access via **Tools → Mixamo**:
1. Check Token - Validate stored token
2. Set Token - Enter new token
3. Search Animations - Test search
4. Batch Download - Download animations
5. Clear Token - Remove stored token
6. Show Keywords - List available keywords

## Troubleshooting

### Token Expired
```
Error: Authentication failed
```
Get a new token from mixamo.com (tokens expire periodically)

### Animation Not Found
```
Error: No animations found for 'xyz'
```
Try broader keywords or use `mixamo-search` to see available animations

### Rate Limited
```
Error: Rate limit exceeded
```
Wait a few seconds; automatic retry with exponential backoff is built-in

## Limitations

- **Unofficial API**: Mixamo API is reverse-engineered and may change
- **Humanoid Only**: Only humanoid characters are supported
- **Token Expiration**: Tokens expire; plan for re-authentication
- **Rate Limits**: Respect Mixamo's rate limits

This project is not affiliated with Adobe or Mixamo.

## License

Apache License 2.0 - See [LICENSE](LICENSE) file.

## Credits

- [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) by Ivan Murzak - Base MCP integration
- [Mixamo](https://www.mixamo.com) by Adobe - Animation source

---

Made for the Unity game development community
