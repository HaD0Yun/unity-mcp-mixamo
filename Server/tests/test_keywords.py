"""
Tests for keyword expansion functionality.
These tests do NOT require a Mixamo token.
"""

import pytest
from services.mixamo_service import expand_search_keywords, ANIMATION_KEYWORD_MAP


class TestKeywordExpansion:
    """Test the keyword expansion logic."""

    def test_expand_run(self):
        """Test that 'run' expands to running variants."""
        result = expand_search_keywords("run")
        assert "run" in result
        assert "running" in result
        assert "jog" in result
        assert "sprint" in result

    def test_expand_attack(self):
        """Test that 'attack' expands to combat moves."""
        result = expand_search_keywords("attack")
        assert "attack" in result
        assert "punch" in result
        assert "kick" in result
        assert "slash" in result

    def test_expand_idle(self):
        """Test that 'idle' expands to standing variants."""
        result = expand_search_keywords("idle")
        assert "idle" in result
        assert "breathing" in result
        assert "standing" in result

    def test_expand_unknown_word(self):
        """Unknown words should return just the original."""
        result = expand_search_keywords("xyzabc123")
        assert result == ["xyzabc123"]

    def test_expand_preserves_order(self):
        """Original query should be first in results."""
        result = expand_search_keywords("walk")
        assert result[0] == "walk"

    def test_expand_no_duplicates(self):
        """Results should not have duplicates."""
        result = expand_search_keywords("run")
        assert len(result) == len(set(result))

    def test_expand_case_insensitive(self):
        """Expansion should be case-insensitive."""
        result_lower = expand_search_keywords("run")
        result_upper = expand_search_keywords("RUN")
        # Original case is preserved but expansions are same
        assert "running" in result_lower
        assert "running" in result_upper

    def test_expand_partial_match(self):
        """Partial matches should expand."""
        result = expand_search_keywords("running")
        # Should find 'run' category and expand
        assert len(result) > 1

    def test_all_categories_present(self):
        """Verify all expected categories exist."""
        expected = ["run", "walk", "jump", "idle", "attack", "die", "dance"]
        for category in expected:
            assert category in ANIMATION_KEYWORD_MAP, f"Missing category: {category}"

    def test_keyword_map_values_are_lists(self):
        """All keyword values should be lists of strings."""
        for key, values in ANIMATION_KEYWORD_MAP.items():
            assert isinstance(values, list), f"{key} values is not a list"
            for v in values:
                assert isinstance(v, str), f"{key} contains non-string: {v}"


class TestKeywordCategories:
    """Test specific keyword categories."""

    def test_locomotion_keywords(self):
        """Test locomotion category."""
        locomotion = ["run", "walk", "jump", "crouch", "crawl"]
        for kw in locomotion:
            assert kw in ANIMATION_KEYWORD_MAP

    def test_combat_keywords(self):
        """Test combat category."""
        combat = ["attack", "punch", "kick", "sword", "shoot", "block", "dodge"]
        for kw in combat:
            assert kw in ANIMATION_KEYWORD_MAP

    def test_expression_keywords(self):
        """Test expression/reaction category."""
        expressions = ["wave", "dance", "die", "hurt", "celebrate", "taunt"]
        for kw in expressions:
            assert kw in ANIMATION_KEYWORD_MAP
