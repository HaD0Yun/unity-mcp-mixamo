"""Build script for creating the installer executable."""

import subprocess
import sys
import shutil
from pathlib import Path


def build():
    """Build the installer executable."""

    installer_dir = Path(__file__).parent
    server_dir = installer_dir.parent / "server"
    dist_dir = installer_dir / "dist"

    # Check if mixamo-mcp.exe exists
    mcp_exe = server_dir / "dist" / "mixamo-mcp.exe"
    if not mcp_exe.exists():
        print(f"Error: {mcp_exe} not found!")
        print("Please build the server first: cd server && python build.py")
        return 1

    # Clean previous builds
    for folder in ["build", "dist"]:
        path = installer_dir / folder
        if path.exists():
            shutil.rmtree(path)

    # PyInstaller command
    cmd = [
        sys.executable,
        "-m",
        "PyInstaller",
        "--onefile",
        "--windowed",  # No console window
        "--name",
        "MixamoMCP-Setup",
        "--clean",
        "--noconfirm",
        # Add the MCP exe as data
        "--add-data",
        f"{mcp_exe};.",
        str(installer_dir / "setup.py"),
    ]

    print("Building installer...")
    print(f"Command: {' '.join(cmd)}")

    result = subprocess.run(cmd, cwd=installer_dir)

    if result.returncode == 0:
        exe_path = dist_dir / "MixamoMCP-Setup.exe"
        if exe_path.exists():
            print(f"\nBuild successful!")
            print(f"Installer: {exe_path}")
            print(f"Size: {exe_path.stat().st_size / 1024 / 1024:.1f} MB")
        else:
            print("\nBuild completed but exe not found")
    else:
        print(f"\nBuild failed with code {result.returncode}")

    return result.returncode


if __name__ == "__main__":
    sys.exit(build())
