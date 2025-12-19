"""Mixamo HTTP client for API interactions."""

import asyncio
import json
import os
import time
from pathlib import Path
from typing import Optional
from dataclasses import dataclass, asdict

import httpx

from .models import (
    Animation,
    Character,
    DownloadResult,
    BatchDownloadResult,
    SearchResult,
    DEFAULT_CHARACTER_ID,
)
from .keywords import get_search_queries


@dataclass
class MixamoConfig:
    """Configuration for Mixamo MCP."""

    unity_project_path: str = ""
    default_character_name: str = "Player"
    animations_subfolder: str = "Animations"

    def get_unity_animations_path(self) -> Optional[str]:
        """Get full path to Unity Animations folder."""
        if not self.unity_project_path:
            return None
        return str(Path(self.unity_project_path) / "Assets" / self.animations_subfolder)


class MixamoClient:
    """HTTP client for Mixamo API."""

    BASE_URL = "https://www.mixamo.com/api/v1"
    CDN_URL = "https://www.mixamo.com"

    # Storage files
    TOKEN_FILE = Path.home() / ".mixamo_mcp_token"
    CONFIG_FILE = Path.home() / ".mixamo_mcp_config.json"

    def __init__(self, access_token: Optional[str] = None):
        self._access_token = access_token
        self._client: Optional[httpx.AsyncClient] = None
        self._config: MixamoConfig = MixamoConfig()

        # Load token and config from files
        if not self._access_token:
            self._access_token = self._load_token()
        self._load_config()

    @property
    def access_token(self) -> Optional[str]:
        return self._access_token

    @access_token.setter
    def access_token(self, value: str):
        self._access_token = value
        self._save_token(value)

    @property
    def config(self) -> MixamoConfig:
        return self._config

    def _load_token(self) -> Optional[str]:
        """Load token from file."""
        if self.TOKEN_FILE.exists():
            try:
                return self.TOKEN_FILE.read_text().strip()
            except Exception:
                pass
        return None

    def _save_token(self, token: str) -> None:
        """Save token to file."""
        try:
            self.TOKEN_FILE.write_text(token)
            # Set restrictive permissions on Unix
            if os.name != "nt":
                os.chmod(self.TOKEN_FILE, 0o600)
        except Exception as e:
            print(f"Warning: Could not save token: {e}")

    def clear_token(self) -> None:
        """Clear stored token."""
        self._access_token = None
        if self.TOKEN_FILE.exists():
            try:
                self.TOKEN_FILE.unlink()
            except Exception:
                pass

    def _load_config(self) -> None:
        """Load config from file."""
        if self.CONFIG_FILE.exists():
            try:
                data = json.loads(self.CONFIG_FILE.read_text())
                self._config = MixamoConfig(
                    unity_project_path=data.get("unity_project_path", ""),
                    default_character_name=data.get("default_character_name", "Player"),
                    animations_subfolder=data.get("animations_subfolder", "Animations"),
                )
            except Exception:
                self._config = MixamoConfig()
        else:
            self._config = MixamoConfig()

    def _save_config(self) -> None:
        """Save config to file."""
        try:
            self.CONFIG_FILE.write_text(json.dumps(asdict(self._config), indent=2))
        except Exception as e:
            print(f"Warning: Could not save config: {e}")

    def set_unity_project(self, path: str) -> bool:
        """Set Unity project path and validate it."""
        project_path = Path(path)
        assets_path = project_path / "Assets"

        # Validate it looks like a Unity project
        if not assets_path.exists():
            return False

        self._config.unity_project_path = str(project_path)
        self._save_config()
        return True

    def set_config(
        self,
        unity_project_path: Optional[str] = None,
        default_character_name: Optional[str] = None,
        animations_subfolder: Optional[str] = None,
    ) -> None:
        """Update config values."""
        if unity_project_path is not None:
            self._config.unity_project_path = unity_project_path
        if default_character_name is not None:
            self._config.default_character_name = default_character_name
        if animations_subfolder is not None:
            self._config.animations_subfolder = animations_subfolder
        self._save_config()

    def get_output_dir(self, custom_dir: Optional[str] = None) -> Optional[str]:
        """Get output directory, using Unity project if configured."""
        if custom_dir:
            return custom_dir
        return self._config.get_unity_animations_path()

    def _get_headers(self) -> dict:
        """Get request headers."""
        headers = {
            "Accept": "application/json",
            "Content-Type": "application/json",
            "X-Requested-With": "XMLHttpRequest",
            "X-Api-Key": "mixamo2",
        }
        if self._access_token:
            headers["Authorization"] = f"Bearer {self._access_token}"
        return headers

    async def _get_client(self) -> httpx.AsyncClient:
        """Get or create HTTP client."""
        if self._client is None or self._client.is_closed:
            self._client = httpx.AsyncClient(
                timeout=httpx.Timeout(60.0, connect=10.0),
                follow_redirects=True,
            )
        return self._client

    async def close(self) -> None:
        """Close the HTTP client."""
        if self._client and not self._client.is_closed:
            await self._client.aclose()
            self._client = None

    async def validate_token(self) -> bool:
        """Validate the current access token."""
        if not self._access_token:
            return False

        try:
            client = await self._get_client()
            response = await client.get(
                f"{self.BASE_URL}/characters",
                headers=self._get_headers(),
                params={"page": 1, "limit": 1},
            )
            return response.status_code == 200
        except Exception:
            return False

    async def search(
        self,
        query: str,
        limit: int = 10,
        character_id: str = DEFAULT_CHARACTER_ID,
    ) -> SearchResult:
        """Search for animations."""
        client = await self._get_client()

        # Get search queries for the keyword
        queries = get_search_queries(query)
        all_animations: list[Animation] = []
        seen_ids: set[str] = set()

        for search_query in queries:
            if len(all_animations) >= limit:
                break

            try:
                response = await client.get(
                    f"{self.BASE_URL}/products",
                    headers=self._get_headers(),
                    params={
                        "page": 1,
                        "limit": min(limit, 24),
                        "order": "",
                        "query": search_query,
                        "type": "Motion,MotionPack",
                    },
                )

                if response.status_code != 200:
                    continue

                data = response.json()
                results = data.get("results", [])

                for item in results:
                    anim_id = item.get("id", "")
                    if anim_id and anim_id not in seen_ids:
                        seen_ids.add(anim_id)
                        all_animations.append(
                            Animation(
                                id=anim_id,
                                name=item.get("name", ""),
                                description=item.get("description", ""),
                                motion_id=item.get("motion_id", ""),
                                thumbnail_url=item.get("thumbnail", ""),
                            )
                        )

                        if len(all_animations) >= limit:
                            break

            except Exception as e:
                print(f"Search error for '{search_query}': {e}")
                continue

        return SearchResult(
            query=query,
            total=len(all_animations),
            animations=all_animations[:limit],
        )

    def _is_uuid(self, value: str) -> bool:
        """Check if value looks like a UUID."""
        import re

        uuid_pattern = re.compile(
            r"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            re.IGNORECASE,
        )
        return bool(uuid_pattern.match(value))

    async def download(
        self,
        animation_id_or_name: str,
        output_dir: str,
        character_id: str = DEFAULT_CHARACTER_ID,
        file_name: Optional[str] = None,
    ) -> DownloadResult:
        """Download a single animation."""
        client = await self._get_client()

        # If it doesn't look like a UUID, search for it first
        animation_id = animation_id_or_name
        animation_name = animation_id_or_name

        if not self._is_uuid(animation_id_or_name):
            # Not a UUID, search for it
            search_result = await self.search(animation_id_or_name, limit=1)
            if search_result.animations:
                animation_id = search_result.animations[0].id
                animation_name = search_result.animations[0].name
            else:
                return DownloadResult(
                    success=False,
                    animation_name=animation_id_or_name,
                    error=f"Animation not found: {animation_id_or_name}",
                )

        try:
            # Request export
            export_response = await client.post(
                f"{self.BASE_URL}/animations/export",
                headers=self._get_headers(),
                json={
                    "character_id": character_id,
                    "product_name": animation_id,
                    "preferences": {
                        "format": "fbx7",
                        "skin": "false",
                        "fps": "30",
                        "reducekf": "0",
                    },
                    "gms_hash": None,
                },
            )

            if export_response.status_code not in (200, 202):
                return DownloadResult(
                    success=False,
                    animation_name=animation_name,
                    error=f"Export request failed: {export_response.status_code}",
                )

            # Poll for download URL
            download_url = None
            for _ in range(30):  # Max 30 attempts
                await asyncio.sleep(2)

                monitor_response = await client.get(
                    f"{self.BASE_URL}/animations/monitor",
                    headers=self._get_headers(),
                    params={
                        "character_id": character_id,
                        "product_name": animation_id,
                    },
                )

                if monitor_response.status_code != 200:
                    continue

                monitor_data = monitor_response.json()
                status = monitor_data.get("status", "")

                if status == "completed":
                    download_url = monitor_data.get("job_result", "")
                    break
                elif status == "failed":
                    return DownloadResult(
                        success=False,
                        animation_name=animation_name,
                        error="Export job failed on Mixamo server",
                    )

            if not download_url:
                return DownloadResult(
                    success=False,
                    animation_name=animation_name,
                    error="Timeout waiting for download URL",
                )

            # Download the file
            file_response = await client.get(download_url, follow_redirects=True)

            if file_response.status_code != 200:
                return DownloadResult(
                    success=False,
                    animation_name=animation_name,
                    error=f"Download failed: {file_response.status_code}",
                )

            # Save file
            output_path = Path(output_dir)
            output_path.mkdir(parents=True, exist_ok=True)

            safe_name = file_name or animation_name
            safe_name = "".join(
                c if c.isalnum() or c in "._- " else "_" for c in safe_name
            )
            safe_name = safe_name.replace(" ", "_")

            if not safe_name.endswith(".fbx"):
                safe_name += ".fbx"

            file_path = output_path / safe_name
            file_path.write_bytes(file_response.content)

            return DownloadResult(
                success=True,
                animation_name=animation_name,
                file_path=str(file_path),
            )

        except Exception as e:
            return DownloadResult(
                success=False,
                animation_name=animation_name,
                error=str(e),
            )

    async def batch_download(
        self,
        keywords: list[str],
        output_dir: str,
        character_id: str = DEFAULT_CHARACTER_ID,
        character_name: str = "Character",
    ) -> BatchDownloadResult:
        """Download multiple animations."""
        results: list[DownloadResult] = []

        # Create character-specific folder
        char_dir = Path(output_dir) / character_name
        char_dir.mkdir(parents=True, exist_ok=True)

        for keyword in keywords:
            file_name = f"{character_name}_{keyword.strip().replace(' ', '_')}"
            result = await self.download(
                animation_id_or_name=keyword.strip(),
                output_dir=str(char_dir),
                character_id=character_id,
                file_name=file_name,
            )
            results.append(result)

            # Small delay between downloads
            await asyncio.sleep(1)

        successful = sum(1 for r in results if r.success)

        return BatchDownloadResult(
            total=len(results),
            successful=successful,
            failed=len(results) - successful,
            results=results,
        )

    async def upload_character(
        self,
        fbx_path: str,
    ) -> Optional[Character]:
        """Upload a character FBX for auto-rigging."""
        # This is a complex multi-step process that requires:
        # 1. Getting upload URL
        # 2. Uploading file
        # 3. Triggering auto-rig
        # 4. Waiting for rig completion
        # For now, return None with a note that this feature needs more work
        print("Warning: Character upload not yet implemented in standalone server")
        return None
