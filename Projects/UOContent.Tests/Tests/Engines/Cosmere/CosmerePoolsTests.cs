using Server.Engines.Cosmere;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmerePoolsTests
{
    [Fact]
    public void DamageHealth_AtZero_AppliesUnconsciousAndInjury()
    {
        using var rng = new PredictableRandom(10); // d20=11 → Shallow (6-15)
        var sheet = CosmereSheet.CreateMundaneStarter();

        CosmerePools.DamageHealth(sheet, sheet.Health);

        Assert.Equal(0, sheet.Health);
        Assert.True(sheet.HasCondition(CosmereConditionKind.Unconscious));
        Assert.Equal(1, sheet.ActiveInjuryCount);
        Assert.False(sheet.Dead);
    }

    [Fact]
    public void DamageHealth_WhileAtZero_AddsAnotherInjury()
    {
        using var rng = new PredictableRandom(10);
        var sheet = CosmereSheet.CreateMundaneStarter();
        CosmerePools.DamageHealth(sheet, sheet.Health);
        Assert.Equal(1, sheet.ActiveInjuryCount);

        CosmerePools.DamageHealth(sheet, 1);
        Assert.Equal(2, sheet.ActiveInjuryCount);
    }

    [Fact]
    public void HealHealth_ClearsUnconscious_DoesNotClearDead()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Health = 0;
        sheet.SetFlagCondition(CosmereConditionKind.Unconscious, true);
        sheet.Dead = true;

        CosmerePools.HealHealth(sheet, 5);

        Assert.Equal(5, sheet.Health);
        Assert.False(sheet.HasCondition(CosmereConditionKind.Unconscious));
        Assert.True(sheet.Dead);
    }

    [Fact]
    public void SpendFocus_FailsWhenInsufficient()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        Assert.False(CosmerePools.SpendFocus(sheet, sheet.Focus + 1));
        Assert.True(CosmerePools.SpendFocus(sheet, 1));
        Assert.Equal(2, sheet.Focus);
    }
}
