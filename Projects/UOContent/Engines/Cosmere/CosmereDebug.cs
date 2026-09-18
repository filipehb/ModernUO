using Server.Mobiles;

namespace Server.Engines.Cosmere;

/// <summary>
/// Per-player Cosmere debug traces. Session flag on <see cref="PlayerMobile.CosmereDebug"/>;
/// gated by <see cref="CosmereConfig.DebugEnabled"/>.
/// </summary>
public static class CosmereDebug
{
    public const string Prefix = "[CosmereDBG]";

    public static bool IsOn(PlayerMobile pm) =>
        pm != null && CosmereConfig.DebugEnabled && pm.CosmereDebug;

    public static void Trace(PlayerMobile pm, string message)
    {
        if (!IsOn(pm) || string.IsNullOrEmpty(message))
        {
            return;
        }

        pm.SendMessage($"{Prefix} {message}");
    }

    public static void TraceTest(PlayerMobile pm, CosmereTestResult test)
    {
        if (!IsOn(pm))
        {
            return;
        }

        if (test.Blocked)
        {
            Trace(pm, $"Test blocked: {test.BlockReason} ({CosmereSkills.GetDisplayName(test.Skill)} vs DC {test.Dc}).");
            return;
        }

        Trace(
            pm,
            $"Test {CosmereSkills.GetDisplayName(test.Skill)} vs DC {test.Dc}: " +
            $"d20={test.D20} mod={test.Modifier} plot={test.PlotFace}(+{test.ComplicationBonus}) " +
            $"exh=-{test.ExhaustedPenalty} total={test.Total} " +
            $"{(test.Success ? "SUCCESS" : "FAILURE")} opp={test.Opportunities} comp={test.Complications} " +
            $"stakes={test.RaiseStakes}"
        );
    }

    public static void TraceStrike(PlayerMobile pm, CosmereStrikeResult result, string role)
    {
        if (!IsOn(pm))
        {
            return;
        }

        if (result.Outcome == CosmereStrikeOutcome.Blocked)
        {
            Trace(pm, $"Strike {role} blocked: {result.Test.BlockReason}");
            return;
        }

        TraceTest(pm, result.Test);
        Trace(
            pm,
            $"Strike {role}: skill={CosmereSkills.GetDisplayName(result.AttackSkill)} DC={result.Dc} " +
            $"outcome={result.Outcome} dodge={result.Dodged} dmgType={result.DamageType} " +
            $"dmgRoll={result.DamageRoll} dmgDealt={result.DamageDealt}"
        );
    }

    public static void TracePools(
        PlayerMobile pm,
        string action,
        int healthBefore,
        int focusBefore,
        int invBefore = -1
    )
    {
        if (!IsOn(pm))
        {
            return;
        }

        var sheet = pm.CosmereSheet;
        if (sheet == null)
        {
            return;
        }

        if (invBefore >= 0)
        {
            Trace(
                pm,
                $"{action}: Health {healthBefore}→{sheet.Health}/{sheet.MaxHealth} " +
                $"Focus {focusBefore}→{sheet.Focus}/{sheet.MaxFocus} " +
                $"Inv {invBefore}→{sheet.Investiture}/{sheet.MaxInvestiture}"
            );
            return;
        }

        Trace(
            pm,
            $"{action}: Health {healthBefore}→{sheet.Health}/{sheet.MaxHealth} " +
            $"Focus {focusBefore}→{sheet.Focus}/{sheet.MaxFocus}"
        );
    }

    public static void TraceInvestiture(PlayerMobile pm, string action, int before, int amount, bool success)
    {
        if (!IsOn(pm))
        {
            return;
        }

        var sheet = pm.CosmereSheet;
        Trace(
            pm,
            $"{action}: spend={amount} ok={success} Inv {before}→{sheet.Investiture}/{sheet.MaxInvestiture}"
        );
    }
}
