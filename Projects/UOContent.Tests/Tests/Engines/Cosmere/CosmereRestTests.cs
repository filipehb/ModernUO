using Server.Engines.Cosmere;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereRestTests
{
    [Fact]
    public void ShortRest_AllocatesRecoveryDie()
    {
        using var rng = new PredictableRandom(0); // Random(6)+1 = 1 for Willpower 1
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Health = 5;
        sheet.Focus = 0;

        var result = CosmereRest.ShortRest(sheet, healthShare: 1);

        Assert.True(result.Success);
        Assert.Equal(1, result.DieRoll);
        Assert.Equal(6, sheet.Health);
        Assert.Equal(0, sheet.Focus);
    }

    [Fact]
    public void LongRest_FillsPools_ReducesExhausted_ClearsFleshWound()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Health = 1;
        sheet.Focus = 0;
        sheet.AddExhausted(2);
        sheet.Injuries.Add(
            new CosmereInjuryRecord
            {
                Id = 1,
                Severity = CosmereInjurySeverity.FleshWound,
                Cleared = false
            }
        );
        sheet.Injuries.Add(
            new CosmereInjuryRecord
            {
                Id = 2,
                Severity = CosmereInjurySeverity.Shallow,
                Cleared = false
            }
        );

        CosmereRest.LongRest(sheet);

        Assert.Equal(sheet.MaxHealth, sheet.Health);
        Assert.Equal(sheet.MaxFocus, sheet.Focus);
        Assert.Equal(1, sheet.GetExhaustedMagnitude());
        Assert.True(sheet.Injuries[0].Cleared);
        Assert.False(sheet.Injuries[1].Cleared);
    }
}
