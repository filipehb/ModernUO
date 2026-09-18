using Server.Engines.Cosmere;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereInjuryTests
{
    [Fact]
    public void ResolveInjury_DeathBand_SetsDead()
    {
        // Need total <= -6: with 2 existing injuries, d20=1 → 1 - 10 = -9 → Death.
        using var rng = new PredictableRandom(0);
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Injuries.Add(new CosmereInjuryRecord { Id = 1, Severity = CosmereInjurySeverity.Shallow });
        sheet.Injuries.Add(new CosmereInjuryRecord { Id = 2, Severity = CosmereInjurySeverity.Shallow });

        var result = CosmereInjury.ResolveInjury(sheet);

        Assert.Equal(CosmereInjurySeverity.Death, result.Severity);
        Assert.True(sheet.Dead);
        Assert.Equal(1 - 10, result.RollTotal);
    }
}
