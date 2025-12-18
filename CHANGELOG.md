# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-12-19

### Changed
- **BREAKING**: Restructured to standalone Python MCP server
- No longer depends on Unity-MCP or any specific MCP implementation
- Works with ANY MCP client (Claude Desktop, VS Code, Cursor, etc.)

### Added
- `server/` - Standalone Python MCP server
  - `mixamo-auth`: Token authentication
  - `mixamo-search`: Animation search
  - `mixamo-download`: Single download
  - `mixamo-batch`: Batch download
  - `mixamo-keywords`: Keyword listing
- `unity-helper/` - Lightweight Unity package (optional)
  - Auto Humanoid rig configuration on FBX import
  - Animator Controller builder from folder
  - No external dependencies

### Deprecated
- `UnityPackage/` - Legacy Unity-MCP dependent package (still works with Unity-MCP v0.27.0)

### Migration Guide
If you were using the Unity-MCP version:
1. Install the Python server: `pip install mixamo-mcp`
2. Configure your MCP client to use `mixamo-mcp` command
3. (Optional) Install unity-helper for auto-import features

## [1.0.0] - 2024-12-18

### Added
- Initial release of Unity MCP Mixamo Animation Tools
- 8 MCP tools for Mixamo integration:
  - `mixamo-auth`: Token authentication and storage
  - `mixamo-search`: Animation search with keyword expansion
  - `mixamo-download`: Single animation download
  - `mixamo-batch`: Batch download with Animator creation
  - `mixamo-upload`: Character upload for auto-rigging
  - `mixamo-configure`: Animator Controller generation
  - `mixamo-apply`: Apply Animator to GameObject
  - `mixamo-keywords`: List available keywords
- Smart keyword mapping for natural language queries
- Automatic Humanoid rig configuration
- Encrypted token storage in EditorPrefs
- Rate limiting with exponential backoff retry
- Unity Editor menu (Tools/Mixamo) for manual testing
- Animator Controller auto-generation with:
  - Locomotion state machine (Idle/Walk/Run)
  - Action triggers (Jump, Attack, Hit, Death)
  - Speed-based transitions
- Support for custom character upload and rigging

### Technical Details
- Built as Unity package for easy distribution
- Depends on Unity-MCP v0.27.0+
- Requires Unity 2021.3 LTS or newer
- Uses unofficial Mixamo API (reverse-engineered)
