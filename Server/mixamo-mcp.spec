# -*- mode: python ; coding: utf-8 -*-


a = Analysis(
    ['D:\\Mcp_Project\\unity-mcp-mixamo\\server\\src\\mixamo_mcp\\server.py'],
    pathex=[],
    binaries=[],
    datas=[],
    hiddenimports=['mcp', 'mcp.server', 'mcp.server.stdio', 'mcp.types', 'httpx', 'pydantic', 'anyio', 'anyio._backends', 'anyio._backends._asyncio'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='mixamo-mcp',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
