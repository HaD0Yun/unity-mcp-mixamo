"""Mixamo MCP Server - Main entry point."""

import asyncio
import json
from typing import Any

from mcp.server import Server
from mcp.server.stdio import stdio_server
from mcp.types import Tool, TextContent

from .client import MixamoClient
from .keywords import get_all_keywords, filter_keywords_by_category, get_search_queries
from .models import DEFAULT_CHARACTER_ID


# Initialize server and client
server = Server("mixamo-mcp")
client = MixamoClient()


# Tool definitions
TOOLS = [
    Tool(
        name="mixamo-config",
        description="""Configure Mixamo MCP settings, especially Unity project path.

Set your Unity project path once, and all downloads will automatically go to the Assets/Animations folder.
Unity will auto-detect new files and import them!

Example: mixamo-config unityProjectPath="D:/MyGame" """,
        inputSchema={
            "type": "object",
            "properties": {
                "unityProjectPath": {
                    "type": "string",
                    "description": "Path to your Unity project root (containing Assets folder). Example: 'D:/MyGame' or 'C:/Users/Me/Projects/MyGame'",
                },
                "characterName": {
                    "type": "string",
                    "description": "Default character name for organizing animations (default: 'Player')",
                },
                "animationsSubfolder": {
                    "type": "string",
                    "description": "Subfolder under Assets for animations (default: 'Animations')",
                },
                "show": {
                    "type": "boolean",
                    "description": "Show current configuration",
                    "default": False,
                },
            },
        },
    ),
    Tool(
        name="mixamo-auth",
        description="""Store or validate Mixamo authentication token.

To get your token:
1. Go to https://www.mixamo.com and log in
2. Open browser DevTools (F12)
3. In Console, run: copy(localStorage.access_token)
4. Paste the token here

The token is stored securely and persists across sessions.""",
        inputSchema={
            "type": "object",
            "properties": {
                "accessToken": {
                    "type": "string",
                    "description": "Adobe access_token from Mixamo localStorage. Leave empty to check current token status.",
                },
                "clear": {
                    "type": "boolean",
                    "description": "Set to true to clear stored token",
                    "default": False,
                },
            },
        },
    ),
    Tool(
        name="mixamo-search",
        description="""Search for animations on Mixamo by keyword.

Supports natural language keywords like 'run', 'jump', 'attack', 'idle', etc.
Keywords are automatically mapped to Mixamo search queries for best results.

Example: 'run' -> searches for 'running', 'run', 'jog', 'sprint'""",
        inputSchema={
            "type": "object",
            "properties": {
                "keyword": {
                    "type": "string",
                    "description": "Search keyword (e.g., 'run', 'jump', 'idle', 'attack', 'dance')",
                },
                "limit": {
                    "type": "integer",
                    "description": "Maximum number of results to return (default: 10, max: 50)",
                    "default": 10,
                },
                "showQueries": {
                    "type": "boolean",
                    "description": "Show all alternative search queries for the keyword",
                    "default": False,
                },
            },
            "required": ["keyword"],
        },
    ),
    Tool(
        name="mixamo-download",
        description="""Download a single animation from Mixamo.

The animation is downloaded as FBX format.
If Unity project is configured (via mixamo-config), files auto-save to Assets/Animations and Unity imports automatically!

Use 'mixamo-search' first to find animation IDs, or just provide a keyword.""",
        inputSchema={
            "type": "object",
            "properties": {
                "animationIdOrName": {
                    "type": "string",
                    "description": "Animation ID from search results (or animation name/keyword to search and download first result)",
                },
                "outputDir": {
                    "type": "string",
                    "description": "Directory to save the FBX file. Optional if Unity project is configured via mixamo-config.",
                },
                "characterId": {
                    "type": "string",
                    "description": "Character ID for rigging (default: Mixamo Y-Bot)",
                },
                "fileName": {
                    "type": "string",
                    "description": "Custom file name (default: uses animation name)",
                },
            },
            "required": ["animationIdOrName"],
        },
    ),
    Tool(
        name="mixamo-batch",
        description="""Download multiple animations from Mixamo at once.

Provide a comma-separated list of animation keywords.
If Unity project is configured (via mixamo-config), files auto-save to Assets/Animations/{characterName}/ and Unity imports automatically!

Example: 'idle, walk, run, jump, attack'""",
        inputSchema={
            "type": "object",
            "properties": {
                "animations": {
                    "type": "string",
                    "description": "Animation keywords (comma-separated: 'idle,walk,run,jump,attack')",
                },
                "outputDir": {
                    "type": "string",
                    "description": "Base directory to save FBX files. Optional if Unity project is configured via mixamo-config.",
                },
                "characterName": {
                    "type": "string",
                    "description": "Character name for folder organization (uses config default if not specified)",
                },
                "characterId": {
                    "type": "string",
                    "description": "Character ID for rigging (default: Mixamo Y-Bot)",
                },
            },
            "required": ["animations"],
        },
    ),
    Tool(
        name="mixamo-keywords",
        description="""List all available animation keywords and their Mixamo search mappings.

Use these keywords with mixamo-search and mixamo-batch for best results.""",
        inputSchema={
            "type": "object",
            "properties": {
                "filter": {
                    "type": "string",
                    "description": "Filter by keyword category (e.g., 'combat', 'locomotion', 'social', 'dance')",
                },
            },
        },
    ),
]


@server.list_tools()
async def list_tools() -> list[Tool]:
    """List available tools."""
    return TOOLS


@server.call_tool()
async def call_tool(name: str, arguments: dict[str, Any]) -> list[TextContent]:
    """Handle tool calls."""

    if name == "mixamo-config":
        return await handle_config(arguments)
    elif name == "mixamo-auth":
        return await handle_auth(arguments)
    elif name == "mixamo-search":
        return await handle_search(arguments)
    elif name == "mixamo-download":
        return await handle_download(arguments)
    elif name == "mixamo-batch":
        return await handle_batch(arguments)
    elif name == "mixamo-keywords":
        return await handle_keywords(arguments)
    else:
        return [TextContent(type="text", text=f"Unknown tool: {name}")]


async def handle_config(args: dict[str, Any]) -> list[TextContent]:
    """Handle mixamo-config tool."""
    show = args.get("show", False)
    unity_path = args.get("unityProjectPath")
    char_name = args.get("characterName")
    anim_subfolder = args.get("animationsSubfolder")

    # If only show is requested or no args
    if show or (not unity_path and not char_name and not anim_subfolder):
        config = client.config
        project_info = client.get_detected_project_info()

        lines = ["Mixamo MCP Status:"]
        lines.append("")

        # Show configured vs detected
        if project_info["configured"]:
            lines.append(
                f"  [OK] Unity Project (configured): {project_info['configured']}"
            )
        elif project_info["detected"]:
            lines.append(
                f"  [AUTO] Unity Project (detected): {project_info['detected']}"
            )
        else:
            lines.append("  [!] Unity Project: Not found")

        lines.append(f"  Character Name: {config.default_character_name}")
        lines.append(f"  Animations Folder: {config.animations_subfolder}")

        if project_info["output_path"]:
            lines.append(f"  Output Path: {project_info['output_path']}")
            lines.append("")
            lines.append('Ready! Just run: mixamo-batch animations="idle,walk,run"')
        else:
            lines.append("")
            lines.append("No Unity project detected. Options:")
            lines.append("  1. Open Unity with your project")
            lines.append(
                '  2. Or set manually: mixamo-config unityProjectPath="D:/MyGame"'
            )

        return [TextContent(type="text", text="\n".join(lines))]

    # Update config
    update_msgs = []

    if unity_path:
        if client.set_unity_project(unity_path):
            update_msgs.append(f"Unity project set to: {unity_path}")
            update_msgs.append(
                f"Animations will save to: {client.config.get_unity_animations_path()}"
            )
        else:
            return [
                TextContent(
                    type="text",
                    text=f"Error: '{unity_path}' doesn't look like a Unity project (no Assets folder found).",
                )
            ]

    if char_name:
        client.set_config(default_character_name=char_name)
        update_msgs.append(f"Default character name set to: {char_name}")

    if anim_subfolder:
        client.set_config(animations_subfolder=anim_subfolder)
        update_msgs.append(f"Animations subfolder set to: {anim_subfolder}")

    update_msgs.append("")
    update_msgs.append("Downloads will now auto-import to Unity!")

    return [TextContent(type="text", text="\n".join(update_msgs))]


async def handle_auth(args: dict[str, Any]) -> list[TextContent]:
    """Handle mixamo-auth tool."""
    clear = args.get("clear", False)
    token = args.get("accessToken", "")

    if clear:
        client.clear_token()
        return [TextContent(type="text", text="Token cleared successfully.")]

    if token:
        client.access_token = token
        is_valid = await client.validate_token()
        if is_valid:
            return [
                TextContent(
                    type="text", text="Token stored and validated successfully!"
                )
            ]
        else:
            return [
                TextContent(
                    type="text",
                    text="Warning: Token stored but validation failed. It may be expired or invalid.",
                )
            ]

    # Check current token
    if client.access_token:
        is_valid = await client.validate_token()
        status = "valid" if is_valid else "invalid/expired"
        return [TextContent(type="text", text=f"Token is stored. Status: {status}")]
    else:
        return [
            TextContent(
                type="text", text="No token stored. Please provide an access token."
            )
        ]


async def handle_search(args: dict[str, Any]) -> list[TextContent]:
    """Handle mixamo-search tool."""
    keyword = args.get("keyword", "")
    limit = min(args.get("limit", 10), 50)
    show_queries = args.get("showQueries", False)

    if not keyword:
        return [TextContent(type="text", text="Error: keyword is required")]

    # Check for authentication
    if not client.access_token:
        return [
            TextContent(
                type="text",
                text="Error: Not authenticated.\n\nPlease run mixamo-auth first:\n"
                "1. Go to https://www.mixamo.com and log in\n"
                "2. Open browser DevTools (F12)\n"
                "3. In Console, run: copy(localStorage.access_token)\n"
                '4. Then run: mixamo-auth accessToken="YOUR_TOKEN"',
            )
        ]

    result = await client.search(keyword, limit=limit)

    # Check for errors in search result
    if result.error:
        return [TextContent(type="text", text=f"Error: {result.error}")]

    output_lines = [f"Search results for '{keyword}':"]

    if show_queries:
        queries = get_search_queries(keyword)
        output_lines.append(f"Search queries used: {', '.join(queries)}")

    output_lines.append(f"Found {result.total} animations:\n")

    for i, anim in enumerate(result.animations, 1):
        output_lines.append(f"{i}. {anim.name}")
        output_lines.append(f"   ID: {anim.id}")
        if anim.description:
            output_lines.append(f"   Description: {anim.description[:100]}...")
        output_lines.append("")

    return [TextContent(type="text", text="\n".join(output_lines))]


async def handle_download(args: dict[str, Any]) -> list[TextContent]:
    """Handle mixamo-download tool."""
    animation = args.get("animationIdOrName", "")
    output_dir = args.get("outputDir", "")
    character_id = args.get("characterId", DEFAULT_CHARACTER_ID)
    file_name = args.get("fileName")

    if not animation:
        return [TextContent(type="text", text="Error: animationIdOrName is required")]

    # Check for authentication
    if not client.access_token:
        return [
            TextContent(
                type="text",
                text="Error: Not authenticated.\n\nPlease run mixamo-auth first:\n"
                "1. Go to https://www.mixamo.com and log in\n"
                "2. Open browser DevTools (F12)\n"
                "3. In Console, run: copy(localStorage.access_token)\n"
                '4. Then run: mixamo-auth accessToken="YOUR_TOKEN"',
            )
        ]

    # Use Unity project path if outputDir not specified
    final_output_dir = client.get_output_dir(output_dir if output_dir else None)
    if not final_output_dir:
        return [
            TextContent(
                type="text",
                text="Error: outputDir is required (or configure Unity project with mixamo-config)",
            )
        ]

    result = await client.download(
        animation_id_or_name=animation,
        output_dir=final_output_dir,
        character_id=character_id,
        file_name=file_name,
    )

    if result.success:
        msg = f"Downloaded: {result.animation_name}\nSaved to: {result.file_path}"
        if client.config.unity_project_path and not output_dir:
            msg += "\n\nUnity will auto-import this file!"
        return [TextContent(type="text", text=msg)]
    else:
        return [
            TextContent(
                type="text",
                text=f"Failed to download '{result.animation_name}': {result.error}",
            )
        ]


async def handle_batch(args: dict[str, Any]) -> list[TextContent]:
    """Handle mixamo-batch tool."""
    animations_str = args.get("animations", "")
    output_dir = args.get("outputDir", "")
    character_name = args.get("characterName") or client.config.default_character_name
    character_id = args.get("characterId", DEFAULT_CHARACTER_ID)

    if not animations_str:
        return [TextContent(type="text", text="Error: animations is required")]

    # Check for authentication
    if not client.access_token:
        return [
            TextContent(
                type="text",
                text="Error: Not authenticated.\n\nPlease run mixamo-auth first:\n"
                "1. Go to https://www.mixamo.com and log in\n"
                "2. Open browser DevTools (F12)\n"
                "3. In Console, run: copy(localStorage.access_token)\n"
                '4. Then run: mixamo-auth accessToken="YOUR_TOKEN"',
            )
        ]

    # Use Unity project path if outputDir not specified
    final_output_dir = client.get_output_dir(output_dir if output_dir else None)
    if not final_output_dir:
        return [
            TextContent(
                type="text",
                text="Error: outputDir is required (or configure Unity project with mixamo-config)",
            )
        ]

    keywords = [k.strip() for k in animations_str.split(",") if k.strip()]

    if not keywords:
        return [
            TextContent(type="text", text="Error: No valid animation keywords provided")
        ]

    result = await client.batch_download(
        keywords=keywords,
        output_dir=final_output_dir,
        character_id=character_id,
        character_name=character_name,
    )

    output_lines = [
        f"Batch download complete:",
        f"  Total: {result.total}",
        f"  Successful: {result.successful}",
        f"  Failed: {result.failed}",
        "",
        "Results:",
    ]

    for r in result.results:
        if r.success:
            output_lines.append(f"  [OK] {r.animation_name} -> {r.file_path}")
        else:
            output_lines.append(f"  [FAIL] {r.animation_name}: {r.error}")

    if client.config.unity_project_path and not output_dir:
        output_lines.append("")
        output_lines.append("Unity will auto-import these files!")

    return [TextContent(type="text", text="\n".join(output_lines))]


async def handle_keywords(args: dict[str, Any]) -> list[TextContent]:
    """Handle mixamo-keywords tool."""
    filter_cat = args.get("filter")

    keywords = filter_keywords_by_category(filter_cat)

    output_lines = ["Available animation keywords:"]

    if filter_cat:
        output_lines[0] = f"Animation keywords (filtered by '{filter_cat}'):"

    for category, kw_list in sorted(keywords.items()):
        output_lines.append(f"\n{category.upper()}:")
        output_lines.append(f"  {', '.join(sorted(kw_list))}")

    output_lines.append("\n\nUse these keywords with mixamo-search or mixamo-batch.")

    return [TextContent(type="text", text="\n".join(output_lines))]


async def run_server():
    """Run the MCP server."""
    try:
        async with stdio_server() as (read_stream, write_stream):
            await server.run(
                read_stream, write_stream, server.create_initialization_options()
            )
    finally:
        # Ensure HTTP client is properly closed
        await client.close()


def main():
    """Main entry point."""
    asyncio.run(run_server())


if __name__ == "__main__":
    main()
