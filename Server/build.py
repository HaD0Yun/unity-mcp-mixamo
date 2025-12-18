"""Build script for creating standalone executable."""

import subprocess
import sys
import shutil
from pathlib import Path


def build():
    """Build the executable using PyInstaller."""

    # Paths
    server_dir = Path(__file__).parent
    src_dir = server_dir / "src" / "mixamo_mcp"
    dist_dir = server_dir / "dist"

    # Clean previous builds
    for folder in ["build", "dist"]:
        path = server_dir / folder
        if path.exists():
            shutil.rmtree(path)

    # PyInstaller command
    cmd = [
        sys.executable,
        "-m",
        "PyInstaller",
        "--onefile",
        "--name",
        "mixamo-mcp",
        "--clean",
        "--noconfirm",
        # Hidden imports that PyInstaller might miss
        "--hidden-import",
        "mcp",
        "--hidden-import",
        "mcp.server",
        "--hidden-import",
        "mcp.server.stdio",
        "--hidden-import",
        "mcp.types",
        "--hidden-import",
        "httpx",
        "--hidden-import",
        "pydantic",
        "--hidden-import",
        "anyio",
        "--hidden-import",
        "anyio._backends",
        "--hidden-import",
        "anyio._backends._asyncio",
        str(src_dir / "server.py"),
    ]

    print("Building executable...")
    print(f"Command: {' '.join(cmd)}")

    result = subprocess.run(cmd, cwd=server_dir)

    if result.returncode == 0:
        exe_path = dist_dir / "mixamo-mcp.exe"
        if exe_path.exists():
            print(f"\n✅ Build successful!")
            print(f"   Executable: {exe_path}")
            print(f"   Size: {exe_path.stat().st_size / 1024 / 1024:.1f} MB")
        else:
            print("\n❌ Build completed but exe not found")
    else:
        print(f"\n❌ Build failed with code {result.returncode}")

    return result.returncode


if __name__ == "__main__":
    sys.exit(build())
