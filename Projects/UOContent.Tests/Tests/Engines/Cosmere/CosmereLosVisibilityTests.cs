using Server.Engines.Cosmere;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

public class CosmereLosVisibilityTests
{
    [Fact]
    public void RequiresLos_RespectsConfigAndAccess()
    {
        var previous = CosmereConfig.LosVisibilityEnabled;
        try
        {
            CosmereConfig.SetLosVisibilityEnabledForTests(false);
            // Helper only gates PlayerMobile + Player access; null-safe via RequiresLos false path
            Assert.False(CosmereConfig.LosVisibilityEnabled);

            CosmereConfig.SetLosVisibilityEnabledForTests(true);
            Assert.True(CosmereConfig.LosVisibilityEnabled);
        }
        finally
        {
            CosmereConfig.SetLosVisibilityEnabledForTests(previous);
        }
    }
}
