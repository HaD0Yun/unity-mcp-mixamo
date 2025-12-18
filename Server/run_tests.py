#!/usr/bin/env python3
"""
Quick test runner for Unity MCP Mixamo.

Usage:
    # Run all tests (no API)
    python run_tests.py

    # Run with Mixamo token for API tests
    python run_tests.py --token YOUR_TOKEN

    # Run specific test
    python run_tests.py -k test_expand_run
"""

import argparse
import asyncio
import os
import sys

# Add src to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "src"))


def test_keyword_expansion():
    """Test keyword expansion without API."""
    from services.mixamo_service import expand_search_keywords, ANIMATION_KEYWORD_MAP

    print("\n=== Testing Keyword Expansion ===")

    # Test cases
    tests = [
        ("run", ["running", "jog", "sprint"]),
        ("attack", ["punch", "kick", "slash"]),
        ("idle", ["breathing", "standing"]),
        ("unknown_word", []),  # Should only return original
    ]

    passed = 0
    failed = 0

    for query, expected_in_result in tests:
        result = expand_search_keywords(query)

        # Check original is included
        if query not in result:
            print(f"  FAIL: {query} - original not in result")
            failed += 1
            continue

        # Check expected expansions
        missing = [e for e in expected_in_result if e not in result]
        if missing and expected_in_result:  # Allow empty expected for unknown words
            print(f"  FAIL: {query} - missing: {missing}")
            failed += 1
        else:
            print(f"  PASS: {query} -> {result[:4]}{'...' if len(result) > 4 else ''}")
            passed += 1

    print(f"\nKeyword tests: {passed} passed, {failed} failed")
    return failed == 0


async def test_api_search(token: str):
    """Test API search with token."""
    from services.mixamo_service import MixamoService, MixamoConfig

    print("\n=== Testing API Search ===")

    config = MixamoConfig(access_token=token)
    service = MixamoService(config)

    try:
        # Test search
        print("  Searching for 'idle'...")
        results = await service.search_animations("idle", limit=3)

        if results:
            print(f"  PASS: Found {len(results)} animations")
            for r in results[:3]:
                print(f"    - {r.description}")
            return True
        else:
            print("  FAIL: No results found")
            return False

    except Exception as e:
        print(f"  FAIL: {e}")
        return False
    finally:
        await service.close()


async def test_api_character(token: str):
    """Test getting primary character."""
    from services.mixamo_service import MixamoService, MixamoConfig

    print("\n=== Testing Primary Character ===")

    config = MixamoConfig(access_token=token)
    service = MixamoService(config)

    try:
        character = await service.get_primary_character()
        print(f"  PASS: Character = {character.name} (ID: {character.id[:8]}...)")
        return True
    except Exception as e:
        print(f"  FAIL: {e}")
        return False
    finally:
        await service.close()


async def test_mcp_tool():
    """Test MCP tool interface."""
    from services.manage_mixamo import manage_mixamo

    print("\n=== Testing MCP Tool Interface ===")

    # Test list_keywords (no token needed)
    result = await manage_mixamo(operation="list_keywords")

    if result["success"]:
        categories = list(result["keyword_categories"].keys())
        print(f"  PASS: list_keywords - {len(categories)} categories")
    else:
        print(f"  FAIL: list_keywords - {result.get('error')}")
        return False

    # Test error handling
    result = await manage_mixamo(operation="search", query="run")
    if not result["success"] and "token" in result.get("error", "").lower():
        print("  PASS: Correctly requires token")
    else:
        print("  FAIL: Should require token")
        return False

    return True


async def run_all_tests(token: str = None):
    """Run all tests."""
    print("=" * 50)
    print("Unity MCP Mixamo - Test Runner")
    print("=" * 50)

    all_passed = True

    # Keyword tests (always run)
    if not test_keyword_expansion():
        all_passed = False

    # MCP tool tests
    if not await test_mcp_tool():
        all_passed = False

    # API tests (only with token)
    if token:
        if not await test_api_character(token):
            all_passed = False
        if not await test_api_search(token):
            all_passed = False
    else:
        print("\n⚠️  Skipping API tests (no token provided)")
        print("   Use --token YOUR_TOKEN to run API tests")

    print("\n" + "=" * 50)
    if all_passed:
        print("✅ All tests passed!")
    else:
        print("❌ Some tests failed")
    print("=" * 50)

    return all_passed


def main():
    parser = argparse.ArgumentParser(description="Run Mixamo tests")
    parser.add_argument("--token", "-t", help="Mixamo access token for API tests")
    parser.add_argument("-k", "--keyword", help="Run only tests matching keyword")
    args = parser.parse_args()

    # Get token from arg or environment
    token = args.token or os.environ.get("MIXAMO_ACCESS_TOKEN")

    # Run tests
    success = asyncio.run(run_all_tests(token))
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
