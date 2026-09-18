using Server.Engines.Cosmere;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereTestResolverTests
{
    [Fact]
    public void Resolve_Success_WhenTotalMeetsDc()
    {
        // FixedRandom(10) → Utility.Random(20) = 10 → d20 = 11; modifier 1 → total 12.
        using var rng = new PredictableRandom(10);
        var sheet = CosmereSheet.CreateMundaneStarter();

        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 12);

        Assert.Equal(11, result.D20);
        Assert.Equal(1, result.Modifier);
        Assert.Equal(12, result.Total);
        Assert.True(result.Success);
        Assert.Equal(0, result.Opportunities);
        Assert.Equal(0, result.Complications);
        Assert.False(result.RaiseStakes);
    }

    [Fact]
    public void Resolve_Failure_WhenTotalBelowDc()
    {
        using var rng = new PredictableRandom(10);
        var sheet = CosmereSheet.CreateMundaneStarter();

        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 13);

        Assert.Equal(12, result.Total);
        Assert.False(result.Success);
    }

    [Fact]
    public void Resolve_Natural20_GrantsOpportunity()
    {
        // FixedRandom(19) → Random(20) = 19 → d20 = 20.
        using var rng = new PredictableRandom(19);
        var sheet = CosmereSheet.CreateMundaneStarter();

        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 99);

        Assert.Equal(20, result.D20);
        Assert.Equal(1, result.Opportunities);
        Assert.Equal(0, result.Complications);
        Assert.False(result.Success);
    }

    [Fact]
    public void Resolve_Natural1_GrantsComplication()
    {
        using var rng = new PredictableRandom(0);
        var sheet = CosmereSheet.CreateMundaneStarter();

        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 1);

        Assert.Equal(1, result.D20);
        Assert.Equal(0, result.Opportunities);
        Assert.Equal(1, result.Complications);
        // d20 1 + mod 1 = 2 ≥ DC 1
        Assert.True(result.Success);
    }

    [Fact]
    public void Resolve_RaiseStakes_OpportunityFace_StacksWithNatural()
    {
        // Random(20)=19 → d20 20; Random(6)=19 clamped to 5 → Complication4.
        // Use value 2: d20=3, plot face Opportunity (faces 2,3).
        using var rng = new PredictableRandom(2);
        var sheet = CosmereSheet.CreateMundaneStarter();

        var result = CosmereTestResolver.Resolve(
            sheet,
            CosmereSkill.Athletics,
            dc: 1,
            raiseStakes: true
        );

        Assert.True(result.RaiseStakes);
        Assert.Equal(CosmerePlotFace.Opportunity, result.PlotFace);
        Assert.Equal(0, result.ComplicationBonus);
        Assert.Equal(1, result.Opportunities);
        Assert.Equal(0, result.Complications);
    }

    [Fact]
    public void Resolve_RaiseStakes_Complication2_AddsBonus()
    {
        // Random(6) with fixed 4 → Complication2.
        using var rng = new PredictableRandom(4);
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Strength = 2;
        sheet.SetRank(CosmereSkill.Athletics, 1);

        var result = CosmereTestResolver.Resolve(
            sheet,
            CosmereSkill.Athletics,
            dc: 10,
            raiseStakes: true
        );

        Assert.Equal(CosmerePlotFace.Complication2, result.PlotFace);
        Assert.Equal(2, result.ComplicationBonus);
        Assert.Equal(1, result.Complications);
        // d20 = 5, mod = 3, bonus = 2 → total 10
        Assert.Equal(5, result.D20);
        Assert.Equal(3, result.Modifier);
        Assert.Equal(10, result.Total);
        Assert.True(result.Success);
    }

    [Fact]
    public void Resolve_IncludesSkillModifier()
    {
        using var rng = new PredictableRandom(5);
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Strength = 4;
        sheet.SetRank(CosmereSkill.Athletics, 2);

        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 1);

        Assert.Equal(6, result.Modifier);
        Assert.Equal(6 + 6, result.Total); // d20 = Random(20)+1 = 6
    }

    [Fact]
    public void Resolve_AppliesExhaustedPenalty()
    {
        using var rng = new PredictableRandom(10); // d20=11, mod=1 → raw 12 - 2 exh = 10
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.AddExhausted(2);

        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 10);

        Assert.Equal(2, result.ExhaustedPenalty);
        Assert.Equal(10, result.Total);
        Assert.True(result.Success);
        Assert.False(result.Blocked);
    }

    [Fact]
    public void Resolve_Slowed_AddsDisadvantageOnSpeedSkills()
    {
        // Two d20 rolls under disadv: first 15→16, second 5→6 → keeps 6.
        using var rng = new PredictableRandom(15, 5);
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.EnsureCondition(CosmereConditionKind.Slowed, source: "cumbersome");

        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Agility, dc: 1);

        Assert.Equal(6, result.D20);
        Assert.False(result.Blocked);
    }

    [Fact]
    public void Resolve_Slowed_DoesNotAffectStrengthSkills()
    {
        using var rng = new PredictableRandom(15, 5);
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.EnsureCondition(CosmereConditionKind.Slowed, source: "cumbersome");

        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 1);

        // Only one d20 when no disadv: 15→16 (second value unused)
        Assert.Equal(16, result.D20);
    }

    [Fact]
    public void Resolve_BlockedWhenUnconsciousOrDead()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.SetFlagCondition(CosmereConditionKind.Unconscious, true);

        var blocked = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 1);
        Assert.True(blocked.Blocked);
        Assert.Equal("Unconscious", blocked.BlockReason);

        sheet.ClearCondition(CosmereConditionKind.Unconscious);
        sheet.Dead = true;
        var dead = CosmereTestResolver.Resolve(sheet, CosmereSkill.Athletics, dc: 1);
        Assert.True(dead.Blocked);
        Assert.Equal("Dead", dead.BlockReason);
    }
}
