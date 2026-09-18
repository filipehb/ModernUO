using System;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public readonly struct CosmereAdvancementRow
{
    public int AttrPoints { get; init; }
    public int FlatHealth { get; init; }
    public bool AddStrengthToHealth { get; init; }
    public int SkillRanks { get; init; }
    public int TalentPicks { get; init; }
    public int AncestryTalentSlots { get; init; }
}

public static class CosmereAdvancement
{
    // Rows indexed by level; level 0 unused, level 1 is creation baseline (zeros).
    private static readonly CosmereAdvancementRow[] _rows =
    [
        default, // 0
        new() { AttrPoints = 0, FlatHealth = 0, SkillRanks = 0, TalentPicks = 0 }, // 1 creation
        new() { FlatHealth = 5, SkillRanks = 2, TalentPicks = 1 }, // 2
        new() { AttrPoints = 1, FlatHealth = 5, SkillRanks = 2, TalentPicks = 1 }, // 3
        new() { FlatHealth = 5, SkillRanks = 2, TalentPicks = 1 }, // 4
        new() { FlatHealth = 5, SkillRanks = 2, TalentPicks = 1 }, // 5
        new()
        {
            AttrPoints = 1,
            FlatHealth = 4,
            AddStrengthToHealth = true,
            SkillRanks = 2,
            TalentPicks = 1,
            AncestryTalentSlots = 1
        }, // 6
        new() { FlatHealth = 4, SkillRanks = 2, TalentPicks = 1 }, // 7
        new() { FlatHealth = 4, SkillRanks = 2, TalentPicks = 1 }, // 8
        new() { AttrPoints = 1, FlatHealth = 4, SkillRanks = 2, TalentPicks = 1 }, // 9
        new() { FlatHealth = 4, SkillRanks = 2, TalentPicks = 1 }, // 10
        new()
        {
            FlatHealth = 3,
            AddStrengthToHealth = true,
            SkillRanks = 2,
            TalentPicks = 1,
            AncestryTalentSlots = 1
        }, // 11
        new() { AttrPoints = 1, FlatHealth = 3, SkillRanks = 2, TalentPicks = 1 }, // 12
        new() { FlatHealth = 3, SkillRanks = 2, TalentPicks = 1 }, // 13
        new() { FlatHealth = 3, SkillRanks = 2, TalentPicks = 1 }, // 14
        new() { AttrPoints = 1, FlatHealth = 3, SkillRanks = 2, TalentPicks = 1 }, // 15
        new()
        {
            FlatHealth = 2,
            AddStrengthToHealth = true,
            SkillRanks = 2,
            TalentPicks = 1,
            AncestryTalentSlots = 1
        }, // 16
        new() { FlatHealth = 2, SkillRanks = 2, TalentPicks = 1 }, // 17
        new() { AttrPoints = 1, FlatHealth = 2, SkillRanks = 2, TalentPicks = 1 }, // 18
        new() { FlatHealth = 2, SkillRanks = 2, TalentPicks = 1 }, // 19
        new() { FlatHealth = 2, SkillRanks = 2, TalentPicks = 1 }, // 20
    ];

    public static CosmereAdvancementRow GetRow(int level)
    {
        if (level >= 1 && level < _rows.Length)
        {
            return _rows[level];
        }

        if (level >= 21)
        {
            // Stub: +1 Health, +1 skill rank; ancestry slot only at exactly 21.
            return new CosmereAdvancementRow
            {
                FlatHealth = 1,
                SkillRanks = 1,
                AncestryTalentSlots = level == 21 ? 1 : 0
            };
        }

        return default;
    }

    public static int GetMaxSkillRank(int level) =>
        level switch
        {
            <= 5 => 2,
            <= 10 => 3,
            <= 15 => 4,
            _ => 5
        };

    public static int ComputeHealthGain(int level, int strength)
    {
        var row = GetRow(level);
        var gain = row.FlatHealth;
        if (row.AddStrengthToHealth)
        {
            gain += Math.Max(0, strength);
        }

        if (level >= 21 && CosmereConfig.Tier5HealthSoftCap > 0)
        {
            // Soft cap applied at ApplyLevelUp against current LevelHealthBonus.
            return gain;
        }

        return gain;
    }

    public static bool TryParseAttribute(string text, out CosmereAttribute attribute)
    {
        attribute = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        if (normalized.Equals("str", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("strength", StringComparison.OrdinalIgnoreCase))
        {
            attribute = CosmereAttribute.Strength;
            return true;
        }

        if (normalized.Equals("spd", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("speed", StringComparison.OrdinalIgnoreCase))
        {
            attribute = CosmereAttribute.Speed;
            return true;
        }

        if (normalized.Equals("int", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("intellect", StringComparison.OrdinalIgnoreCase))
        {
            attribute = CosmereAttribute.Intellect;
            return true;
        }

        if (normalized.Equals("wil", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("will", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("willpower", StringComparison.OrdinalIgnoreCase))
        {
            attribute = CosmereAttribute.Willpower;
            return true;
        }

        if (normalized.Equals("awa", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("awareness", StringComparison.OrdinalIgnoreCase))
        {
            attribute = CosmereAttribute.Awareness;
            return true;
        }

        if (normalized.Equals("pre", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("presence", StringComparison.OrdinalIgnoreCase))
        {
            attribute = CosmereAttribute.Presence;
            return true;
        }

        return Enum.TryParse(normalized, true, out attribute);
    }

    public static bool ApplyLevelUp(PlayerMobile pm, int targetLevel, bool allowBeyondMax = false)
    {
        if (pm == null)
        {
            return false;
        }

        if (!CosmereConfig.AdvancementEnabled)
        {
            pm.SendMessage("Cosmere advancement is disabled on this shard.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;

        if (targetLevel <= sheet.Level)
        {
            pm.SendMessage($"Already at level {sheet.Level}.");
            return false;
        }

        var hardCap = CosmereConfig.MaxLevel;
        if (!allowBeyondMax && targetLevel > hardCap)
        {
            targetLevel = hardCap;
        }

        if (targetLevel <= sheet.Level)
        {
            pm.SendMessage($"Cannot level past MaxLevel ({hardCap}) without GM override.");
            return false;
        }

        var previousMax = sheet.MaxHealth;
        var wasAtMax = sheet.Health >= previousMax;

        for (var level = sheet.Level + 1; level <= targetLevel; level++)
        {
            var row = GetRow(level);
            sheet.PendingAttrPoints += row.AttrPoints;
            sheet.PendingSkillRanks += row.SkillRanks;
            sheet.PendingTalentPicks += row.TalentPicks + row.AncestryTalentSlots;

            var healthGain = ComputeHealthGain(level, sheet.Strength);
            if (level >= 21 && CosmereConfig.Tier5HealthSoftCap > 0)
            {
                var room = CosmereConfig.Tier5HealthSoftCap - sheet.LevelHealthBonus;
                if (room <= 0)
                {
                    healthGain = 0;
                }
                else if (healthGain > room)
                {
                    healthGain = room;
                }
            }

            sheet.LevelHealthBonus += healthGain;
        }

        sheet.Level = targetLevel;
        sheet.ClampAllSkillRanks();

        if (wasAtMax)
        {
            sheet.Health = sheet.MaxHealth;
        }
        else
        {
            sheet.ClampPools();
        }

        CosmereFacade.Apply(pm);
        pm.SendMessage(
            $"Level {sheet.Level}. Pending: attr {sheet.PendingAttrPoints}, " +
            $"skills {sheet.PendingSkillRanks}, talents {sheet.PendingTalentPicks}. " +
            $"Health bonus {sheet.LevelHealthBonus} (MaxHealth {sheet.MaxHealth})."
        );
        CosmereDebug.Trace(
            pm,
            $"LevelUp → {sheet.Level}: HealthBonus={sheet.LevelHealthBonus} MaxHealth={sheet.MaxHealth} " +
            $"pending attr={sheet.PendingAttrPoints} skills={sheet.PendingSkillRanks} talents={sheet.PendingTalentPicks} " +
            $"maxSkillRank={sheet.MaxSkillRank}"
        );
        return true;
    }

    public static bool SpendAttr(PlayerMobile pm, CosmereAttribute attribute)
    {
        if (pm == null)
        {
            return false;
        }

        if (!CosmereConfig.AdvancementEnabled)
        {
            pm.SendMessage("Cosmere advancement is disabled on this shard.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (sheet.PendingAttrPoints < 1)
        {
            pm.SendMessage("No pending attribute points.");
            return false;
        }

        var previousMax = sheet.MaxHealth;
        var wasAtMax = sheet.Health >= previousMax;

        sheet.PendingAttrPoints--;
        sheet.SetAttribute(attribute, sheet.GetAttribute(attribute) + 1);

        if (wasAtMax)
        {
            sheet.Health = sheet.MaxHealth;
        }
        else
        {
            sheet.ClampPools();
        }

        if (sheet.GetAttribute(attribute) > 5)
        {
            pm.SendMessage($"Warning: {attribute} is above Handbook soft cap (5).");
        }

        CosmereFacade.Apply(pm);
        pm.SendMessage($"{attribute} is now {sheet.GetAttribute(attribute)}. Pending attr: {sheet.PendingAttrPoints}.");
        CosmereDebug.Trace(
            pm,
            $"SpendAttr {attribute}={sheet.GetAttribute(attribute)} pending={sheet.PendingAttrPoints} MaxHealth={sheet.MaxHealth}"
        );
        return true;
    }

    public static bool SpendSkill(PlayerMobile pm, CosmereSkill skill)
    {
        if (pm == null)
        {
            return false;
        }

        if (!CosmereConfig.AdvancementEnabled)
        {
            pm.SendMessage("Cosmere advancement is disabled on this shard.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (sheet.PendingSkillRanks < 1)
        {
            pm.SendMessage("No pending skill ranks.");
            return false;
        }

        var current = sheet.GetRank(skill);
        if (current >= sheet.MaxSkillRank)
        {
            pm.SendMessage($"{CosmereSkills.GetDisplayName(skill)} is already at the tier cap ({sheet.MaxSkillRank}).");
            return false;
        }

        sheet.PendingSkillRanks--;
        sheet.SetRank(skill, current + 1);
        CosmereFacade.Apply(pm);
        pm.SendMessage(
            $"{CosmereSkills.GetDisplayName(skill)} rank {sheet.GetRank(skill)}. Pending skills: {sheet.PendingSkillRanks}."
        );
        CosmereDebug.Trace(
            pm,
            $"SpendSkill {CosmereSkills.GetDisplayName(skill)} rank={sheet.GetRank(skill)} " +
            $"cap={sheet.MaxSkillRank} pending={sheet.PendingSkillRanks}"
        );
        return true;
    }

    public static bool GrantTalent(PlayerMobile pm, string talentId, bool force = false)
    {
        if (pm == null || string.IsNullOrWhiteSpace(talentId))
        {
            return false;
        }

        if (!CosmereConfig.AdvancementEnabled && !force)
        {
            pm.SendMessage("Cosmere advancement is disabled on this shard.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        talentId = talentId.Trim();

        if (sheet.HasTalent(talentId))
        {
            pm.SendMessage($"Already has talent '{talentId}'.");
            return false;
        }

        if (!force && sheet.PendingTalentPicks < 1)
        {
            pm.SendMessage("No pending talent picks.");
            return false;
        }

        if (!force)
        {
            sheet.PendingTalentPicks--;
        }

        sheet.Talents.Add(talentId);
        CosmereFacade.Apply(pm);
        pm.SendMessage($"Granted talent '{talentId}'. Pending talents: {sheet.PendingTalentPicks}.");
        CosmereDebug.Trace(
            pm,
            $"GrantTalent id={talentId} force={force} pending={sheet.PendingTalentPicks} count={sheet.Talents.Count}"
        );
        return true;
    }
}
