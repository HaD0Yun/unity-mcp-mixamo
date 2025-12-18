# Mixamo Helper for Unity

Lightweight Unity Editor utilities for working with Mixamo animations. **No external dependencies** - works standalone without any MCP packages.

## Features

### Auto Humanoid Rig Setup

Automatically configures imported FBX files with Humanoid rig when placed in folders containing "Mixamo", "Animations", or "Animation" in the path.

### Animator Controller Builder

Create Animator Controllers from a folder of animation clips with:
- Automatic state creation for each clip
- Basic locomotion transitions (Idle ↔ Walk ↔ Run)
- Trigger-based actions (Jump, Attack)
- Common parameters pre-configured

## Installation

### Unity Package Manager (Git URL)

```
https://github.com/HaD0Yun/unity-mcp-mixamo.git?path=unity-helper
```

### Manual Installation

Copy the `unity-helper` folder to your project's `Assets/` directory.

## Usage

### Automatic FBX Import

1. Create a folder like `Assets/Animations/Player/`
2. Drop Mixamo FBX files into it
3. Files are automatically configured with Humanoid rig

### Create Animator Controller

1. Select a folder containing animation FBX files
2. Go to **Tools > Mixamo Helper > Create Animator from Selected Folder**
3. An Animator Controller is created with all animations as states

### Scripting API

```csharp
using MixamoHelper;

// Create Animator Controller from code
string controllerPath = AnimatorBuilder.CreateFromFolder(
    "Assets/Animations/Player",
    defaultStateName: "Idle"
);
```

## How It Works

### MixamoPostprocessor

- Intercepts FBX imports in designated folders
- Sets `ModelImporter.animationType` to `Human`
- Disables unnecessary imports (materials, cameras, etc.)
- Auto-detects looping animations based on name patterns

### AnimatorBuilder

- Scans folder for FBX files
- Extracts AnimationClip from each
- Creates AnimatorController with states
- Adds basic transitions and parameters

## Folder Detection

The postprocessor activates for FBX files in paths containing:
- `Mixamo`
- `Animations`
- `Animation`

Examples:
- `Assets/Animations/Player/Walk.fbx` ✓
- `Assets/Characters/Mixamo/Idle.fbx` ✓
- `Assets/Models/Character.fbx` ✗

## Loop Detection

Animations are automatically set to loop based on name patterns:

**Looping**: idle, walk, run, jog, sprint, crouch, crawl, swim, fly, strafe, dance

**Non-looping**: jump, attack, hit, death, shoot, reload, throw, dodge, roll, land

## License

Apache-2.0
