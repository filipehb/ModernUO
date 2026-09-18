using System;
using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereSustenanceTests
{
    [Fact]
    public void Sync_Empty_AddsSustenanceExhaustedAndFocusPenalty()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            pm.Hunger = 0;
            pm.Thirst = 10;

            CosmereSustenance.Sync(pm);

            Assert.Equal(1, pm.CosmereSheet.GetExhaustedMagnitude());
            Assert.Equal(1, pm.CosmereSheet.FocusSoftPenalty);
            Assert.Equal(Math.Max(0, 2 + pm.CosmereSheet.Willpower - 1), pm.CosmereSheet.MaxFocus);
            Assert.Contains(
                pm.CosmereSheet.Conditions,
                c => c.Kind == CosmereConditionKind.Exhausted && c.Source == CosmereSustenance.ExhaustedSource
            );
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Sync_Refill_ClearsSustenanceExhausted()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            pm.Hunger = 0;
            pm.Thirst = 0;
            CosmereSustenance.Sync(pm);

            pm.Hunger = 10;
            pm.Thirst = 10;
            CosmereSustenance.Sync(pm);

            Assert.DoesNotContain(
                pm.CosmereSheet.Conditions,
                c => c.Source == CosmereSustenance.ExhaustedSource
            );
            Assert.Equal(0, pm.CosmereSheet.FocusSoftPenalty);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Sync_Disabled_ClearsEffects()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        var previous = CosmereConfig.HungerEffectsEnabled;

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            pm.Hunger = 0;
            CosmereSustenance.Sync(pm);
            Assert.Equal(1, pm.CosmereSheet.FocusSoftPenalty);

            CosmereConfig.SetHungerEffectsEnabledForTests(false);
            CosmereSustenance.Sync(pm);

            Assert.Equal(0, pm.CosmereSheet.FocusSoftPenalty);
            Assert.DoesNotContain(
                pm.CosmereSheet.Conditions,
                c => c.Source == CosmereSustenance.ExhaustedSource
            );
        }
        finally
        {
            CosmereConfig.SetHungerEffectsEnabledForTests(previous);
            pm.Delete();
        }
    }
}
