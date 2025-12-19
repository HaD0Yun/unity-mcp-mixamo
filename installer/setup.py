"""Mixamo MCP Installer - One-click setup for all MCP clients."""

import os
import sys
import json
import shutil
import subprocess
import ctypes
from pathlib import Path
from typing import Optional
import tkinter as tk
from tkinter import ttk, messagebox
import webbrowser


class MixamoInstaller:
    """Installer for Mixamo MCP."""

    # Installation paths
    INSTALL_DIR = Path(os.environ.get("LOCALAPPDATA", "")) / "MixamoMCP"
    EXE_NAME = "mixamo-mcp.exe"

    # MCP client config locations
    MCP_CLIENTS = {
        "Claude Desktop": {
            "windows": Path(os.environ.get("APPDATA", ""))
            / "Claude"
            / "claude_desktop_config.json",
            "mac": Path.home()
            / "Library"
            / "Application Support"
            / "Claude"
            / "claude_desktop_config.json",
        },
        "Cursor": {
            "windows": Path(os.environ.get("APPDATA", ""))
            / "Cursor"
            / "User"
            / "globalStorage"
            / "cursor.mcp"
            / "config.json",
        },
        "Windsurf": {
            "windows": Path.home() / ".codeium" / "windsurf" / "mcp_config.json",
        },
    }

    def __init__(self):
        self.exe_path = self.INSTALL_DIR / self.EXE_NAME
        self.token_file = Path.home() / ".mixamo_mcp_token"

    def get_bundled_exe(self) -> Optional[Path]:
        """Get path to bundled exe (when running as installer)."""
        if getattr(sys, "frozen", False):
            # Running as compiled exe
            bundle_dir = Path(sys._MEIPASS)
            bundled = bundle_dir / self.EXE_NAME
            if bundled.exists():
                return bundled

        # Development: look for exe in dist folder
        dev_exe = Path(__file__).parent.parent / "server" / "dist" / self.EXE_NAME
        if dev_exe.exists():
            return dev_exe

        return None

    def install_exe(self) -> bool:
        """Install mixamo-mcp.exe to local app data."""
        source = self.get_bundled_exe()
        if not source:
            return False

        try:
            self.INSTALL_DIR.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, self.exe_path)
            return True
        except Exception as e:
            print(f"Install error: {e}")
            return False

    def configure_mcp_client(self, client_name: str) -> bool:
        """Configure an MCP client to use Mixamo MCP."""
        if client_name not in self.MCP_CLIENTS:
            return False

        client_info = self.MCP_CLIENTS[client_name]
        config_path = client_info.get("windows" if os.name == "nt" else "mac")

        if not config_path:
            return False

        try:
            # Read existing config or create new
            config = {}
            if config_path.exists():
                try:
                    config = json.loads(config_path.read_text(encoding="utf-8"))
                except:
                    config = {}

            # Ensure mcpServers exists
            if "mcpServers" not in config:
                config["mcpServers"] = {}

            # Add mixamo server
            exe_path_str = str(self.exe_path).replace("\\", "\\\\")
            config["mcpServers"]["mixamo"] = {"command": str(self.exe_path)}

            # Create parent directory if needed
            config_path.parent.mkdir(parents=True, exist_ok=True)

            # Write config
            config_path.write_text(json.dumps(config, indent=2), encoding="utf-8")
            return True

        except Exception as e:
            print(f"Config error for {client_name}: {e}")
            return False

    def save_token(self, token: str) -> bool:
        """Save Mixamo token."""
        try:
            self.token_file.write_text(token.strip())
            return True
        except:
            return False

    def get_token(self) -> Optional[str]:
        """Get saved token."""
        if self.token_file.exists():
            try:
                return self.token_file.read_text().strip()
            except:
                pass
        return None

    def is_installed(self) -> bool:
        """Check if already installed."""
        return self.exe_path.exists()

    def get_available_clients(self) -> list[tuple[str, bool]]:
        """Get list of MCP clients and their install status."""
        results = []
        for name, paths in self.MCP_CLIENTS.items():
            config_path = paths.get("windows" if os.name == "nt" else "mac")
            if config_path:
                # Check if client seems to be installed (parent dir exists)
                client_installed = config_path.parent.exists()
                results.append((name, client_installed))
        return results


class InstallerGUI:
    """Simple GUI for the installer."""

    def __init__(self):
        self.installer = MixamoInstaller()

        # Enable DPI awareness on Windows
        try:
            ctypes.windll.shcore.SetProcessDpiAwareness(1)
        except:
            pass

        self.root = tk.Tk()
        self.root.title("Mixamo MCP Installer")

        # Set minimum size and initial size
        self.root.minsize(500, 500)

        # Center window
        self.root.update_idletasks()
        x = (self.root.winfo_screenwidth() - 500) // 2
        y = (self.root.winfo_screenheight() - 500) // 2
        self.root.geometry(f"500x500+{x}+{y}")

        self.setup_ui()

    def setup_ui(self):
        """Setup the UI."""
        # Install button at BOTTOM first (pack order matters)
        self.install_btn = ttk.Button(
            self.root, text="Install", command=self.do_install
        )
        self.install_btn.pack(side="bottom", pady=20)

        # Title
        title = tk.Label(
            self.root, text="Mixamo MCP Installer", font=("Arial", 16, "bold")
        )
        title.pack(pady=15)

        # Status
        self.status_label = tk.Label(self.root, text="", font=("Arial", 10))
        self.status_label.pack(pady=5)

        # Frame for MCP clients
        clients_frame = ttk.LabelFrame(self.root, text="MCP Clients", padding=10)
        clients_frame.pack(padx=20, pady=10, fill="x")

        self.client_vars = {}
        for name, installed in self.installer.get_available_clients():
            var = tk.BooleanVar(value=installed)
            self.client_vars[name] = var
            cb = ttk.Checkbutton(
                clients_frame,
                text=name,
                variable=var,
                state="normal" if installed else "disabled",
            )
            cb.pack(anchor="w")
            if not installed:
                tk.Label(
                    clients_frame,
                    text="  (not installed)",
                    font=("Arial", 8),
                    fg="gray",
                ).pack(anchor="w")

        # Token frame
        token_frame = ttk.LabelFrame(self.root, text="Mixamo Token", padding=10)
        token_frame.pack(padx=20, pady=10, fill="x")

        tk.Label(token_frame, text="Paste your Mixamo token:").pack(anchor="w")
        self.token_entry = ttk.Entry(token_frame, width=50, show="*")
        self.token_entry.pack(fill="x", pady=5)

        # Pre-fill if token exists
        existing_token = self.installer.get_token()
        if existing_token:
            self.token_entry.insert(0, existing_token)

        # Help button
        help_btn = ttk.Button(
            token_frame, text="How to get token?", command=self.show_token_help
        )
        help_btn.pack(anchor="w")

        # Update status
        if self.installer.is_installed():
            self.status_label.config(text="Status: Already installed", fg="green")
            self.install_btn.config(text="Reinstall / Update")

    def show_token_help(self):
        """Show help for getting token."""
        help_text = """How to get your Mixamo token:

1. Go to mixamo.com and log in
2. Press F12 to open Developer Tools
3. Go to Console tab
4. Type: copy(localStorage.access_token)
5. Press Enter
6. Token is now copied to clipboard!
7. Paste it here

Open Mixamo now?"""

        if messagebox.askyesno("Get Token", help_text):
            webbrowser.open("https://www.mixamo.com")

    def do_install(self):
        """Perform installation."""
        self.install_btn.config(state="disabled")
        self.status_label.config(text="Installing...", fg="blue")
        self.root.update()

        # Install exe
        if not self.installer.install_exe():
            messagebox.showerror("Error", "Failed to install mixamo-mcp.exe")
            self.install_btn.config(state="normal")
            return

        # Configure selected clients
        configured = []
        for name, var in self.client_vars.items():
            if var.get():
                if self.installer.configure_mcp_client(name):
                    configured.append(name)

        # Save token
        token = self.token_entry.get().strip()
        if token:
            self.installer.save_token(token)

        # Show result
        self.status_label.config(text="Status: Installed!", fg="green")
        self.install_btn.config(state="normal", text="Reinstall / Update")

        msg = f"Installation complete!\n\nConfigured: {', '.join(configured) if configured else 'None'}"
        if not token:
            msg += '\n\nNote: No token entered. You\'ll need to set it later with:\nmixamo-auth accessToken="your_token"'
        else:
            msg += "\n\nToken saved!"

        msg += "\n\nPlease restart your MCP clients (Claude, Cursor, etc.)"

        messagebox.showinfo("Success", msg)

    def run(self):
        """Run the installer."""
        self.root.mainloop()


def main():
    """Main entry point."""
    # Check for admin rights on Windows (optional, might not be needed for LOCALAPPDATA)
    if os.name == "nt":
        try:
            # Try to create install dir to check permissions
            install_dir = Path(os.environ.get("LOCALAPPDATA", "")) / "MixamoMCP"
            install_dir.mkdir(parents=True, exist_ok=True)
        except PermissionError:
            messagebox.showerror("Error", "Please run as administrator")
            sys.exit(1)

    app = InstallerGUI()
    app.run()


if __name__ == "__main__":
    main()
