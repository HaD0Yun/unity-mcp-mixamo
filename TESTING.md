# Testing Guide - Unity MCP Mixamo Animation System

## Quick Start

### Prerequisites
1. Python 3.10+ with `uv` or `pip`
2. Unity 2021.3+ with Unity MCP installed
3. Mixamo account (free at mixamo.com)

---

## 1. Get Your Mixamo Access Token

**This is required for all API tests.**

```bash
# Step 1: Open mixamo.com in Chrome/Firefox
# Step 2: Log in with your Adobe ID
# Step 3: Open DevTools (F12) → Console tab
# Step 4: Run this command:

localStorage.access_token

# Step 5: Copy the token (long string starting with "eyJ...")
```

Save the token:
```bash
# Option A: Environment variable (recommended)
export MIXAMO_ACCESS_TOKEN="eyJhbGciOiJS..."

# Option B: Create token file
echo "eyJhbGciOiJS..." > ~/.mixamo_token
```

---

## 2. Python Unit Tests

### Setup Test Environment

```bash
cd D:\Mcp_Project\unity-mcp-mixamo\Server

# Create virtual environment
uv venv
source .venv/bin/activate  # Linux/Mac
# or: .venv\Scripts\activate  # Windows

# Install dependencies
uv pip install -e ".[dev]"
```

### Run All Tests

```bash
# Run all tests
pytest tests/ -v

# Run with coverage
pytest tests/ -v --cov=src --cov-report=html

# Run specific test file
pytest tests/test_mixamo_service.py -v
```

### Run Individual Test Categories

```bash
# Test keyword expansion (no API needed)
pytest tests/test_keywords.py -v

# Test API integration (requires token)
pytest tests/test_mixamo_api.py -v --token="your_token_here"

# Test MCP tool interface
pytest tests/test_manage_mixamo.py -v
```

---

## 3. Manual Python Testing

### Test 1: Keyword Expansion (No Token Needed)

```python
# test_keywords_manual.py
import sys
sys.path.insert(0, 'src/services')

from mixamo_service import expand_search_keywords, ANIMATION_KEYWORD_MAP

# Test keyword expansion
print("=== Keyword Expansion Test ===")
test_words = ["run", "attack", "idle", "dance", "custom_word"]

for word in test_words:
    expanded = expand_search_keywords(word)
    print(f"{word} → {expanded}")

print("\n=== All Keywords ===")
for key, values in ANIMATION_KEYWORD_MAP.items():
    print(f"  {key}: {values}")
```

Run it:
```bash
python test_keywords_manual.py
```

### Test 2: Search Animations (Requires Token)

```python
# test_search_manual.py
import asyncio
import os
import sys
sys.path.insert(0, 'src/services')

from mixamo_service import MixamoService, MixamoConfig

async def test_search():
    token = os.environ.get('MIXAMO_ACCESS_TOKEN') or input("Enter token: ")
    
    config = MixamoConfig(access_token=token)
    service = MixamoService(config)
    
    try:
        print("=== Searching for 'run' animations ===")
        results = await service.search_animations("run", limit=5)
        
        for i, anim in enumerate(results, 1):
            print(f"{i}. {anim.description} (ID: {anim.id})")
        
        print(f"\nFound {len(results)} animations")
        
    except Exception as e:
        print(f"Error: {e}")
    finally:
        await service.close()

asyncio.run(test_search())
```

Run it:
```bash
python test_search_manual.py
```

### Test 3: Download Animation (Requires Token)

```python
# test_download_manual.py
import asyncio
import os
import sys
sys.path.insert(0, 'src/services')

from mixamo_service import MixamoService, MixamoConfig

async def test_download():
    token = os.environ.get('MIXAMO_ACCESS_TOKEN') or input("Enter token: ")
    
    config = MixamoConfig(
        access_token=token,
        output_dir="./test_downloads",
        fps=30
    )
    service = MixamoService(config)
    
    try:
        # Get primary character
        print("=== Getting primary character ===")
        character = await service.get_primary_character()
        print(f"Character: {character.name} (ID: {character.id})")
        
        # Search for an animation
        print("\n=== Searching for 'idle' ===")
        results = await service.search_animations("idle", limit=1)
        if not results:
            print("No animations found")
            return
            
        anim = results[0]
        print(f"Found: {anim.description}")
        
        # Export and download
        print("\n=== Exporting animation ===")
        job = await service.export_animation(character.id, anim.id)
        print(f"Export status: {job.status}")
        
        if job.status == "completed":
            print("\n=== Downloading ===")
            file_path = await service.download_animation(job)
            print(f"Downloaded to: {file_path}")
        else:
            print(f"Export failed: {job.error_message}")
            
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()
    finally:
        await service.close()

asyncio.run(test_download())
```

Run it:
```bash
python test_download_manual.py
```

### Test 4: MCP Tool Interface

```python
# test_mcp_tool_manual.py
import asyncio
import os
import sys
sys.path.insert(0, 'src/services')

from manage_mixamo import manage_mixamo

async def test_mcp_tool():
    token = os.environ.get('MIXAMO_ACCESS_TOKEN')
    
    # Test 1: List keywords (no token needed)
    print("=== Test 1: List Keywords ===")
    result = await manage_mixamo(operation="list_keywords")
    print(f"Success: {result['success']}")
    print(f"Categories: {list(result['keyword_categories'].keys())}")
    
    if not token:
        print("\nSkipping API tests (no token)")
        return
    
    # Test 2: Search
    print("\n=== Test 2: Search ===")
    result = await manage_mixamo(
        operation="search",
        access_token=token,
        query="jump"
    )
    print(f"Success: {result['success']}")
    print(f"Found: {result.get('count', 0)} animations")
    
    # Test 3: Fetch (full workflow)
    print("\n=== Test 3: Fetch Animations ===")
    result = await manage_mixamo(
        operation="fetch",
        access_token=token,
        animations=["idle"],
        output_dir="./test_downloads"
    )
    print(f"Success: {result['success']}")
    print(f"Summary: {result.get('summary', {})}")

asyncio.run(test_mcp_tool())
```

---

## 4. Unity Editor Tests

### Test 1: Import Single FBX

1. Download a test FBX from Mixamo manually
2. In Unity, open Window → MCP for Unity
3. Use the MCP tool:

```
User: "Import the FBX at C:/path/to/animation.fbx for TestCharacter"
```

Or test directly in C#:

```csharp
// In Unity Editor, create a test script
using UnityEditor;
using MCPForUnity.Mixamo;
using System.Collections.Generic;

public class MixamoTestRunner
{
    [MenuItem("Tests/Mixamo/Test Import")]
    public static void TestImport()
    {
        var args = new Dictionary<string, object>
        {
            { "operation", "import" },
            { "fbx_paths", new[] { "C:/path/to/test.fbx" } },
            { "character_name", "TestCharacter" },
            { "target_path", "Assets/Animations/TestCharacter" },
            { "create_animator", true }
        };
        
        string result = MixamoAnimationTool.Execute(args);
        Debug.Log($"Result: {result}");
    }
    
    [MenuItem("Tests/Mixamo/Test Animator Creation")]
    public static void TestAnimatorCreation()
    {
        // First, import some FBX files manually to Assets/Animations/Test/
        var args = new Dictionary<string, object>
        {
            { "operation", "create_animator" },
            { "clip_paths", new[] { 
                "Assets/Animations/Test/Idle.fbx",
                "Assets/Animations/Test/Walk.fbx",
                "Assets/Animations/Test/Run.fbx"
            }},
            { "target_path", "Assets/Animations/Test" },
            { "character_name", "Test" }
        };
        
        string result = MixamoAnimationTool.Execute(args);
        Debug.Log($"Result: {result}");
    }
}
```

### Test 2: Full Workflow Test

```csharp
[MenuItem("Tests/Mixamo/Test Full Workflow")]
public static void TestFullWorkflow()
{
    var config = new MixamoCoordinatorService.MixamoWorkflowConfig
    {
        accessToken = "YOUR_TOKEN_HERE", // Or read from file
        animationKeywords = new[] { "idle", "walk", "run" },
        characterName = "TestCharacter",
        fps = 30,
        createAnimatorController = true,
        applyToSelection = true
    };
    
    var result = MixamoCoordinatorService.ExecuteWorkflow(config);
    
    Debug.Log($"Success: {result.success}");
    Debug.Log($"Message: {result.message}");
    
    if (result.errors != null)
    {
        foreach (var error in result.errors)
        {
            Debug.LogError(error);
        }
    }
}
```

---

## 5. Integration Test Script

Create a full end-to-end test:

```bash
# run_integration_test.sh (or .bat for Windows)

#!/bin/bash
set -e

echo "=== Unity MCP Mixamo Integration Test ==="

# Check token
if [ -z "$MIXAMO_ACCESS_TOKEN" ]; then
    echo "ERROR: Set MIXAMO_ACCESS_TOKEN environment variable"
    exit 1
fi

cd D:/Mcp_Project/unity-mcp-mixamo/Server

# 1. Test Python setup
echo -e "\n[1/4] Testing Python environment..."
uv run python -c "from src.services import MixamoService; print('OK')"

# 2. Test keyword expansion
echo -e "\n[2/4] Testing keyword expansion..."
uv run python -c "
from src.services import expand_search_keywords
result = expand_search_keywords('run')
assert 'running' in result, 'Keyword expansion failed'
print(f'OK - run expands to {len(result)} terms')
"

# 3. Test API connection
echo -e "\n[3/4] Testing Mixamo API..."
uv run python -c "
import asyncio
import os
from src.services import MixamoService, MixamoConfig

async def test():
    config = MixamoConfig(access_token=os.environ['MIXAMO_ACCESS_TOKEN'])
    service = MixamoService(config)
    try:
        results = await service.search_animations('idle', limit=1)
        assert len(results) > 0, 'No results'
        print(f'OK - Found animation: {results[0].description}')
    finally:
        await service.close()

asyncio.run(test())
"

# 4. Test MCP tool interface
echo -e "\n[4/4] Testing MCP tool..."
uv run python -c "
import asyncio
from src.services import manage_mixamo

async def test():
    result = await manage_mixamo(operation='list_keywords')
    assert result['success'], 'Tool failed'
    print(f'OK - {len(result[\"keyword_categories\"])} categories')

asyncio.run(test())
"

echo -e "\n=== All tests passed! ==="
```

---

## 6. Troubleshooting

### Common Issues

| Error | Cause | Solution |
|-------|-------|----------|
| `AuthenticationError` | Invalid/expired token | Get new token from mixamo.com |
| `AnimationNotFoundError` | No results for query | Try different keywords |
| `Rate limit exceeded` | Too many requests | Wait 30 seconds, retry |
| `httpx not found` | Missing dependency | `uv pip install httpx` |
| `No primary character` | No character selected on Mixamo | Select a character on mixamo.com |

### Debug Mode

```python
# Enable verbose logging
import logging
logging.basicConfig(level=logging.DEBUG)

# Then run your tests
```

### Check Token Validity

```python
import asyncio
from src.services import MixamoService, MixamoConfig

async def check_token(token):
    config = MixamoConfig(access_token=token)
    service = MixamoService(config)
    try:
        char = await service.get_primary_character()
        print(f"✅ Token valid - Character: {char.name}")
    except Exception as e:
        print(f"❌ Token invalid: {e}")
    finally:
        await service.close()

# Run with: asyncio.run(check_token("your_token"))
```

---

## 7. Automated Test Suite

```bash
# Install test dependencies
uv pip install pytest pytest-asyncio pytest-cov

# Run full test suite
pytest tests/ -v --tb=short

# Run with coverage report
pytest tests/ -v --cov=src --cov-report=term-missing
```

Expected output:
```
tests/test_keywords.py::test_expand_run PASSED
tests/test_keywords.py::test_expand_attack PASSED
tests/test_keywords.py::test_expand_unknown PASSED
tests/test_mixamo_service.py::test_search PASSED
tests/test_mixamo_service.py::test_export PASSED
tests/test_manage_mixamo.py::test_list_keywords PASSED
tests/test_manage_mixamo.py::test_search_operation PASSED

==================== 7 passed in 5.23s ====================
```
