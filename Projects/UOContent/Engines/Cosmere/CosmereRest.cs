using Server.Mobiles;

namespace Server.Engines.Cosmere;

public readonly struct CosmereShortRestResult
{
    public required int DieRoll { get; init; }
    public required int HealthGained { get; init; }
    public required int FocusGained { get; init; }
    public required bool Success { get; init; }
    public string Error { get; init; }
}

public static class CosmereRest
{
    public static CosmereShortRestResult ShortRest(CosmereSheet sheet, int healthShare)
    {
        if (sheet == null)
        {
            return new CosmereShortRestResult
            {
                DieRoll = 0,
                HealthGained = 0,
                FocusGained = 0,
                Success = false,
                Error = "No sheet."
            };
        }

        if (sheet.Dead)
        {
            return new CosmereShortRestResult
            {
                DieRoll = 0,
                HealthGained = 0,
                FocusGained = 0,
                Success = false,
                Error = "Cannot rest while dead."
            };
        }

        var sides = sheet.GetRecoveryDieSides();
        var roll = Utility.Random(sides) + 1;

        if (healthShare < 0 || healthShare > roll)
        {
            return new CosmereShortRestResult
            {
                DieRoll = roll,
                HealthGained = 0,
                FocusGained = 0,
                Success = false,
                Error = $"healthShare must be 0..{roll}."
            };
        }

        var focusShare = roll - healthShare;
        var healthBefore = sheet.Health;
        var focusBefore = sheet.Focus;

        CosmerePools.HealHealth(sheet, healthShare);
        CosmerePools.RecoverFocus(sheet, focusShare);
        sheet.LastShortRest = Core.Now;

        return new CosmereShortRestResult
        {
            DieRoll = roll,
            HealthGained = sheet.Health - healthBefore,
            FocusGained = sheet.Focus - focusBefore,
            Success = true
        };
    }

    public static CosmereShortRestResult ShortRest(PlayerMobile pm, int healthShare)
    {
        pm.EnsureCosmereSheet();
        var result = ShortRest(pm.CosmereSheet, healthShare);
        if (result.Success)
        {
            CosmereFacade.Apply(pm);
            pm.SendMessage(
                $"Short rest: d{pm.CosmereSheet.GetRecoveryDieSides()}={result.DieRoll} → +{result.HealthGained} Health, +{result.FocusGained} Focus."
            );
            CosmereDebug.Trace(
                pm,
                $"ShortRest die={result.DieRoll} healthShare={healthShare} " +
                $"+Health={result.HealthGained} +Focus={result.FocusGained} " +
                $"now Health {pm.CosmereSheet.Health}/{pm.CosmereSheet.MaxHealth} Focus {pm.CosmereSheet.Focus}/{pm.CosmereSheet.MaxFocus}"
            );
        }
        else if (result.Error != null)
        {
            pm.SendMessage(result.Error);
            CosmereDebug.Trace(pm, $"ShortRest failed: {result.Error}");
        }

        return result;
    }

    public static void LongRest(CosmereSheet sheet)
    {
        if (sheet == null || sheet.Dead)
        {
            return;
        }

        sheet.FillPoolsToMax();
        sheet.ReduceExhausted(1);
        sheet.ClearFleshWounds();
        sheet.ClearCondition(CosmereConditionKind.Unconscious);
        sheet.LastLongRest = Core.Now;
        sheet.ClampPools();
    }

    public static bool LongRest(PlayerMobile pm)
    {
        pm.EnsureCosmereSheet();
        if (pm.CosmereSheet.Dead)
        {
            pm.SendMessage("Cannot long rest while dead.");
            return false;
        }

        LongRest(pm.CosmereSheet);
        CosmereFacade.Apply(pm);
        pm.SendMessage("Long rest complete: pools full; Exhausted reduced by 1; flesh wounds cleared.");
        CosmereDebug.Trace(
            pm,
            $"LongRest: Health {pm.CosmereSheet.Health}/{pm.CosmereSheet.MaxHealth} " +
            $"Focus {pm.CosmereSheet.Focus}/{pm.CosmereSheet.MaxFocus} " +
            $"Inv {pm.CosmereSheet.Investiture}/{pm.CosmereSheet.MaxInvestiture} " +
            $"Exhausted={pm.CosmereSheet.GetExhaustedMagnitude()}"
        );
        return true;
    }
}
