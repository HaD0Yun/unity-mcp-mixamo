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
        name="mixamo-auth",
        description="""Store or validate Mixamo authentication token.

To get your token:
1. Go to https://www.mixamo.com and log in
2. Open browser DevTools (F12)
3. In Console, run: localStorage.access_token
4. Copy the token value (without quotes)

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
                    "description": "Directory to save the FBX file (e.g., 'C:/MyProject/Assets/Animations')",
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
            "required": ["animationIdOrName", "outputDir"],
        },
    ),
    Tool(
        name="mixamo-batch",
        description="""Download multiple animations from Mixamo at once.

Provide a comma-separated list of animation keywords.
Each keyword is searched and the first result is downloaded.

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
                    "description": "Base directory to save FBX files",
                },
                "characterName": {
                    "type": "string",
                    "description": "Character name for folder organization",
                    "default": "Character",
                },
                "characterId": {
                    "type": "string",
                    "description": "Character ID for rigging (default: Mixamo Y-Bot)",
                },
            },
            "required": ["animations", "outputDir"],
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

    if name == "mixamo-auth":
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

    result = await client.search(keyword, limit=limit)

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
    if not output_dir:
        return [TextContent(type="text", text="Error: outputDir is required")]

    result = await client.download(
        animation_id_or_name=animation,
        output_dir=output_dir,
        character_id=character_id,
        file_name=file_name,
    )

    if result.success:
        return [
            TextContent(
                type="text",
                text=f"Downloaded: {result.animation_name}\nSaved to: {result.file_path}",
            )
        ]
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
    character_name = args.get("characterName", "Character")
    character_id = args.get("characterId", DEFAULT_CHARACTER_ID)

    if not animations_str:
        return [TextContent(type="text", text="Error: animations is required")]
    if not output_dir:
        return [TextContent(type="text", text="Error: outputDir is required")]

    keywords = [k.strip() for k in animations_str.split(",") if k.strip()]

    if not keywords:
        return [
            TextContent(type="text", text="Error: No valid animation keywords provided")
        ]

    result = await client.batch_download(
        keywords=keywords,
        output_dir=output_dir,
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
            output_lines.append(f"  ✓ {r.animation_name} -> {r.file_path}")
        else:
            output_lines.append(f"  ✗ {r.animation_name}: {r.error}")

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
    async with stdio_server() as (read_stream, write_stream):
        await server.run(
            read_stream, write_stream, server.create_initialization_options()
        )


def main():
    """Main entry point."""
    asyncio.run(run_server())


if __name__ == "__main__":
    main()
