using System;

namespace Server.Engines.Cosmere;

public enum CosmereDamageType
{
    Impact,
    Keen,
    Energy,
    Spirit,
    Vital
}

public enum CosmereConditionKind
{
    Exhausted,
    Unconscious,
    Slowed,
    Disoriented,
    Surprised,
    Prone,
    Restrained,
    Immobilized,
    Stunned,
    Determined,
    Focused,
    Afflicted,
    Depleted,
    Diminished,
    Enhanced,
    OneHand,
    Rioted,
    Influenced
}

public enum CosmereAllomanticMetal
{
    None = 0,
    Bronze = 1,
    Steel = 2,
    Tin = 3,
    Zinc = 4
}

[Flags]
public enum CosmereMetalFlags
{
    None = 0,
    Bronze = 1 << 0,
    Steel = 1 << 1,
    Tin = 1 << 2,
    Zinc = 1 << 3,
    AllStarter = Bronze | Steel | Tin | Zinc
}

public enum CosmerePowerTier
{
    None = 0,
    Nascent = 1,
    Full = 2
}

public enum CosmereProfessionId
{
    None = 0,
    Artisan = 1,
    Producer = 2,
    Merchant = 3
}

public enum CosmereDayPeriod
{
    Morning = 0,
    Afternoon = 1,
    Evening = 2,
    Night = 3
}

public enum CosmereGoalStatus
{
    Active = 0,
    Completed = 1,
    Failed = 2
}

public enum CosmereGoalRewardKind
{
    None = 0,
    UnlockFullAllomancy = 1,
    GrantTalent = 2,
    GrantItemStub = 3
}

public class CosmereGoalRecord
{
    public string Id { get; set; }
    public string Title { get; set; }
    public CosmereGoalStatus Status { get; set; }
    public CosmereGoalRewardKind RewardKind { get; set; }
    public string RewardPayload { get; set; }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(Id);
        writer.Write(Title);
        writer.Write((int)Status);
        writer.Write((int)RewardKind);
        writer.Write(RewardPayload);
    }

    public static CosmereGoalRecord Deserialize(IGenericReader reader) =>
        new()
        {
            Id = reader.ReadString(),
            Title = reader.ReadString(),
            Status = (CosmereGoalStatus)reader.ReadInt(),
            RewardKind = (CosmereGoalRewardKind)reader.ReadInt(),
            RewardPayload = reader.ReadString()
        };
}

public enum CosmereInjurySeverity
{
    FleshWound,
    Shallow,
    Vicious,
    Permanent,
    Death
}

public class CosmereCondition
{
    public CosmereConditionKind Kind { get; set; }
    public int Magnitude { get; set; }
    public string Payload { get; set; }
    public string Source { get; set; }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write((int)Kind);
        writer.Write(Magnitude);
        writer.Write(Payload);
        writer.Write(Source);
    }

    public static CosmereCondition Deserialize(IGenericReader reader)
    {
        return new CosmereCondition
        {
            Kind = (CosmereConditionKind)reader.ReadInt(),
            Magnitude = reader.ReadInt(),
            Payload = reader.ReadString(),
            Source = reader.ReadString()
        };
    }
}

public class CosmereInjuryRecord
{
    public int Id { get; set; }
    public CosmereInjurySeverity Severity { get; set; }
    public CosmereConditionKind? EffectKind { get; set; }
    public int EffectMagnitude { get; set; }
    public string Payload { get; set; }
    public DateTime Created { get; set; }
    public DateTime Expires { get; set; } // MinValue = none / flesh until long rest
    public bool Cleared { get; set; }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(Id);
        writer.Write((int)Severity);
        writer.Write(EffectKind.HasValue);
        if (EffectKind.HasValue)
        {
            writer.Write((int)EffectKind.Value);
        }

        writer.Write(EffectMagnitude);
        writer.Write(Payload);
        writer.Write(Created);
        writer.Write(Expires);
        writer.Write(Cleared);
    }

    public static CosmereInjuryRecord Deserialize(IGenericReader reader)
    {
        var record = new CosmereInjuryRecord
        {
            Id = reader.ReadInt(),
            Severity = (CosmereInjurySeverity)reader.ReadInt()
        };

        if (reader.ReadBool())
        {
            record.EffectKind = (CosmereConditionKind)reader.ReadInt();
        }

        record.EffectMagnitude = reader.ReadInt();
        record.Payload = reader.ReadString();
        record.Created = reader.ReadDateTime();
        record.Expires = reader.ReadDateTime();
        record.Cleared = reader.ReadBool();
        return record;
    }
}

public class CosmereRpDiaryEntry
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(Id);
        writer.Write(Created);
        writer.Write(Title);
        writer.Write(Body);
    }

    public static CosmereRpDiaryEntry Deserialize(IGenericReader reader) =>
        new()
        {
            Id = reader.ReadInt(),
            Created = reader.ReadDateTime(),
            Title = reader.ReadString() ?? "",
            Body = reader.ReadString() ?? ""
        };
}

public class CosmereRpEvaluation
{
    public int Id { get; set; }
    public Serial EvaluatorSerial { get; set; }
    public string EvaluatorName { get; set; } = "";
    public DateTime Created { get; set; }
    public int Score { get; set; }
    public string Feedback { get; set; } = "";
    public bool Superseded { get; set; }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(Id);
        writer.Write(EvaluatorSerial);
        writer.Write(EvaluatorName);
        writer.Write(Created);
        writer.Write(Score);
        writer.Write(Feedback);
        writer.Write(Superseded);
    }

    public static CosmereRpEvaluation Deserialize(IGenericReader reader) =>
        new()
        {
            Id = reader.ReadInt(),
            EvaluatorSerial = reader.ReadSerial(),
            EvaluatorName = reader.ReadString() ?? "",
            Created = reader.ReadDateTime(),
            Score = reader.ReadInt(),
            Feedback = reader.ReadString() ?? "",
            Superseded = reader.ReadBool()
        };
}
