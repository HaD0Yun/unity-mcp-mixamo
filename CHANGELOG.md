# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
