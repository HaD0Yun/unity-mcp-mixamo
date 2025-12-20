# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [5.0.2] - 2025-12-20

### Fixed
- **400 error on export** - Fixed gms_hash building logic
  - Now copies original gms_hash from API and only converts params to string
  - Restored `type: "Motion"` field (was incorrectly removed)
  - Matches working implementation from gnuton/mixamo_anims_downloader

## [5.0.1] - 2025-12-20

### Fixed
- ~~400 error on export~~ (incorrect fix, superseded by 5.0.2)

## [5.0.0] - 2025-12-20

### Fixed
- **CRITICAL: Download was completely broken** - Fixed gms_hash payload construction
  - Added `_get_animation_details()` to fetch animation info before export
  - Added `_build_gms_hash()` to construct proper export payload
  - Was sending `gms_hash=None` which API rejects
  - Was using animation ID instead of name for `product_name`
  - Monitor endpoint now correctly uses `/characters/{id}/monitor`
- **Claude Desktop config corruption** - Replaced dangerous string manipulation with proper JSON parsing
  - Existing MCP server configs are now preserved when adding mixamo
- **FBX auto-import too broad** - Changed from substring match to exact folder name match
  - Only triggers for folders named exactly "Mixamo" (not any folder containing "Animation")
  - Added toggle: Window > Mixamo MCP > Enable FBX Auto-Config
- **Menu path conflict** - Settings window moved to Window > Mixamo MCP > Settings

### Added
- Token validation before API calls with clear error messages
- HTTP client cleanup on server shutdown
- Streaming download to prevent memory issues with large FBX files
- `error` field in SearchResult for proper error propagation

### Changed
- Better error messages guide users to authenticate first
- Unity menu reorganized: Window > Mixamo MCP > Settings / Enable FBX Auto-Config

## [2.1.0] - 2025-12-19

### Added
- **Standalone executable** (`mixamo-mcp.exe`) - No Python installation required!
- Build script (`build.py`) for creating executables with PyInstaller
- Simplified README with 2-minute installation guide

### Changed
- Updated documentation for end-user friendly installation
- Reorganized README structure for clarity

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
1. Download `mixamo-mcp.exe` from Releases
2. Configure your MCP client to use the exe path
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
