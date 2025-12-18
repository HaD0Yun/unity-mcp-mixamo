"""
Mixamo MCP Tool

Provides MCP tool interface for fetching and managing Mixamo animations
in Unity projects. This tool integrates with the Unity MCP server to
enable natural language animation requests.

Usage:
    User: "Fetch run, jump, and idle animations for my character"
    Tool: Searches Mixamo, downloads FBX files, returns paths for Unity import
"""

import json
import os
from typing import Any, Optional

# These imports work within the Unity MCP server context
try:
    from .mixamo_service import (
        MixamoService,
        MixamoConfig,
        MixamoError,
        AuthenticationError,
        AnimationNotFoundError,
        ExportFormat,
        expand_search_keywords,
        ANIMATION_KEYWORD_MAP,
    )
except ImportError:
    # Fallback for standalone testing
    from mixamo_service import (
        MixamoService,
        MixamoConfig,
        MixamoError,
        AuthenticationError,
        AnimationNotFoundError,
        ExportFormat,
        expand_search_keywords,
        ANIMATION_KEYWORD_MAP,
    )


# Tool metadata for MCP registration
TOOL_NAME = "manage_mixamo"
TOOL_DESCRIPTION = """
Fetch and manage animations from Mixamo for humanoid characters in Unity.

Operations:
- search: Search for animations by keyword
- fetch: Download animations for the current character
- upload: Upload a custom character for auto-rigging
- list_keywords: Show available animation keyword mappings

Authentication:
Requires a Mixamo/Adobe access token. Get it from browser localStorage 
after logging into mixamo.com (localStorage.access_token).
"""

TOOL_SCHEMA = {
    "type": "object",
    "properties": {
        "operation": {
            "type": "string",
            "enum": ["search", "fetch", "upload", "list_keywords", "configure"],
            "description": "The operation to perform",
        },
        "access_token": {
            "type": "string",
            "description": "Mixamo/Adobe access token (required for API operations)",
        },
        "query": {
            "type": "string",
            "description": "Animation search query (e.g., 'run', 'jump', 'idle')",
        },
        "animations": {
            "type": "array",
            "items": {"type": "string"},
            "description": "List of animation keywords to fetch (e.g., ['run', 'jump', 'idle'])",
        },
        "character_id": {
            "type": "string",
            "description": "Mixamo character UUID (optional, uses primary if not specified)",
        },
        "character_path": {
            "type": "string",
            "description": "Path to character FBX for upload operation",
        },
        "output_dir": {
            "type": "string",
            "description": "Unity project path for downloaded animations",
        },
        "fps": {
            "type": "integer",
            "default": 30,
            "description": "Animation FPS (24 or 30)",
        },
        "include_skin": {
            "type": "boolean",
            "default": False,
            "description": "Include character mesh in animation FBX",
        },
    },
    "required": ["operation"],
}


async def handle_search(
    service: MixamoService, query: str, limit: int = 10
) -> dict[str, Any]:
    """
    Search for animations by keyword.

    Returns list of matching animations with IDs and descriptions.
    """
    try:
        results = await service.search_animations(query, limit=limit)

        return {
            "success": True,
            "query": query,
            "expanded_keywords": expand_search_keywords(query),
            "count": len(results),
            "animations": [
                {
                    "id": r.id,
                    "name": r.name,
                    "description": r.description,
                    "type": r.type,
                }
                for r in results
            ],
        }

    except AnimationNotFoundError:
        return {
            "success": False,
            "error": f"No animations found for '{query}'",
            "suggestions": list(ANIMATION_KEYWORD_MAP.keys()),
        }
    except MixamoError as e:
        return {"success": False, "error": str(e)}


async def handle_fetch(
    service: MixamoService,
    animations: list[str],
    output_dir: str,
    character_id: Optional[str] = None,
) -> dict[str, Any]:
    """
    Fetch multiple animations for a character.

    Downloads FBX files to the specified output directory.
    """
    try:
        # Get character ID if not provided
        if not character_id:
            character = await service.get_primary_character()
            character_id = character.id
            character_name = character.name
        else:
            character_name = "Custom Character"

        # Ensure output directory exists
        os.makedirs(output_dir, exist_ok=True)

        # Fetch all animations
        results = await service.fetch_animations_for_character(
            character_id, animations, output_dir
        )

        # Summarize results
        completed = [r for r in results if r["status"] == "completed"]
        failed = [r for r in results if r["status"] != "completed"]

        return {
            "success": len(completed) > 0,
            "character": {"id": character_id, "name": character_name},
            "output_dir": output_dir,
            "summary": {
                "requested": len(animations),
                "completed": len(completed),
                "failed": len(failed),
            },
            "animations": results,
            "unity_import_paths": [
                r["file_path"] for r in completed if r.get("file_path")
            ],
        }

    except MixamoError as e:
        return {"success": False, "error": str(e)}


async def handle_upload(
    service: MixamoService, character_path: str, character_name: Optional[str] = None
) -> dict[str, Any]:
    """
    Upload a custom character for auto-rigging.

    Returns the character ID for subsequent animation fetches.
    """
    try:
        character = await service.upload_character(character_path, character_name)

        return {
            "success": True,
            "character": {"id": character.id, "name": character.name},
            "message": f"Character '{character.name}' uploaded and rigged successfully",
        }

    except MixamoError as e:
        return {"success": False, "error": str(e)}


def handle_list_keywords() -> dict[str, Any]:
    """
    List available animation keyword mappings.

    Helps users understand what keywords map to which Mixamo searches.
    """
    categories = {}

    # Group keywords by category
    locomotion = ["run", "walk", "jump", "crouch", "crawl"]
    idle = ["idle", "sit", "lay"]
    combat = ["attack", "punch", "kick", "sword", "shoot", "block", "dodge"]
    interactions = ["pick", "throw", "push", "pull"]
    expressions = ["wave", "dance", "die", "hurt", "celebrate", "taunt"]
    traversal = ["climb", "hang", "swim", "fly"]

    return {
        "success": True,
        "keyword_categories": {
            "locomotion": {
                k: ANIMATION_KEYWORD_MAP.get(k, [])
                for k in locomotion
                if k in ANIMATION_KEYWORD_MAP
            },
            "idle_stance": {
                k: ANIMATION_KEYWORD_MAP.get(k, [])
                for k in idle
                if k in ANIMATION_KEYWORD_MAP
            },
            "combat": {
                k: ANIMATION_KEYWORD_MAP.get(k, [])
                for k in combat
                if k in ANIMATION_KEYWORD_MAP
            },
            "interactions": {
                k: ANIMATION_KEYWORD_MAP.get(k, [])
                for k in interactions
                if k in ANIMATION_KEYWORD_MAP
            },
            "expressions": {
                k: ANIMATION_KEYWORD_MAP.get(k, [])
                for k in expressions
                if k in ANIMATION_KEYWORD_MAP
            },
            "traversal": {
                k: ANIMATION_KEYWORD_MAP.get(k, [])
                for k in traversal
                if k in ANIMATION_KEYWORD_MAP
            },
        },
        "usage": "Use any keyword as a search term. The system will expand it to find relevant animations.",
    }


async def manage_mixamo(
    operation: str,
    access_token: Optional[str] = None,
    query: Optional[str] = None,
    animations: Optional[list[str]] = None,
    character_id: Optional[str] = None,
    character_path: Optional[str] = None,
    output_dir: Optional[str] = None,
    fps: int = 30,
    include_skin: bool = False,
    **kwargs,
) -> dict[str, Any]:
    """
    Main entry point for the Mixamo MCP tool.

    This function is called by the MCP server to handle Mixamo operations.

    Args:
        operation: The operation to perform
        access_token: Mixamo/Adobe access token
        query: Search query for animations
        animations: List of animation keywords to fetch
        character_id: Optional character UUID
        character_path: Path to character FBX for upload
        output_dir: Directory for downloaded animations
        fps: Animation FPS
        include_skin: Include mesh in animation FBX

    Returns:
        Operation result dictionary
    """

    # Handle non-authenticated operations
    if operation == "list_keywords":
        return handle_list_keywords()

    # Validate access token for authenticated operations
    if not access_token:
        return {
            "success": False,
            "error": "Access token required. Get it from mixamo.com browser console: localStorage.access_token",
            "hint": "Log into mixamo.com, open browser DevTools (F12), Console tab, type: localStorage.access_token",
        }

    # Create service with config
    config = MixamoConfig(
        access_token=access_token,
        output_dir=output_dir or "./mixamo_downloads",
        fps=fps,
        include_skin=include_skin,
    )

    service = MixamoService(config)

    try:
        if operation == "search":
            if not query:
                return {
                    "success": False,
                    "error": "Query required for search operation",
                }
            return await handle_search(service, query)

        elif operation == "fetch":
            if not animations:
                return {
                    "success": False,
                    "error": "Animations list required for fetch operation",
                }
            if not output_dir:
                return {
                    "success": False,
                    "error": "Output directory required for fetch operation",
                }
            return await handle_fetch(service, animations, output_dir, character_id)

        elif operation == "upload":
            if not character_path:
                return {
                    "success": False,
                    "error": "Character path required for upload operation",
                }
            return await handle_upload(service, character_path)

        elif operation == "configure":
            return {
                "success": True,
                "current_config": {
                    "fps": fps,
                    "include_skin": include_skin,
                    "export_format": "fbx7_2019",
                },
                "available_formats": ["fbx7_2019", "fbx7", "dae_mixamo"],
            }

        else:
            return {
                "success": False,
                "error": f"Unknown operation: {operation}",
                "available_operations": [
                    "search",
                    "fetch",
                    "upload",
                    "list_keywords",
                    "configure",
                ],
            }

    finally:
        await service.close()


# Export for MCP registration
__all__ = ["manage_mixamo", "TOOL_NAME", "TOOL_DESCRIPTION", "TOOL_SCHEMA"]
