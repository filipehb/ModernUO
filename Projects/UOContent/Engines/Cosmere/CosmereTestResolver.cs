namespace Server.Engines.Cosmere;

public readonly struct CosmereTestResult
{
    public required CosmereSkill Skill { get; init; }
    public required int Dc { get; init; }
    public required int D20 { get; init; }
    public required int Modifier { get; init; }
    public required CosmerePlotFace PlotFace { get; init; }
    public required int ComplicationBonus { get; init; }
    public required int ExhaustedPenalty { get; init; }
    public required int Total { get; init; }
    public required bool Success { get; init; }
    public required int Opportunities { get; init; }
    public required int Complications { get; init; }
    public required bool RaiseStakes { get; init; }
    public required bool Blocked { get; init; }
    public string BlockReason { get; init; }
}

public static class CosmereTestResolver
{
    public static CosmereTestResult Resolve(
        CosmereSheet sheet,
        CosmereSkill skill,
        int dc,
        int advantages = 0,
        int disadvantages = 0,
        bool raiseStakes = false
    )
    {
        if (sheet.Dead)
        {
            return BlockedResult(sheet, skill, dc, raiseStakes, "Dead");
        }

        if (sheet.HasCondition(CosmereConditionKind.Unconscious))
        {
            return BlockedResult(sheet, skill, dc, raiseStakes, "Unconscious");
        }

        var netAdv = advantages - disadvantages;
        if (sheet.HasCondition(CosmereConditionKind.Slowed) &&
            CosmereSkills.GetAttribute(skill) == CosmereAttribute.Speed)
        {
            // Cumbersome / Slowed: +1 disadvantage on Speed-attribute skills
            netAdv -= 1;
        }

        var advCount = netAdv > 0 ? netAdv : 0;
        var disadvCount = netAdv < 0 ? -netAdv : 0;

        // Phase 1: only duplicate the d20 (one die type choosable once per stack rules).
        var d20 = RollD20();
        if (advCount > 0)
        {
            var other = RollD20();
            if (other > d20)
            {
                d20 = other;
            }
        }
        else if (disadvCount > 0)
        {
            var other = RollD20();
            if (other < d20)
            {
                d20 = other;
            }
        }

        var plotFace = CosmerePlotFace.Blank;
        var complicationBonus = 0;
        var opportunities = 0;
        var complications = 0;

        if (raiseStakes)
        {
            plotFace = RollPlotDie();
            switch (plotFace)
            {
                case CosmerePlotFace.Opportunity:
                    {
                        opportunities++;
                        break;
                    }
                case CosmerePlotFace.Complication2:
                    {
                        complications++;
                        complicationBonus = 2;
                        break;
                    }
                case CosmerePlotFace.Complication4:
                    {
                        complications++;
                        complicationBonus = 4;
                        break;
                    }
            }
        }

        if (d20 == 20)
        {
            opportunities++;
        }
        else if (d20 == 1)
        {
            complications++;
        }

        var modifier = sheet.GetModifier(skill);
        var exhausted = sheet.GetExhaustedMagnitude();
        var total = d20 + modifier + complicationBonus - exhausted;
        if (total < 0)
        {
            total = 0;
        }

        return new CosmereTestResult
        {
            Skill = skill,
            Dc = dc,
            D20 = d20,
            Modifier = modifier,
            PlotFace = plotFace,
            ComplicationBonus = complicationBonus,
            ExhaustedPenalty = exhausted,
            Total = total,
            Success = total >= dc,
            Opportunities = opportunities,
            Complications = complications,
            RaiseStakes = raiseStakes,
            Blocked = false
        };
    }

    private static CosmereTestResult BlockedResult(
        CosmereSheet sheet,
        CosmereSkill skill,
        int dc,
        bool raiseStakes,
        string reason
    ) =>
        new()
        {
            Skill = skill,
            Dc = dc,
            D20 = 0,
            Modifier = sheet.GetModifier(skill),
            PlotFace = CosmerePlotFace.Blank,
            ComplicationBonus = 0,
            ExhaustedPenalty = sheet.GetExhaustedMagnitude(),
            Total = 0,
            Success = false,
            Opportunities = 0,
            Complications = 0,
            RaiseStakes = raiseStakes,
            Blocked = true,
            BlockReason = reason
        };

    private static int RollD20() => Utility.Random(20) + 1;

    // Faces: 2 blank, 2 opportunity, 1 complication+2, 1 complication+4
    private static CosmerePlotFace RollPlotDie() =>
        Utility.Random(6) switch
        {
            0 or 1 => CosmerePlotFace.Blank,
            2 or 3 => CosmerePlotFace.Opportunity,
            4 => CosmerePlotFace.Complication2,
            _ => CosmerePlotFace.Complication4
        };
}
