"""
Mixamo Animation Service

Handles all interactions with the Mixamo API for searching, downloading,
and managing animations for humanoid characters in Unity.

API Reference (Reverse-Engineered):
- Base URL: https://www.mixamo.com/api/v1/
- Authentication: Bearer token from Adobe ID session
- Headers: X-Api-Key: mixamo2
"""

import asyncio
import httpx
import json
import os
import time
from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from typing import Any, Optional


class MixamoError(Exception):
    """Base exception for Mixamo-related errors."""

    pass


class AuthenticationError(MixamoError):
    """Raised when authentication fails or token is invalid."""

    pass


class AnimationNotFoundError(MixamoError):
    """Raised when no animations match the search query."""

    pass


class DownloadError(MixamoError):
    """Raised when animation download fails."""

    pass


class CharacterError(MixamoError):
    """Raised when character operations fail."""

    pass


class ExportFormat(str, Enum):
    """Available export formats for animations."""

    FBX_2019 = "fbx7_2019"
    FBX_7 = "fbx7"
    DAE = "dae_mixamo"  # Collada format


@dataclass
class AnimationResult:
    """Represents a single animation search result."""

    id: str
    name: str
    description: str
    type: str  # "Motion" or "MotionPack"
    thumbnail_url: Optional[str] = None
    duration: Optional[float] = None

    @classmethod
    def from_api_response(cls, data: dict) -> "AnimationResult":
        return cls(
            id=data.get("id", ""),
            name=data.get("name", ""),
            description=data.get("description", ""),
            type=data.get("type", "Motion"),
            thumbnail_url=data.get("thumbnail_url"),
            duration=data.get("duration"),
        )


@dataclass
class Character:
    """Represents a Mixamo character."""

    id: str
    name: str

    @classmethod
    def from_api_response(cls, data: dict) -> "Character":
        return cls(
            id=data.get("primary_character_id", ""),
            name=data.get("primary_character_name", "Unknown"),
        )


@dataclass
class DownloadJob:
    """Tracks the status of an animation download job."""

    character_id: str
    animation_name: str
    status: str = "pending"  # pending, processing, completed, failed
    download_url: Optional[str] = None
    error_message: Optional[str] = None


@dataclass
class MixamoConfig:
    """Configuration for Mixamo service."""

    access_token: str
    output_dir: str = "./downloads"
    export_format: ExportFormat = ExportFormat.FBX_2019
    fps: int = 30
    include_skin: bool = False
    reduce_keyframes: int = 0


# Animation keyword mapping for natural language queries
ANIMATION_KEYWORD_MAP = {
    # Locomotion
    "run": ["running", "run", "jog", "sprint"],
    "walk": ["walking", "walk", "stroll"],
    "jump": ["jump", "jumping", "leap", "hop"],
    "crouch": ["crouch", "crouching", "sneaking", "sneak", "stealth"],
    "crawl": ["crawl", "crawling", "prone"],
    # Idle & Stance
    "idle": ["idle", "breathing", "standing", "wait"],
    "sit": ["sitting", "sit", "seated"],
    "lay": ["laying", "lay", "lying", "prone"],
    # Combat
    "attack": ["punch", "kick", "slash", "strike", "attack", "hit"],
    "punch": ["punch", "jab", "hook", "uppercut"],
    "kick": ["kick", "roundhouse", "front kick"],
    "sword": ["sword", "slash", "sword attack", "melee"],
    "shoot": ["shoot", "shooting", "aim", "fire", "gun"],
    "block": ["block", "blocking", "defend", "guard"],
    "dodge": ["dodge", "evade", "roll", "sidestep"],
    # Interactions
    "pick": ["pick up", "pickup", "grab", "take"],
    "throw": ["throw", "throwing", "toss"],
    "push": ["push", "pushing", "shove"],
    "pull": ["pull", "pulling", "drag"],
    # Expressions & Reactions
    "wave": ["wave", "waving", "greeting", "hello"],
    "dance": ["dance", "dancing", "groove"],
    "die": ["death", "die", "dying", "fall", "collapse"],
    "hurt": ["hurt", "pain", "hit reaction", "damage"],
    "celebrate": ["celebrate", "victory", "cheer", "triumph"],
    "taunt": ["taunt", "mock", "provoke"],
    # Climbing & Traversal
    "climb": ["climb", "climbing", "scale", "ascend"],
    "hang": ["hang", "hanging", "ledge"],
    "swim": ["swim", "swimming", "float"],
    "fly": ["fly", "flying", "hover", "float"],
}


def expand_search_keywords(query: str) -> list[str]:
    """
    Expand a user's search query into multiple Mixamo-compatible search terms.

    Args:
        query: User's natural language animation request

    Returns:
        List of expanded search keywords
    """
    query_lower = query.lower().strip()
    expanded = [query_lower]  # Always include original query

    # Check for keyword matches
    for key, variations in ANIMATION_KEYWORD_MAP.items():
        if key in query_lower or query_lower in key:
            expanded.extend(variations)
        # Also check if any variation matches
        for var in variations:
            if var in query_lower or query_lower in var:
                expanded.extend(variations)
                break

    # Remove duplicates while preserving order
    seen = set()
    return [x for x in expanded if not (x in seen or seen.add(x))]


class MixamoService:
    """
    Service for interacting with the Mixamo API.

    Provides methods for:
    - Searching animations by keyword
    - Downloading animations for a character
    - Uploading custom characters for auto-rigging
    - Managing animation export settings
    """

    BASE_URL = "https://www.mixamo.com/api/v1"
    API_KEY = "mixamo2"

    def __init__(self, config: MixamoConfig):
        """
        Initialize the Mixamo service.

        Args:
            config: MixamoConfig with authentication and settings
        """
        self.config = config
        self._client: Optional[httpx.AsyncClient] = None

    @property
    def headers(self) -> dict[str, str]:
        """Get the standard headers for API requests."""
        return {
            "Accept": "application/json",
            "Accept-Encoding": "gzip, deflate, br",
            "Content-Type": "application/json",
            "Authorization": f"Bearer {self.config.access_token}",
            "X-Api-Key": self.API_KEY,
            "X-Requested-With": "XMLHttpRequest",
        }

    async def _get_client(self) -> httpx.AsyncClient:
        """Get or create the HTTP client."""
        if self._client is None or self._client.is_closed:
            self._client = httpx.AsyncClient(
                timeout=httpx.Timeout(60.0, connect=10.0), follow_redirects=True
            )
        return self._client

    async def close(self):
        """Close the HTTP client."""
        if self._client and not self._client.is_closed:
            await self._client.aclose()

    async def _request(self, method: str, endpoint: str, **kwargs) -> dict[str, Any]:
        """
        Make an authenticated request to the Mixamo API.

        Args:
            method: HTTP method (GET, POST, etc.)
            endpoint: API endpoint (relative to BASE_URL)
            **kwargs: Additional arguments for httpx

        Returns:
            Parsed JSON response

        Raises:
            AuthenticationError: If authentication fails
            MixamoError: For other API errors
        """
        client = await self._get_client()
        url = f"{self.BASE_URL}/{endpoint.lstrip('/')}"

        # Merge headers
        headers = self.headers.copy()
        if "headers" in kwargs:
            headers.update(kwargs.pop("headers"))

        try:
            response = await client.request(method, url, headers=headers, **kwargs)

            if response.status_code == 401:
                raise AuthenticationError(
                    "Authentication failed. Please provide a valid access token."
                )
            elif response.status_code == 404:
                raise MixamoError(f"Resource not found: {endpoint}")
            elif response.status_code == 429:
                raise MixamoError("Rate limit exceeded. Please wait before retrying.")
            elif response.status_code >= 400:
                raise MixamoError(f"API error {response.status_code}: {response.text}")

            return response.json()

        except httpx.RequestError as e:
            raise MixamoError(f"Network error: {str(e)}")

    async def get_primary_character(self) -> Character:
        """
        Get the currently selected primary character.

        Returns:
            Character object with ID and name

        Raises:
            CharacterError: If no character is selected
        """
        try:
            data = await self._request("GET", "/characters/primary")
            if not data.get("primary_character_id"):
                raise CharacterError("No primary character selected in Mixamo.")
            return Character.from_api_response(data)
        except MixamoError as e:
            raise CharacterError(f"Failed to get primary character: {str(e)}")

    async def search_animations(
        self, query: str, limit: int = 24, page: int = 1, expand_keywords: bool = True
    ) -> list[AnimationResult]:
        """
        Search for animations by keyword.

        Args:
            query: Search query string
            limit: Maximum results per page (default 24, max 96)
            page: Page number for pagination
            expand_keywords: Whether to expand keywords using mapping

        Returns:
            List of AnimationResult objects
        """
        results = []

        # Expand search terms if enabled
        search_terms = expand_search_keywords(query) if expand_keywords else [query]

        for term in search_terms[:3]:  # Limit to first 3 expansions
            params = {
                "limit": min(limit, 96),
                "page": page,
                "type": "Motion",  # Exclude MotionPacks for simplicity
                "query": term,
            }

            try:
                data = await self._request("GET", "/products", params=params)

                for item in data.get("results", []):
                    # Avoid duplicates
                    if not any(r.id == item["id"] for r in results):
                        results.append(AnimationResult.from_api_response(item))

            except MixamoError:
                continue  # Try next search term

        if not results:
            raise AnimationNotFoundError(f"No animations found for query: {query}")

        return results

    async def get_animation_details(
        self, animation_id: str, character_id: str
    ) -> dict[str, Any]:
        """
        Get detailed information about an animation for a specific character.

        Args:
            animation_id: The animation product ID
            character_id: The character ID to apply the animation to

        Returns:
            Animation details including gms_hash for export
        """
        endpoint = f"/products/{animation_id}"
        params = {"similar": 0, "character_id": character_id}

        return await self._request("GET", endpoint, params=params)

    async def _build_export_payload(
        self, character_id: str, animation_details: dict[str, Any], product_name: str
    ) -> dict[str, Any]:
        """Build the payload for animation export."""

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

        processed_gms_hash = {
            "model-id": gms_hash.get("model-id", 0),
            "mirror": gms_hash.get("mirror", False),
            "trim": trim,
            "overdrive": 0,
            "params": params_string,
            "arm-space": gms_hash.get("arm-space", 0),
            "inplace": gms_hash.get("inplace", False),
        }

        return {
            "character_id": character_id,
            "product_name": product_name,
            "type": animation_details.get("type", "Motion"),
            "preferences": {
                "format": self.config.export_format.value,
                "skin": str(self.config.include_skin).lower(),
                "fps": str(self.config.fps),
                "reducekf": str(self.config.reduce_keyframes),
            },
            "gms_hash": [processed_gms_hash],
        }

    async def export_animation(
        self, character_id: str, animation_id: str, output_name: Optional[str] = None
    ) -> DownloadJob:
        """
        Export an animation for download.

        Args:
            character_id: Character UUID
            animation_id: Animation product ID
            output_name: Custom output filename (optional)

        Returns:
            DownloadJob with status and download URL when complete
        """
        # Get animation details
        details = await self.get_animation_details(animation_id, character_id)
        product_name = output_name or details.get(
            "description", f"animation_{animation_id}"
        )

        # Build and send export request
        payload = await self._build_export_payload(character_id, details, product_name)

        job = DownloadJob(
            character_id=character_id, animation_name=product_name, status="processing"
        )

        try:
            await self._request("POST", "/animations/export", json=payload)

            # Poll for completion
            job = await self._monitor_export(character_id, job)

        except MixamoError as e:
            job.status = "failed"
            job.error_message = str(e)

        return job

    async def _monitor_export(
        self,
        character_id: str,
        job: DownloadJob,
        max_attempts: int = 60,
        poll_interval: float = 1.0,
    ) -> DownloadJob:
        """
        Monitor an export job until completion.

        Args:
            character_id: Character UUID
            job: DownloadJob to update
            max_attempts: Maximum polling attempts
            poll_interval: Seconds between polls

        Returns:
            Updated DownloadJob
        """
        endpoint = f"/characters/{character_id}/monitor"

        for _ in range(max_attempts):
            try:
                data = await self._request("GET", endpoint)
                status = data.get("status", "")

                if status == "completed":
                    job.status = "completed"
                    job.download_url = data.get("job_result")
                    return job

                elif status == "failed":
                    job.status = "failed"
                    job.error_message = data.get("message", "Export failed")
                    return job

                elif status == "processing":
                    await asyncio.sleep(poll_interval)

                else:
                    await asyncio.sleep(poll_interval)

            except MixamoError as e:
                job.status = "failed"
                job.error_message = str(e)
                return job

        job.status = "failed"
        job.error_message = "Export timed out"
        return job

    async def download_animation(
        self, job: DownloadJob, output_dir: Optional[str] = None
    ) -> str:
        """
        Download a completed animation to disk.

        Args:
            job: Completed DownloadJob with download URL
            output_dir: Directory to save the file (uses config default if None)

        Returns:
            Path to the downloaded file

        Raises:
            DownloadError: If download fails
        """
        if job.status != "completed" or not job.download_url:
            raise DownloadError(
                f"Cannot download: job status is {job.status}, "
                f"error: {job.error_message}"
            )

        output_path = Path(output_dir or self.config.output_dir)
        output_path.mkdir(parents=True, exist_ok=True)

        # Sanitize filename
        safe_name = "".join(
            c if c.isalnum() or c in "._- " else "_" for c in job.animation_name
        )
        file_path = output_path / f"{safe_name}.fbx"

        client = await self._get_client()

        try:
            response = await client.get(job.download_url)
            response.raise_for_status()

            file_path.write_bytes(response.content)
            return str(file_path)

        except httpx.RequestError as e:
            raise DownloadError(f"Failed to download animation: {str(e)}")

    async def upload_character(
        self, file_path: str, character_name: Optional[str] = None
    ) -> Character:
        """
        Upload a custom character FBX for auto-rigging.

        Args:
            file_path: Path to the FBX or OBJ file
            character_name: Optional name for the character

        Returns:
            Character object for the uploaded character

        Raises:
            CharacterError: If upload fails
        """
        path = Path(file_path)
        if not path.exists():
            raise CharacterError(f"File not found: {file_path}")

        name = character_name or path.stem

        client = await self._get_client()

        # Build multipart form
        files = {"file": (f"{name}.fbx", path.read_bytes(), "application/octet-stream")}

        headers = {
            "Accept": "application/json, text/javascript, */*",
            "Authorization": f"Bearer {self.config.access_token}",
            "X-Api-Key": self.API_KEY,
            "X-Requested-With": "XMLHttpRequest",
        }

        try:
            response = await client.post(
                f"{self.BASE_URL}/characters", files=files, headers=headers
            )

            if response.status_code >= 400:
                raise CharacterError(
                    f"Upload failed with status {response.status_code}: {response.text}"
                )

            data = response.json()
            character_uuid = data.get("uuid")

            if not character_uuid:
                raise CharacterError("No character UUID returned from upload")

            # Wait for auto-rigging to complete
            job = DownloadJob(
                character_id=character_uuid, animation_name=name, status="processing"
            )

            result = await self._monitor_export(character_uuid, job, max_attempts=120)

            if result.status != "completed":
                raise CharacterError(
                    f"Character rigging failed: {result.error_message}"
                )

            return Character(id=character_uuid, name=name)

        except httpx.RequestError as e:
            raise CharacterError(f"Upload failed: {str(e)}")

    async def fetch_animations_for_character(
        self, character_id: str, animation_queries: list[str], output_dir: str
    ) -> list[dict[str, Any]]:
        """
        Fetch multiple animations for a character.

        This is the main entry point for bulk animation downloads.

        Args:
            character_id: Character UUID
            animation_queries: List of animation keywords to search for
            output_dir: Directory to save downloaded animations

        Returns:
            List of results with status for each animation
        """
        results = []

        for query in animation_queries:
            result = {
                "query": query,
                "status": "pending",
                "animation_name": None,
                "file_path": None,
                "error": None,
            }

            try:
                # Search for the animation
                animations = await self.search_animations(query, limit=1)
                if not animations:
                    result["status"] = "not_found"
                    result["error"] = f"No animations found for '{query}'"
                    results.append(result)
                    continue

                anim = animations[0]
                result["animation_name"] = anim.description

                # Export the animation
                job = await self.export_animation(
                    character_id, anim.id, output_name=anim.description
                )

                if job.status != "completed":
                    result["status"] = "export_failed"
                    result["error"] = job.error_message
                    results.append(result)
                    continue

                # Download the animation
                file_path = await self.download_animation(job, output_dir)
                result["status"] = "completed"
                result["file_path"] = file_path

            except MixamoError as e:
                result["status"] = "error"
                result["error"] = str(e)

            results.append(result)

            # Small delay to avoid rate limiting
            await asyncio.sleep(0.5)

        return results
