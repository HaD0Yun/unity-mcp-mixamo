"""
Tests for the manage_mixamo MCP tool interface.
"""

import pytest
from services.manage_mixamo import (
    manage_mixamo,
    handle_list_keywords,
    TOOL_NAME,
    TOOL_DESCRIPTION,
    TOOL_SCHEMA,
)
from services.mixamo_service import ANIMATION_KEYWORD_MAP


class TestToolMetadata:
    """Test tool registration metadata."""

    def test_tool_name(self):
        """Test tool has correct name."""
        assert TOOL_NAME == "manage_mixamo"

    def test_tool_description(self):
        """Test tool has description."""
        assert len(TOOL_DESCRIPTION) > 50
        assert "Mixamo" in TOOL_DESCRIPTION

    def test_tool_schema_structure(self):
        """Test tool schema has required fields."""
        assert TOOL_SCHEMA["type"] == "object"
        assert "properties" in TOOL_SCHEMA
        assert "operation" in TOOL_SCHEMA["properties"]
        assert "required" in TOOL_SCHEMA

    def test_tool_schema_operations(self):
        """Test all operations are in schema."""
        operations = TOOL_SCHEMA["properties"]["operation"]["enum"]
        expected = ["search", "fetch", "upload", "list_keywords", "configure"]
        for op in expected:
            assert op in operations


class TestListKeywords:
    """Test list_keywords operation."""

    def test_list_keywords_success(self):
        """Test list_keywords returns success."""
        result = handle_list_keywords()
        assert result["success"] is True

    def test_list_keywords_has_categories(self):
        """Test list_keywords returns categorized keywords."""
        result = handle_list_keywords()
        categories = result["keyword_categories"]

        expected_categories = [
            "locomotion",
            "idle_stance",
            "combat",
            "interactions",
            "expressions",
            "traversal",
        ]

        for cat in expected_categories:
            assert cat in categories

    def test_list_keywords_has_usage(self):
        """Test list_keywords includes usage hint."""
        result = handle_list_keywords()
        assert "usage" in result
        assert len(result["usage"]) > 0


@pytest.mark.asyncio
class TestManageMixamoTool:
    """Test main manage_mixamo function."""

    async def test_list_keywords_operation(self):
        """Test list_keywords via main function."""
        result = await manage_mixamo(operation="list_keywords")
        assert result["success"] is True
        assert "keyword_categories" in result

    async def test_missing_token_error(self):
        """Test operations fail gracefully without token."""
        result = await manage_mixamo(
            operation="search",
            query="run",
            # No access_token
        )
        assert result["success"] is False
        assert "token" in result["error"].lower()

    async def test_search_missing_query(self):
        """Test search fails without query."""
        result = await manage_mixamo(
            operation="search",
            access_token="fake_token",
            # No query
        )
        assert result["success"] is False
        assert "query" in result["error"].lower()

    async def test_fetch_missing_animations(self):
        """Test fetch fails without animations list."""
        result = await manage_mixamo(
            operation="fetch",
            access_token="fake_token",
            output_dir="/tmp/test",
            # No animations
        )
        assert result["success"] is False
        assert "animations" in result["error"].lower()

    async def test_fetch_missing_output_dir(self):
        """Test fetch fails without output directory."""
        result = await manage_mixamo(
            operation="fetch",
            access_token="fake_token",
            animations=["run", "idle"],
            # No output_dir
        )
        assert result["success"] is False
        assert "output" in result["error"].lower()

    async def test_upload_missing_path(self):
        """Test upload fails without character path."""
        result = await manage_mixamo(
            operation="upload",
            access_token="fake_token",
            # No character_path
        )
        assert result["success"] is False
        assert "path" in result["error"].lower()

    async def test_unknown_operation(self):
        """Test unknown operation returns error."""
        result = await manage_mixamo(
            operation="invalid_operation", access_token="token"
        )
        assert result["success"] is False
        assert "unknown" in result["error"].lower()
        assert "available_operations" in result

    async def test_configure_operation(self):
        """Test configure returns current settings."""
        result = await manage_mixamo(
            operation="configure", access_token="token", fps=24, include_skin=True
        )
        assert result["success"] is True
        assert "current_config" in result
        assert result["current_config"]["fps"] == 24


@pytest.mark.asyncio
class TestManageMixamoAPI:
    """API integration tests for manage_mixamo."""

    async def test_search_operation(self, require_token):
        """Test search operation with valid token."""
        result = await manage_mixamo(
            operation="search", access_token=require_token, query="idle"
        )

        assert result["success"] is True
        assert "animations" in result
        assert len(result["animations"]) > 0
        assert "expanded_keywords" in result

    async def test_fetch_operation(self, require_token, temp_output_dir):
        """Test fetch operation with valid token."""
        result = await manage_mixamo(
            operation="fetch",
            access_token=require_token,
            animations=["idle"],
            output_dir=temp_output_dir,
        )

        # May succeed or fail based on rate limits
        assert "success" in result
        if result["success"]:
            assert "unity_import_paths" in result
            assert "summary" in result
