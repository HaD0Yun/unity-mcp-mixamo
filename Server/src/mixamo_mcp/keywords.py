"""Keyword mappings for natural language to Mixamo search queries."""

from typing import Optional
from .models import AnimationCategory

# Keyword to search queries mapping
KEYWORD_MAPPINGS: dict[str, list[str]] = {
    # Locomotion
    "idle": ["idle", "breathing idle", "standing idle", "happy idle"],
    "walk": ["walking", "walk", "strut walk", "sneaking"],
    "run": ["running", "run", "jog", "sprint", "fast run"],
    "jump": ["jump", "jumping", "hop", "leap", "running jump"],
    "crouch": ["crouch", "crouching", "sneaking", "stealth"],
    "crawl": ["crawl", "crawling", "prone"],
    "climb": ["climb", "climbing", "ladder climb"],
    "swim": ["swim", "swimming", "treading water"],
    "fall": ["falling", "fall", "falling idle"],
    "land": ["landing", "land", "hard landing"],
    "slide": ["slide", "sliding", "baseball slide"],
    "roll": ["roll", "rolling", "combat roll", "dive roll"],
    "strafe": ["strafe", "strafing", "side step"],
    "turn": ["turn", "turning", "turn around", "180 turn"],
    # Combat
    "attack": ["attack", "slash", "strike", "hit"],
    "punch": ["punch", "punching", "jab", "hook", "uppercut", "cross punch"],
    "kick": ["kick", "kicking", "roundhouse", "front kick", "side kick"],
    "sword": ["sword", "sword slash", "sword attack", "great sword"],
    "block": ["block", "blocking", "shield block", "parry"],
    "dodge": ["dodge", "dodging", "evade", "sidestep"],
    "shoot": ["shoot", "shooting", "rifle", "pistol", "aim"],
    "reload": ["reload", "reloading", "magazine"],
    "throw": ["throw", "throwing", "grenade throw"],
    "hit": ["hit reaction", "take damage", "get hit", "impact"],
    "death": ["death", "dying", "die", "killed"],
    "knockdown": ["knockdown", "knocked down", "fall back"],
    # Social / Emotes
    "wave": ["wave", "waving", "greeting", "hello"],
    "bow": ["bow", "bowing", "respect"],
    "clap": ["clap", "clapping", "applause"],
    "cheer": ["cheer", "cheering", "celebration", "victory"],
    "laugh": ["laugh", "laughing", "lol"],
    "cry": ["cry", "crying", "sad", "sobbing"],
    "angry": ["angry", "rage", "frustrated"],
    "shrug": ["shrug", "confused", "I don't know"],
    "point": ["point", "pointing", "gesture"],
    "talk": ["talk", "talking", "conversation", "arguing"],
    "sit": ["sit", "sitting", "sit down", "seated"],
    "sleep": ["sleep", "sleeping", "lay down"],
    "pray": ["pray", "praying", "kneel"],
    "salute": ["salute", "military salute"],
    "taunt": ["taunt", "taunting", "provoke"],
    "think": ["think", "thinking", "pondering"],
    "nod": ["nod", "nodding", "yes"],
    "shake head": ["shake head", "no", "disagree"],
    # Dance
    "dance": ["dance", "dancing", "groove"],
    "hip hop": ["hip hop", "hip hop dance", "breakdance"],
    "salsa": ["salsa", "salsa dancing", "latin dance"],
    "ballet": ["ballet", "pirouette", "ballet dance"],
    "robot": ["robot", "robot dance", "robotic"],
    "macarena": ["macarena"],
    "twerk": ["twerk", "twerking"],
    "moonwalk": ["moonwalk"],
    "breakdance": ["breakdance", "breaking", "b-boy"],
    "shuffle": ["shuffle", "shuffling"],
    # Sports
    "baseball": ["baseball", "batting", "pitching", "catching"],
    "basketball": ["basketball", "dribble", "shoot ball", "dunk"],
    "soccer": ["soccer", "football", "kick ball", "header"],
    "golf": ["golf", "golf swing", "putting"],
    "tennis": ["tennis", "forehand", "backhand", "serve"],
    "boxing": ["boxing", "boxer", "fighting stance"],
    "martial arts": ["martial arts", "karate", "kung fu", "taekwondo"],
    # Misc / Utility
    "pickup": ["pickup", "pick up", "grab", "collect"],
    "use": ["use", "interact", "activate", "press button"],
    "push": ["push", "pushing", "shove"],
    "pull": ["pull", "pulling", "drag"],
    "carry": ["carry", "carrying", "hold"],
    "drink": ["drink", "drinking"],
    "eat": ["eat", "eating"],
    "phone": ["phone", "cellphone", "texting"],
    "type": ["type", "typing", "keyboard"],
    "look": ["look", "looking", "look around"],
}

# Category mappings
KEYWORD_CATEGORIES: dict[str, AnimationCategory] = {
    # Locomotion
    "idle": AnimationCategory.LOCOMOTION,
    "walk": AnimationCategory.LOCOMOTION,
    "run": AnimationCategory.LOCOMOTION,
    "jump": AnimationCategory.LOCOMOTION,
    "crouch": AnimationCategory.LOCOMOTION,
    "crawl": AnimationCategory.LOCOMOTION,
    "climb": AnimationCategory.LOCOMOTION,
    "swim": AnimationCategory.LOCOMOTION,
    "fall": AnimationCategory.LOCOMOTION,
    "land": AnimationCategory.LOCOMOTION,
    "slide": AnimationCategory.LOCOMOTION,
    "roll": AnimationCategory.LOCOMOTION,
    "strafe": AnimationCategory.LOCOMOTION,
    "turn": AnimationCategory.LOCOMOTION,
    # Combat
    "attack": AnimationCategory.COMBAT,
    "punch": AnimationCategory.COMBAT,
    "kick": AnimationCategory.COMBAT,
    "sword": AnimationCategory.COMBAT,
    "block": AnimationCategory.COMBAT,
    "dodge": AnimationCategory.COMBAT,
    "shoot": AnimationCategory.COMBAT,
    "reload": AnimationCategory.COMBAT,
    "throw": AnimationCategory.COMBAT,
    "hit": AnimationCategory.COMBAT,
    "death": AnimationCategory.COMBAT,
    "knockdown": AnimationCategory.COMBAT,
    # Social
    "wave": AnimationCategory.SOCIAL,
    "bow": AnimationCategory.SOCIAL,
    "clap": AnimationCategory.SOCIAL,
    "cheer": AnimationCategory.SOCIAL,
    "laugh": AnimationCategory.SOCIAL,
    "cry": AnimationCategory.SOCIAL,
    "angry": AnimationCategory.SOCIAL,
    "shrug": AnimationCategory.SOCIAL,
    "point": AnimationCategory.SOCIAL,
    "talk": AnimationCategory.SOCIAL,
    "sit": AnimationCategory.SOCIAL,
    "sleep": AnimationCategory.SOCIAL,
    "pray": AnimationCategory.SOCIAL,
    "salute": AnimationCategory.SOCIAL,
    "taunt": AnimationCategory.SOCIAL,
    "think": AnimationCategory.SOCIAL,
    "nod": AnimationCategory.SOCIAL,
    "shake head": AnimationCategory.SOCIAL,
    # Dance
    "dance": AnimationCategory.DANCE,
    "hip hop": AnimationCategory.DANCE,
    "salsa": AnimationCategory.DANCE,
    "ballet": AnimationCategory.DANCE,
    "robot": AnimationCategory.DANCE,
    "macarena": AnimationCategory.DANCE,
    "twerk": AnimationCategory.DANCE,
    "moonwalk": AnimationCategory.DANCE,
    "breakdance": AnimationCategory.DANCE,
    "shuffle": AnimationCategory.DANCE,
    # Sports
    "baseball": AnimationCategory.SPORTS,
    "basketball": AnimationCategory.SPORTS,
    "soccer": AnimationCategory.SPORTS,
    "golf": AnimationCategory.SPORTS,
    "tennis": AnimationCategory.SPORTS,
    "boxing": AnimationCategory.SPORTS,
    "martial arts": AnimationCategory.SPORTS,
    # Misc
    "pickup": AnimationCategory.MISC,
    "use": AnimationCategory.MISC,
    "push": AnimationCategory.MISC,
    "pull": AnimationCategory.MISC,
    "carry": AnimationCategory.MISC,
    "drink": AnimationCategory.MISC,
    "eat": AnimationCategory.MISC,
    "phone": AnimationCategory.MISC,
    "type": AnimationCategory.MISC,
    "look": AnimationCategory.MISC,
}


def get_search_queries(keyword: str) -> list[str]:
    """Get search queries for a keyword."""
    keyword_lower = keyword.lower().strip()

    # Direct match
    if keyword_lower in KEYWORD_MAPPINGS:
        return KEYWORD_MAPPINGS[keyword_lower]

    # Partial match
    for key, queries in KEYWORD_MAPPINGS.items():
        if keyword_lower in key or key in keyword_lower:
            return queries

    # No match - return keyword as-is
    return [keyword]


def get_category(keyword: str) -> AnimationCategory:
    """Get the category for a keyword."""
    keyword_lower = keyword.lower().strip()
    return KEYWORD_CATEGORIES.get(keyword_lower, AnimationCategory.MISC)


def get_all_keywords() -> dict[str, list[str]]:
    """Get all available keywords grouped by category."""
    result: dict[str, list[str]] = {}

    for keyword, category in KEYWORD_CATEGORIES.items():
        cat_name = category.value
        if cat_name not in result:
            result[cat_name] = []
        result[cat_name].append(keyword)

    return result


def filter_keywords_by_category(category: Optional[str]) -> dict[str, list[str]]:
    """Filter keywords by category name."""
    all_keywords = get_all_keywords()

    if not category:
        return all_keywords

    category_lower = category.lower().strip()
    return {k: v for k, v in all_keywords.items() if category_lower in k.lower()}
