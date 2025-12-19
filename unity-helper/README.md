# Mixamo MCP for Unity

AI-powered Mixamo animation downloader with one-click MCP client configuration.

## Features

### MCP Integration
- **One-click setup** for Claude Desktop, Cursor, and Windsurf
- **Auto-download** MCP server executable
- **Token management** built into Unity Editor

### Animation Utilities
- **Auto Humanoid Rig** - FBX files automatically configured
- **Animator Builder** - Create Animator Controllers from animation folders

## Installation

### Unity Package Manager (Git URL)

```
https://github.com/HaD0Yun/unity-mcp-mixamo.git?path=unity-helper
```

### Manual Installation

Copy the `unity-helper` folder to your project's `Assets/` directory.

## Quick Start

1. **Window > Mixamo MCP**
2. Click **Download & Install**
3. Click **Configure** for your AI tool (Claude/Cursor/Windsurf)
4. Enter your Mixamo token and click **Save**
5. Restart your AI tool
6. Done! Ask AI to download animations.

## Getting Mixamo Token

1. Go to [mixamo.com](https://www.mixamo.com) and log in
2. Press F12 → Console tab
3. Type: `copy(localStorage.access_token)`
4. Paste into the Unity window

## Usage

Tell your AI:

```
mixamo-search keyword="run"
```

```
mixamo-download animationIdOrName="idle" outputDir="Assets/Animations"
```

```
mixamo-batch animations="idle,walk,run,jump" outputDir="Assets/Animations"
```

## Auto Humanoid Setup

FBX files dropped into folders containing `Animations` or `Mixamo` are automatically configured with Humanoid rig.

## Animator Controller Builder

1. Select a folder with animation FBX files
2. **Tools > Mixamo Helper > Create Animator from Selected Folder**

## License

MIT
