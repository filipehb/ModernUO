using System;
using System.Collections.Generic;

namespace Server.Engines.Cosmere;

/// <summary>
/// Plotweaver character sheet — server source of truth for Cosmere.
/// </summary>
public class CosmereSheet
{
    public const int SheetVersion = 5;

    private readonly int[] _skillRanks = new int[CosmereSkills.Count];

    public int Level { get; set; } = 1;

    public int Strength { get; set; } = 1;
    public int Speed { get; set; } = 1;
    public int Intellect { get; set; } = 1;
    public int Willpower { get; set; } = 1;
    public int Awareness { get; set; } = 1;
    public int Presence { get; set; } = 1;

    public int Health { get; set; }
    public int Focus { get; set; }
    public int Investiture { get; set; }

    public bool Dead { get; set; }
    public DateTime LastShortRest { get; set; } = DateTime.MinValue;
    public DateTime LastLongRest { get; set; } = DateTime.MinValue;
    public int NextInjuryId { get; set; } = 1;

    public bool IsMetalborn { get; set; }
    public CosmereAllomanticMetal AllomanticMetal { get; set; }
    public CosmerePowerTier PowerTier { get; set; }

    public int LevelHealthBonus { get; set; }
    public int PendingAttrPoints { get; set; }
    public int PendingSkillRanks { get; set; }
    public int PendingTalentPicks { get; set; }

    public CosmereProfessionId ProfessionId { get; set; }
    public int ProfessionRank { get; set; }
    public int ProfessionXp { get; set; }

    // v5 RP journal
    public string RpStory { get; set; } = "";
    public string RpGoals { get; set; } = "";
    public string RpAspirations { get; set; } = "";
    public string RpNotes { get; set; } = "";
    public int NextRpDiaryId { get; set; } = 1;
    public int NextRpEvalId { get; set; } = 1;
    public List<CosmereRpDiaryEntry> RpDiary { get; } = [];
    public List<CosmereRpEvaluation> RpEvaluations { get; } = [];

    public List<string> Expertises { get; } = [];
    public List<string> Talents { get; } = [];
    public List<CosmereGoalRecord> Goals { get; } = [];
    public List<CosmereCondition> Conditions { get; } = [];
    public List<CosmereInjuryRecord> Injuries { get; } = [];

    public int MaxHealth => 10 + Strength + LevelHealthBonus;

    /// <summary>Non-serialized soft penalty from sustenance (low hunger/thirst).</summary>
    public int FocusSoftPenalty { get; set; }

    public int MaxFocus => Math.Max(0, 2 + Willpower - FocusSoftPenalty);
    public int MaxInvestiture =>
        IsMetalborn ? 2 + Math.Max(Awareness, Presence) : 0;

    public int PhysicalDefense => 10 + Strength + Speed;
    public int CognitiveDefense => 10 + Intellect + Willpower;
    public int SpiritualDefense => 10 + Awareness + Presence;

    public int MaxSkillRank => Level switch
    {
        <= 5 => 2,
        <= 10 => 3,
        <= 15 => 4,
        _ => 5
    };

    public static CosmereSheet CreateMundaneStarter()
    {
        var sheet = new CosmereSheet
        {
            Level = 1,
            Strength = 1,
            Speed = 1,
            Intellect = 1,
            Willpower = 1,
            Awareness = 1,
            Presence = 1
        };

        sheet.FillPoolsToMax();
        return sheet;
    }

    public void FillPoolsToMax()
    {
        Health = MaxHealth;
        Focus = MaxFocus;
        Investiture = MaxInvestiture;
    }

    public int GetRecoveryDieSides() =>
        Willpower switch
        {
            <= 0 => 4,
            <= 2 => 6,
            <= 4 => 8,
            <= 6 => 10,
            <= 8 => 12,
            _ => 20
        };

    public int GetAttribute(CosmereAttribute attribute) =>
        attribute switch
        {
            CosmereAttribute.Strength => Strength,
            CosmereAttribute.Speed => Speed,
            CosmereAttribute.Intellect => Intellect,
            CosmereAttribute.Willpower => Willpower,
            CosmereAttribute.Awareness => Awareness,
            CosmereAttribute.Presence => Presence,
            _ => 0
        };

    public int GetRank(CosmereSkill skill) => _skillRanks[(int)skill];

    public void SetRank(CosmereSkill skill, int rank)
    {
        _skillRanks[(int)skill] = Math.Clamp(rank, 0, MaxSkillRank);
    }

    public int GetModifier(CosmereSkill skill)
    {
        var mod = GetAttribute(CosmereSkills.GetAttribute(skill)) + GetRank(skill);
        if (CosmereSkills.GetAttribute(skill) == CosmereAttribute.Awareness)
        {
            mod += GetTinEnhancedBonus();
        }

        return mod;
    }

    public int GetTinEnhancedBonus()
    {
        // Filled from active Tin burn via CosmereAllomancy when present on runtime state;
        // sheet-only path uses Enhanced condition magnitude if set.
        for (var i = 0; i < Conditions.Count; i++)
        {
            if (Conditions[i].Kind == CosmereConditionKind.Enhanced &&
                string.Equals(Conditions[i].Payload, "Awareness", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(0, Conditions[i].Magnitude);
            }
        }

        return 0;
    }

    public bool IsMetalDepleted(CosmereAllomanticMetal metal)
    {
        if (metal == CosmereAllomanticMetal.None)
        {
            return false;
        }

        var key = metal.ToString();
        for (var i = 0; i < Conditions.Count; i++)
        {
            if (Conditions[i].Kind == CosmereConditionKind.Depleted &&
                string.Equals(Conditions[i].Payload, key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public void SetMetalDepleted(CosmereAllomanticMetal metal, bool depleted)
    {
        if (metal == CosmereAllomanticMetal.None)
        {
            return;
        }

        var key = metal.ToString();
        for (var i = Conditions.Count - 1; i >= 0; i--)
        {
            if (Conditions[i].Kind == CosmereConditionKind.Depleted &&
                string.Equals(Conditions[i].Payload, key, StringComparison.OrdinalIgnoreCase))
            {
                Conditions.RemoveAt(i);
            }
        }

        if (depleted)
        {
            Conditions.Add(
                new CosmereCondition
                {
                    Kind = CosmereConditionKind.Depleted,
                    Payload = key,
                    Source = "vial"
                }
            );
        }
    }

    public void SetEnhancedAwareness(int magnitude)
    {
        for (var i = Conditions.Count - 1; i >= 0; i--)
        {
            if (Conditions[i].Kind == CosmereConditionKind.Enhanced &&
                string.Equals(Conditions[i].Payload, "Awareness", StringComparison.OrdinalIgnoreCase))
            {
                Conditions.RemoveAt(i);
            }
        }

        if (magnitude > 0)
        {
            Conditions.Add(
                new CosmereCondition
                {
                    Kind = CosmereConditionKind.Enhanced,
                    Magnitude = magnitude,
                    Payload = "Awareness",
                    Source = "tin"
                }
            );
        }
    }

    public string GetColloquialMetalName() =>
        AllomanticMetal switch
        {
            CosmereAllomanticMetal.Bronze => "Seeker",
            CosmereAllomanticMetal.Steel => "Coinshot",
            CosmereAllomanticMetal.Tin => "Tineye",
            CosmereAllomanticMetal.Zinc => "Rioter",
            _ => "Misting"
        };

    public int GetExhaustedMagnitude()
    {
        for (var i = 0; i < Conditions.Count; i++)
        {
            if (Conditions[i].Kind == CosmereConditionKind.Exhausted)
            {
                return Math.Max(0, Conditions[i].Magnitude);
            }
        }

        return 0;
    }

    public bool HasCondition(CosmereConditionKind kind)
    {
        for (var i = 0; i < Conditions.Count; i++)
        {
            if (Conditions[i].Kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    public void SetFlagCondition(CosmereConditionKind kind, bool present, string source = null)
    {
        for (var i = Conditions.Count - 1; i >= 0; i--)
        {
            if (Conditions[i].Kind == kind)
            {
                Conditions.RemoveAt(i);
            }
        }

        if (present)
        {
            Conditions.Add(new CosmereCondition { Kind = kind, Magnitude = 0, Source = source });
        }
    }

    public void AddExhausted(int magnitude, string source = null)
    {
        if (magnitude <= 0)
        {
            return;
        }

        for (var i = 0; i < Conditions.Count; i++)
        {
            if (Conditions[i].Kind == CosmereConditionKind.Exhausted)
            {
                Conditions[i].Magnitude += magnitude;
                if (source != null)
                {
                    Conditions[i].Source = source;
                }

                return;
            }
        }

        Conditions.Add(
            new CosmereCondition
            {
                Kind = CosmereConditionKind.Exhausted,
                Magnitude = magnitude,
                Source = source
            }
        );
    }

    public void ReduceExhausted(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        for (var i = 0; i < Conditions.Count; i++)
        {
            if (Conditions[i].Kind != CosmereConditionKind.Exhausted)
            {
                continue;
            }

            Conditions[i].Magnitude -= amount;
            if (Conditions[i].Magnitude <= 0)
            {
                Conditions.RemoveAt(i);
            }

            return;
        }
    }

    public void EnsureCondition(CosmereConditionKind kind, int magnitude = 0, string payload = null, string source = null)
    {
        if (kind == CosmereConditionKind.Exhausted)
        {
            AddExhausted(Math.Max(1, magnitude), source);
            return;
        }

        if (HasCondition(kind))
        {
            return;
        }

        Conditions.Add(
            new CosmereCondition
            {
                Kind = kind,
                Magnitude = magnitude,
                Payload = payload,
                Source = source
            }
        );
    }

    public void ClearCondition(CosmereConditionKind kind)
    {
        for (var i = Conditions.Count - 1; i >= 0; i--)
        {
            if (Conditions[i].Kind == kind)
            {
                Conditions.RemoveAt(i);
            }
        }
    }

    public int ActiveInjuryCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < Injuries.Count; i++)
            {
                if (!Injuries[i].Cleared)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public void ClearFleshWounds()
    {
        for (var i = 0; i < Injuries.Count; i++)
        {
            if (!Injuries[i].Cleared && Injuries[i].Severity == CosmereInjurySeverity.FleshWound)
            {
                Injuries[i].Cleared = true;
            }
        }
    }

    public void SetAttribute(CosmereAttribute attribute, int value)
    {
        value = Math.Max(0, value);
        switch (attribute)
        {
            case CosmereAttribute.Strength:
                Strength = value;
                break;
            case CosmereAttribute.Speed:
                Speed = value;
                break;
            case CosmereAttribute.Intellect:
                Intellect = value;
                break;
            case CosmereAttribute.Willpower:
                Willpower = value;
                break;
            case CosmereAttribute.Awareness:
                Awareness = value;
                break;
            case CosmereAttribute.Presence:
                Presence = value;
                break;
        }
    }

    public void ClampAllSkillRanks()
    {
        var cap = MaxSkillRank;
        for (var i = 0; i < CosmereSkills.Count; i++)
        {
            if (_skillRanks[i] > cap)
            {
                _skillRanks[i] = cap;
            }
        }
    }

    public bool HasTalent(string talentId)
    {
        if (string.IsNullOrWhiteSpace(talentId))
        {
            return false;
        }

        for (var i = 0; i < Talents.Count; i++)
        {
            if (string.Equals(Talents[i], talentId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public CosmereGoalRecord FindGoal(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        for (var i = 0; i < Goals.Count; i++)
        {
            if (string.Equals(Goals[i].Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return Goals[i];
            }
        }

        return null;
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(SheetVersion);
        writer.Write(Level);
        writer.Write(Strength);
        writer.Write(Speed);
        writer.Write(Intellect);
        writer.Write(Willpower);
        writer.Write(Awareness);
        writer.Write(Presence);
        writer.Write(Health);
        writer.Write(Focus);
        writer.Write(Investiture);

        for (var i = 0; i < CosmereSkills.Count; i++)
        {
            writer.Write(_skillRanks[i]);
        }

        writer.Write(Expertises.Count);
        foreach (var expertise in Expertises)
        {
            writer.Write(expertise);
        }

        // v1+
        writer.Write(Dead);
        writer.Write(LastShortRest);
        writer.Write(LastLongRest);
        writer.Write(NextInjuryId);

        writer.Write(Conditions.Count);
        for (var i = 0; i < Conditions.Count; i++)
        {
            Conditions[i].Serialize(writer);
        }

        writer.Write(Injuries.Count);
        for (var i = 0; i < Injuries.Count; i++)
        {
            Injuries[i].Serialize(writer);
        }

        // v2+
        writer.Write(IsMetalborn);
        writer.Write((int)AllomanticMetal);
        writer.Write((int)PowerTier);

        // v3+
        writer.Write(LevelHealthBonus);
        writer.Write(PendingAttrPoints);
        writer.Write(PendingSkillRanks);
        writer.Write(PendingTalentPicks);

        writer.Write(Talents.Count);
        for (var i = 0; i < Talents.Count; i++)
        {
            writer.Write(Talents[i]);
        }

        writer.Write(Goals.Count);
        for (var i = 0; i < Goals.Count; i++)
        {
            Goals[i].Serialize(writer);
        }

        // v4+
        writer.Write((int)ProfessionId);
        writer.Write(ProfessionRank);
        writer.Write(ProfessionXp);

        // v5+
        writer.Write(RpStory);
        writer.Write(RpGoals);
        writer.Write(RpAspirations);
        writer.Write(RpNotes);
        writer.Write(NextRpDiaryId);
        writer.Write(NextRpEvalId);

        writer.Write(RpDiary.Count);
        for (var i = 0; i < RpDiary.Count; i++)
        {
            RpDiary[i].Serialize(writer);
        }

        writer.Write(RpEvaluations.Count);
        for (var i = 0; i < RpEvaluations.Count; i++)
        {
            RpEvaluations[i].Serialize(writer);
        }
    }

    public void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadInt();

        Level = reader.ReadInt();
        Strength = reader.ReadInt();
        Speed = reader.ReadInt();
        Intellect = reader.ReadInt();
        Willpower = reader.ReadInt();
        Awareness = reader.ReadInt();
        Presence = reader.ReadInt();
        Health = reader.ReadInt();
        Focus = reader.ReadInt();
        Investiture = reader.ReadInt();

        var rankCount = version >= 2 ? CosmereSkills.Count : CosmereSkills.LegacyCount;
        for (var i = 0; i < rankCount; i++)
        {
            _skillRanks[i] = reader.ReadInt();
        }

        for (var i = rankCount; i < CosmereSkills.Count; i++)
        {
            _skillRanks[i] = 0;
        }

        Expertises.Clear();
        var expertiseCount = reader.ReadInt();
        for (var i = 0; i < expertiseCount; i++)
        {
            Expertises.Add(reader.ReadString());
        }

        Conditions.Clear();
        Injuries.Clear();
        Talents.Clear();
        Goals.Clear();
        Dead = false;
        LastShortRest = DateTime.MinValue;
        LastLongRest = DateTime.MinValue;
        NextInjuryId = 1;
        IsMetalborn = false;
        AllomanticMetal = CosmereAllomanticMetal.None;
        PowerTier = CosmerePowerTier.None;
        LevelHealthBonus = 0;
        PendingAttrPoints = 0;
        PendingSkillRanks = 0;
        PendingTalentPicks = 0;
        ProfessionId = CosmereProfessionId.None;
        ProfessionRank = 0;
        ProfessionXp = 0;
        RpStory = "";
        RpGoals = "";
        RpAspirations = "";
        RpNotes = "";
        NextRpDiaryId = 1;
        NextRpEvalId = 1;
        RpDiary.Clear();
        RpEvaluations.Clear();

        if (version >= 1)
        {
            Dead = reader.ReadBool();
            LastShortRest = reader.ReadDateTime();
            LastLongRest = reader.ReadDateTime();
            NextInjuryId = reader.ReadInt();

            var conditionCount = reader.ReadInt();
            for (var i = 0; i < conditionCount; i++)
            {
                Conditions.Add(CosmereCondition.Deserialize(reader));
            }

            var injuryCount = reader.ReadInt();
            for (var i = 0; i < injuryCount; i++)
            {
                Injuries.Add(CosmereInjuryRecord.Deserialize(reader));
            }
        }

        if (version >= 2)
        {
            IsMetalborn = reader.ReadBool();
            AllomanticMetal = (CosmereAllomanticMetal)reader.ReadInt();
            PowerTier = (CosmerePowerTier)reader.ReadInt();
        }

        if (version >= 3)
        {
            LevelHealthBonus = reader.ReadInt();
            PendingAttrPoints = reader.ReadInt();
            PendingSkillRanks = reader.ReadInt();
            PendingTalentPicks = reader.ReadInt();

            var talentCount = reader.ReadInt();
            for (var i = 0; i < talentCount; i++)
            {
                Talents.Add(reader.ReadString());
            }

            var goalCount = reader.ReadInt();
            for (var i = 0; i < goalCount; i++)
            {
                Goals.Add(CosmereGoalRecord.Deserialize(reader));
            }
        }

        if (version >= 4)
        {
            ProfessionId = (CosmereProfessionId)reader.ReadInt();
            ProfessionRank = reader.ReadInt();
            ProfessionXp = reader.ReadInt();
        }

        if (version >= 5)
        {
            RpStory = reader.ReadString() ?? "";
            RpGoals = reader.ReadString() ?? "";
            RpAspirations = reader.ReadString() ?? "";
            RpNotes = reader.ReadString() ?? "";
            NextRpDiaryId = reader.ReadInt();
            NextRpEvalId = reader.ReadInt();

            var diaryCount = reader.ReadInt();
            for (var i = 0; i < diaryCount; i++)
            {
                RpDiary.Add(CosmereRpDiaryEntry.Deserialize(reader));
            }

            var evalCount = reader.ReadInt();
            for (var i = 0; i < evalCount; i++)
            {
                RpEvaluations.Add(CosmereRpEvaluation.Deserialize(reader));
            }
        }

        ClampPools();
    }

    public void ClampPools()
    {
        Health = Math.Clamp(Health, 0, MaxHealth);
        Focus = Math.Clamp(Focus, 0, MaxFocus);
        Investiture = Math.Clamp(Investiture, 0, MaxInvestiture);
    }
}
