"""Pytest configuration and fixtures for Mixamo tests."""

import os
import pytest
import sys

# Add src to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "src"))


def pytest_addoption(parser):
    """Add custom command line options."""
    parser.addoption(
        "--token",
        action="store",
        default=None,
        help="Mixamo access token for API tests",
    )
    parser.addoption(
        "--run-api",
        action="store_true",
        default=False,
        help="Run API integration tests (requires token)",
    )


@pytest.fixture
def mixamo_token(request):
    """Get Mixamo token from command line or environment."""
    token = request.config.getoption("--token")
    if not token:
        token = os.environ.get("MIXAMO_ACCESS_TOKEN")
    return token


@pytest.fixture
def require_token(mixamo_token):
    """Skip test if no token available."""
    if not mixamo_token:
        pytest.skip("Mixamo token required (use --token or MIXAMO_ACCESS_TOKEN)")
    return mixamo_token


@pytest.fixture
def temp_output_dir(tmp_path):
    """Create a temporary output directory."""
    output_dir = tmp_path / "mixamo_downloads"
    output_dir.mkdir()
    return str(output_dir)
