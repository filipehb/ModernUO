using System;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public readonly struct CosmereInjuryResult
{
    public required int RollTotal { get; init; }
    public required CosmereInjurySeverity Severity { get; init; }
    public required CosmereInjuryRecord Record { get; init; }
}

public static class CosmereInjury
{
    public static CosmereInjuryResult ResolveInjury(CosmereSheet sheet, int deflect = 0, int abilityMods = 0)
    {
        var existing = sheet.ActiveInjuryCount;
        var d20 = Utility.Random(20) + 1;
        var total = d20 + deflect + abilityMods - 5 * existing;

        var severity = total switch
        {
            <= -6 => CosmereInjurySeverity.Death,
            <= 0 => CosmereInjurySeverity.Permanent,
            <= 5 => CosmereInjurySeverity.Vicious,
            <= 15 => CosmereInjurySeverity.Shallow,
            _ => CosmereInjurySeverity.FleshWound
        };

        var effectRoll = Utility.Random(8) + 1;
        CosmereConditionKind? effectKind = null;
        var effectMagnitude = 0;
        string payload = null;

        switch (effectRoll)
        {
            case 1:
            case 2:
                {
                    effectKind = CosmereConditionKind.Exhausted;
                    effectMagnitude = 1;
                    break;
                }
            case 3:
                {
                    effectKind = CosmereConditionKind.Exhausted;
                    effectMagnitude = 2;
                    break;
                }
            case 4:
            case 5:
                {
                    effectKind = CosmereConditionKind.Slowed;
                    break;
                }
            case 6:
                {
                    effectKind = CosmereConditionKind.Disoriented;
                    break;
                }
            case 7:
                {
                    effectKind = CosmereConditionKind.Surprised;
                    break;
                }
            default:
                {
                    effectKind = CosmereConditionKind.OneHand;
                    payload = "OneHand";
                    break;
                }
        }

        var expires = DateTime.MinValue;
        if (severity == CosmereInjurySeverity.Vicious)
        {
            var days = 0;
            for (var i = 0; i < 6; i++)
            {
                days += Utility.Random(6) + 1;
            }

            expires = Core.Now.AddDays(days);
        }
        else if (severity == CosmereInjurySeverity.Shallow)
        {
            expires = Core.Now.AddDays(Utility.Random(6) + 1);
        }

        var record = new CosmereInjuryRecord
        {
            Id = sheet.NextInjuryId++,
            Severity = severity,
            EffectKind = effectKind,
            EffectMagnitude = effectMagnitude,
            Payload = payload,
            Created = Core.Now,
            Expires = expires,
            Cleared = false
        };

        sheet.Injuries.Add(record);

        if (effectKind == CosmereConditionKind.Exhausted)
        {
            sheet.AddExhausted(effectMagnitude, $"injury:{record.Id}");
        }
        else if (effectKind != null)
        {
            sheet.EnsureCondition(effectKind.Value, effectMagnitude, payload, $"injury:{record.Id}");
        }

        if (severity == CosmereInjurySeverity.Death)
        {
            sheet.Dead = true;
            sheet.SetFlagCondition(CosmereConditionKind.Unconscious, true, "death");
        }

        return new CosmereInjuryResult
        {
            RollTotal = total,
            Severity = severity,
            Record = record
        };
    }

    public static CosmereInjuryResult ResolveInjury(PlayerMobile pm, int deflect = 0, int abilityMods = 0)
    {
        pm.EnsureCosmereSheet();
        var result = ResolveInjury(pm.CosmereSheet, deflect, abilityMods);
        CosmereFacade.Apply(pm);

        pm.SendMessage(
            $"Injury: roll {result.RollTotal} → {result.Severity}."
        );
        CosmereDebug.Trace(
            pm,
            $"Injury roll={result.RollTotal} severity={result.Severity} " +
            $"id={result.Record?.Id} dead={pm.CosmereSheet.Dead} Health={pm.CosmereSheet.Health}"
        );

        if (result.Severity == CosmereInjurySeverity.Death)
        {
            pm.Frozen = true;
            if (CosmereConfig.PermanentDeath)
            {
                pm.SendMessage("You have died permanently on this shard.");
            }
            else
            {
                pm.SendMessage("You are dead. A GM can use CosmereRez to restore you.");
            }
        }

        return result;
    }
}
