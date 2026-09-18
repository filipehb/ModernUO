using System;

namespace Server.Engines.Cosmere;

public enum CosmereSkill
{
    Agility = 0,
    Athletics = 1,
    Crafting = 2,
    Deception = 3,
    Deduction = 4,
    Discipline = 5,
    HeavyWeaponry = 6,
    Insight = 7,
    Intimidation = 8,
    Leadership = 9,
    LightWeaponry = 10,
    Lore = 11,
    Medicine = 12,
    Perception = 13,
    Persuasion = 14,
    Stealth = 15,
    Survival = 16,
    Thievery = 17,
    Allomancy = 18
}

public enum CosmereAttribute
{
    Strength,
    Speed,
    Intellect,
    Willpower,
    Awareness,
    Presence
}

public enum CosmerePlotFace
{
    Blank,
    Opportunity,
    Complication2,
    Complication4
}

public static class CosmereSkills
{
    public const int Count = 19;
    public const int LegacyCount = 18;

    private static readonly CosmereAttribute[] _attributes =
    [
        CosmereAttribute.Speed,      // Agility
        CosmereAttribute.Strength,   // Athletics
        CosmereAttribute.Intellect,  // Crafting
        CosmereAttribute.Presence,   // Deception
        CosmereAttribute.Intellect,  // Deduction
        CosmereAttribute.Willpower,  // Discipline
        CosmereAttribute.Strength,   // HeavyWeaponry
        CosmereAttribute.Awareness,  // Insight
        CosmereAttribute.Willpower,  // Intimidation
        CosmereAttribute.Presence,   // Leadership
        CosmereAttribute.Speed,      // LightWeaponry
        CosmereAttribute.Intellect,  // Lore
        CosmereAttribute.Intellect,  // Medicine
        CosmereAttribute.Awareness,  // Perception
        CosmereAttribute.Presence,   // Persuasion
        CosmereAttribute.Speed,      // Stealth
        CosmereAttribute.Awareness,  // Survival
        CosmereAttribute.Speed,      // Thievery
        CosmereAttribute.Willpower   // Allomancy
    ];

    private static readonly string[] _names =
    [
        "Agility",
        "Athletics",
        "Crafting",
        "Deception",
        "Deduction",
        "Discipline",
        "Heavy Weaponry",
        "Insight",
        "Intimidation",
        "Leadership",
        "Light Weaponry",
        "Lore",
        "Medicine",
        "Perception",
        "Persuasion",
        "Stealth",
        "Survival",
        "Thievery",
        "Allomancy"
    ];

    public static CosmereAttribute GetAttribute(CosmereSkill skill) => _attributes[(int)skill];

    public static string GetDisplayName(CosmereSkill skill) => _names[(int)skill];

    public static bool TryParse(string text, out CosmereSkill skill)
    {
        skill = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Replace(" ", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        if (Enum.TryParse(normalized, true, out skill))
        {
            return true;
        }

        for (var i = 0; i < Count; i++)
        {
            var name = _names[i].Replace(" ", "", StringComparison.Ordinal);
            if (name.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                skill = (CosmereSkill)i;
                return true;
            }
        }

        return false;
    }
}
