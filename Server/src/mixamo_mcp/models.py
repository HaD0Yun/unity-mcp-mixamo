"""Data models for Mixamo MCP server."""

from dataclasses import dataclass, field
from typing import Optional
from enum import Enum


class AnimationCategory(str, Enum):
    """Animation category types."""

    LOCOMOTION = "locomotion"
    COMBAT = "combat"
    SOCIAL = "social"
    DANCE = "dance"
    SPORTS = "sports"
    MISC = "misc"


@dataclass
class Animation:
    """Represents a Mixamo animation."""

    id: str
    name: str
    description: str = ""
    motion_id: str = ""
    thumbnail_url: str = ""
    category: AnimationCategory = AnimationCategory.MISC

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "name": self.name,
            "description": self.description,
            "motion_id": self.motion_id,
            "thumbnail_url": self.thumbnail_url,
            "category": self.category.value,
        }


@dataclass
class Character:
    """Represents a Mixamo character."""

    id: str
    name: str
    thumbnail_url: str = ""

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "name": self.name,
            "thumbnail_url": self.thumbnail_url,
        }


@dataclass
class DownloadResult:
    """Result of a download operation."""

    success: bool
    animation_name: str
    file_path: str = ""
    error: str = ""

    def to_dict(self) -> dict:
        return {
            "success": self.success,
            "animation_name": self.animation_name,
            "file_path": self.file_path,
            "error": self.error,
        }


@dataclass
class BatchDownloadResult:
    """Result of a batch download operation."""

    total: int
    successful: int
    failed: int
    results: list[DownloadResult] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "total": self.total,
            "successful": self.successful,
            "failed": self.failed,
            "results": [r.to_dict() for r in self.results],
        }


@dataclass
class SearchResult:
    """Result of a search operation."""

    query: str
    total: int
    animations: list[Animation] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "query": self.query,
            "total": self.total,
            "animations": [a.to_dict() for a in self.animations],
        }


# Default character IDs
DEFAULT_CHARACTER_ID = "bd5c7f38-3eda-4bf5-9fb8-cc338e1bde8a"  # Y-Bot
YBOT_CHARACTER_ID = DEFAULT_CHARACTER_ID
XBOT_CHARACTER_ID = "e197db12-4260-4c85-831f-723609a71c5d"  # X-Bot
