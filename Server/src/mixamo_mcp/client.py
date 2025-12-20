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
        """Get output directory, using Unity project if configured or auto-detected."""
        if custom_dir:
            return custom_dir

        # Try configured path first
        config_path = self._config.get_unity_animations_path()
        if config_path:
            return config_path

        # Auto-detect Unity project
        detected = self.detect_unity_project()
        if detected:
            return str(Path(detected) / "Assets" / self._config.animations_subfolder)

        return None

    def detect_unity_project(self) -> Optional[str]:
        """Auto-detect running or recent Unity project."""
        # Method 1: Check for running Unity (Library/EditorInstance.json exists with recent timestamp)
        running = self._find_running_unity_project()
        if running:
            return running

        # Method 2: Check Unity Hub recent projects
        recent = self._get_recent_unity_projects()
        if recent:
            return recent[0]  # Return most recent

        return None

    def _find_running_unity_project(self) -> Optional[str]:
        """Find currently running Unity project by checking lock files."""
        import glob

        # Common Unity project locations
        search_paths = [
            Path.home() / "Documents",
            Path.home() / "Projects",
            Path.home() / "Unity Projects",
            Path("D:/"),
            Path("C:/Users") / os.getenv("USERNAME", "") / "Documents",
        ]

        # Also check drives
        if os.name == "nt":
            for drive in ["C:", "D:", "E:", "F:"]:
                drive_path = Path(drive + "/")
                if drive_path.exists():
                    search_paths.append(drive_path)

        running_projects = []

        for base_path in search_paths:
            if not base_path.exists():
                continue

            # Look for Unity project indicators (max 3 levels deep)
            try:
                for depth in range(3):
                    pattern = str(
                        base_path / ("*/" * depth) / "Library" / "EditorInstance.json"
                    )
                    for editor_file in glob.glob(pattern):
                        editor_path = Path(editor_file)
                        project_path = editor_path.parent.parent

                        # Verify it's a Unity project
                        if (project_path / "Assets").exists():
                            # Check if recently modified (Unity is likely running)
                            try:
                                mtime = editor_path.stat().st_mtime
                                # If modified in last 5 minutes, Unity is probably running
                                if time.time() - mtime < 300:
                                    running_projects.append((project_path, mtime))
                            except:
                                pass
            except:
                continue

        # Return most recently active project
        if running_projects:
            running_projects.sort(key=lambda x: x[1], reverse=True)
            return str(running_projects[0][0])

        return None

    def _get_recent_unity_projects(self) -> list[str]:
        """Get recent Unity projects from Unity Hub preferences."""
        recent_projects = []

        if os.name == "nt":
            # Windows: Check Unity Hub preferences
            hub_prefs = (
                Path.home() / "AppData" / "Roaming" / "UnityHub" / "projectDir.json"
            )
            if hub_prefs.exists():
                try:
                    data = json.loads(hub_prefs.read_text())
                    if isinstance(data, dict) and "directoryPath" in data:
                        recent_projects.append(data["directoryPath"])
                except:
                    pass

            # Also check Unity Editor preferences
            prefs_path = (
                Path.home()
                / "AppData"
                / "Roaming"
                / "Unity"
                / "Editor-5.x"
                / "Preferences"
            )
            if prefs_path.exists():
                try:
                    for pref_file in prefs_path.glob("RecentlyUsedProjectPaths-*"):
                        content = pref_file.read_text(errors="ignore")
                        # Parse the binary-ish format
                        for line in content.split("\x00"):
                            line = line.strip()
                            if (
                                line
                                and Path(line).exists()
                                and (Path(line) / "Assets").exists()
                            ):
                                if line not in recent_projects:
                                    recent_projects.append(line)
                except:
                    pass

        # Filter to only valid Unity projects
        valid_projects = []
        for proj in recent_projects:
            proj_path = Path(proj)
            if proj_path.exists() and (proj_path / "Assets").exists():
                valid_projects.append(str(proj_path))

        return valid_projects

    def get_detected_project_info(self) -> dict:
        """Get info about detected Unity project for status display."""
        configured = self._config.unity_project_path
        detected = self.detect_unity_project()

        return {
            "configured": configured or None,
            "detected": detected,
            "active": configured or detected,
            "output_path": self.get_output_dir(),
        }

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
        # Check authentication first
        if not self._access_token:
            return SearchResult(
                query=query,
                total=0,
                animations=[],
                error="Not authenticated. Please run mixamo-auth first with your access token.",
            )

        client = await self._get_client()

        # Get search queries for the keyword
        queries = get_search_queries(query)
        all_animations: list[Animation] = []
        seen_ids: set[str] = set()
        any_success = False
        last_error = None

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

                if response.status_code == 401:
                    return SearchResult(
                        query=query,
                        total=0,
                        animations=[],
                        error="Authentication failed. Token may be expired. Please run mixamo-auth with a new token.",
                    )

                if response.status_code != 200:
                    last_error = f"API returned status {response.status_code}"
                    continue

                any_success = True
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
                last_error = str(e)
                print(f"Search error for '{search_query}': {e}")
                continue

        # If no successful responses and no results, report error
        if not any_success and not all_animations:
            return SearchResult(
                query=query,
                total=0,
                animations=[],
                error=last_error
                or "All search queries failed. Check your network connection.",
            )

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

    async def _get_animation_details(
        self, animation_id: str, character_id: str
    ) -> dict:
        """Get detailed animation info including gms_hash."""
        client = await self._get_client()
        response = await client.get(
            f"{self.BASE_URL}/products/{animation_id}",
            headers=self._get_headers(),
            params={"similar": 0, "character_id": character_id},
        )
        if response.status_code != 200:
            raise Exception(f"Failed to get animation details: {response.status_code}")
        return response.json()

    def _build_gms_hash(self, animation_details: dict) -> list[dict]:
        """Build gms_hash payload from animation details."""
        gms_hash = animation_details.get("details", {}).get("gms_hash", {})

        # Convert params array to comma-separated string
        params = gms_hash.get("params", [])
        if isinstance(params, list):
            param_values = [
                str(p[1]) if isinstance(p, list) else str(p) for p in params
            ]
            params_string = ",".join(param_values) if param_values else "0"
        else:
            params_string = str(params)

        # Process trim values
        trim = gms_hash.get("trim", [0, 100])
        if isinstance(trim, list) and len(trim) >= 2:
            trim = [int(trim[0]), int(trim[1])]
        else:
            trim = [0, 100]

        return [
            {
                "model-id": gms_hash.get("model-id", 0),
                "mirror": gms_hash.get("mirror", False),
                "trim": trim,
                "overdrive": 0,
                "params": params_string,
                "arm-space": gms_hash.get("arm-space", 0),
                "inplace": gms_hash.get("inplace", False),
            }
        ]

    async def download(
        self,
        animation_id_or_name: str,
        output_dir: str,
        character_id: str = DEFAULT_CHARACTER_ID,
        file_name: Optional[str] = None,
    ) -> DownloadResult:
        """Download a single animation."""
        # Check authentication first
        if not self._access_token:
            return DownloadResult(
                success=False,
                animation_name=animation_id_or_name,
                error="Not authenticated. Please run mixamo-auth first with your access token.",
            )

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
            # Get animation details to build proper gms_hash
            animation_details = await self._get_animation_details(
                animation_id, character_id
            )
            product_name = animation_details.get("description", animation_name)
            gms_hash = self._build_gms_hash(animation_details)

            # Request export with proper gms_hash
            # Note: Do NOT include "type" field - it causes 400 error
            export_response = await client.post(
                f"{self.BASE_URL}/animations/export",
                headers=self._get_headers(),
                json={
                    "character_id": character_id,
                    "product_name": product_name,
                    "preferences": {
                        "format": "fbx7",
                        "skin": "false",
                        "fps": "30",
                        "reducekf": "0",
                    },
                    "gms_hash": gms_hash,
                },
            )

            if export_response.status_code not in (200, 202):
                error_detail = ""
                try:
                    error_data = export_response.json()
                    error_detail = f" - {error_data.get('message', error_data)}"
                except:
                    pass
                return DownloadResult(
                    success=False,
                    animation_name=animation_name,
                    error=f"Export request failed: {export_response.status_code}{error_detail}",
                )

            # Poll for download URL using character monitor endpoint
            download_url = None
            for attempt in range(30):  # Max 30 attempts (60 seconds)
                await asyncio.sleep(2)

                monitor_response = await client.get(
                    f"{self.BASE_URL}/characters/{character_id}/monitor",
                    headers=self._get_headers(),
                )

                if monitor_response.status_code != 200:
                    continue

                monitor_data = monitor_response.json()
                status = monitor_data.get("status", "")

                if status == "completed":
                    download_url = monitor_data.get("job_result", "")
                    break
                elif status == "failed":
                    error_msg = monitor_data.get(
                        "message", "Export job failed on Mixamo server"
                    )
                    return DownloadResult(
                        success=False,
                        animation_name=animation_name,
                        error=error_msg,
                    )

            if not download_url:
                return DownloadResult(
                    success=False,
                    animation_name=animation_name,
                    error="Timeout waiting for download URL (60s)",
                )

            # Download the file with streaming to handle large files
            async with client.stream(
                "GET", download_url, follow_redirects=True
            ) as response:
                if response.status_code != 200:
                    return DownloadResult(
                        success=False,
                        animation_name=animation_name,
                        error=f"Download failed: {response.status_code}",
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

                # Stream to file to avoid memory issues with large files
                with open(file_path, "wb") as f:
                    async for chunk in response.aiter_bytes(chunk_size=8192):
                        f.write(chunk)

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
