"""Mixamo Services for Unity MCP"""

from .mixamo_service import (
    MixamoService,
    MixamoConfig,
    MixamoError,
    AuthenticationError,
    AnimationNotFoundError,
    DownloadError,
    CharacterError,
    AnimationResult,
    Character,
    DownloadJob,
    ExportFormat,
    expand_search_keywords,
    ANIMATION_KEYWORD_MAP,
)

from .manage_mixamo import (
    manage_mixamo,
    TOOL_NAME,
    TOOL_DESCRIPTION,
    TOOL_SCHEMA,
)

__all__ = [
    # Service
    "MixamoService",
    "MixamoConfig",
    # Errors
    "MixamoError",
    "AuthenticationError",
    "AnimationNotFoundError",
    "DownloadError",
    "CharacterError",
    # Models
    "AnimationResult",
    "Character",
    "DownloadJob",
    "ExportFormat",
    # Utilities
    "expand_search_keywords",
    "ANIMATION_KEYWORD_MAP",
    # MCP Tool
    "manage_mixamo",
    "TOOL_NAME",
    "TOOL_DESCRIPTION",
    "TOOL_SCHEMA",
]
