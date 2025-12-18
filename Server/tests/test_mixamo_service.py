"""
Tests for MixamoService class.
API tests require a valid Mixamo token.
"""

import pytest
from services.mixamo_service import (
    MixamoService,
    MixamoConfig,
    MixamoError,
    AuthenticationError,
    AnimationNotFoundError,
    Character,
    AnimationResult,
    DownloadJob,
    ExportFormat,
)


class TestMixamoConfig:
    """Test MixamoConfig dataclass."""

    def test_default_values(self):
        """Test default configuration values."""
        config = MixamoConfig(access_token="test_token")
        assert config.access_token == "test_token"
        assert config.output_dir == "./downloads"
        assert config.export_format == ExportFormat.FBX_2019
        assert config.fps == 30
        assert config.include_skin is False

    def test_custom_values(self):
        """Test custom configuration."""
        config = MixamoConfig(
            access_token="token",
            output_dir="/custom/path",
            export_format=ExportFormat.FBX_7,
            fps=24,
            include_skin=True,
        )
        assert config.fps == 24
        assert config.include_skin is True
        assert config.export_format == ExportFormat.FBX_7


class TestDataModels:
    """Test data model classes."""

    def test_animation_result_from_api(self):
        """Test AnimationResult creation from API response."""
        data = {
            "id": "anim123",
            "name": "Running",
            "description": "Fast Running Animation",
            "type": "Motion",
        }
        result = AnimationResult.from_api_response(data)
        assert result.id == "anim123"
        assert result.name == "Running"
        assert result.description == "Fast Running Animation"
        assert result.type == "Motion"

    def test_character_from_api(self):
        """Test Character creation from API response."""
        data = {"primary_character_id": "char123", "primary_character_name": "TestChar"}
        char = Character.from_api_response(data)
        assert char.id == "char123"
        assert char.name == "TestChar"

    def test_download_job_defaults(self):
        """Test DownloadJob default values."""
        job = DownloadJob(character_id="char123", animation_name="Run")
        assert job.status == "pending"
        assert job.download_url is None
        assert job.error_message is None


class TestMixamoService:
    """Test MixamoService methods."""

    def test_headers_include_auth(self):
        """Test that headers include authorization."""
        config = MixamoConfig(access_token="test_token_123")
        service = MixamoService(config)

        headers = service.headers
        assert "Authorization" in headers
        assert headers["Authorization"] == "Bearer test_token_123"
        assert headers["X-Api-Key"] == "mixamo2"

    def test_headers_include_content_type(self):
        """Test that headers include content type."""
        config = MixamoConfig(access_token="token")
        service = MixamoService(config)

        headers = service.headers
        assert headers["Content-Type"] == "application/json"
        assert headers["Accept"] == "application/json"


@pytest.mark.asyncio
class TestMixamoServiceAPI:
    """API integration tests - require valid token."""

    async def test_invalid_token_raises_auth_error(self, temp_output_dir):
        """Test that invalid token raises AuthenticationError."""
        config = MixamoConfig(
            access_token="invalid_token_12345", output_dir=temp_output_dir
        )
        service = MixamoService(config)

        try:
            with pytest.raises(AuthenticationError):
                await service.get_primary_character()
        finally:
            await service.close()

    async def test_search_with_valid_token(self, require_token, temp_output_dir):
        """Test animation search with valid token."""
        config = MixamoConfig(access_token=require_token, output_dir=temp_output_dir)
        service = MixamoService(config)

        try:
            results = await service.search_animations("idle", limit=3)
            assert len(results) > 0
            assert all(isinstance(r, AnimationResult) for r in results)
            assert all(r.id for r in results)
        finally:
            await service.close()

    async def test_search_not_found(self, require_token, temp_output_dir):
        """Test search with no results."""
        config = MixamoConfig(access_token=require_token, output_dir=temp_output_dir)
        service = MixamoService(config)

        try:
            with pytest.raises(AnimationNotFoundError):
                await service.search_animations(
                    "xyznonexistent12345abc", expand_keywords=False
                )
        finally:
            await service.close()

    async def test_get_primary_character(self, require_token, temp_output_dir):
        """Test getting primary character."""
        config = MixamoConfig(access_token=require_token, output_dir=temp_output_dir)
        service = MixamoService(config)

        try:
            character = await service.get_primary_character()
            assert isinstance(character, Character)
            assert character.id
            assert character.name
        finally:
            await service.close()

    async def test_export_and_download(self, require_token, temp_output_dir):
        """Test full export and download workflow."""
        config = MixamoConfig(access_token=require_token, output_dir=temp_output_dir)
        service = MixamoService(config)

        try:
            # Get character
            character = await service.get_primary_character()

            # Search for animation
            results = await service.search_animations("idle", limit=1)
            assert len(results) > 0

            # Export
            job = await service.export_animation(character.id, results[0].id)

            assert job.status in ["completed", "failed"]

            if job.status == "completed":
                # Download
                file_path = await service.download_animation(job, temp_output_dir)
                assert file_path.endswith(".fbx")

                import os

                assert os.path.exists(file_path)

        finally:
            await service.close()
