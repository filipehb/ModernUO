using System;
using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereLivingWorldTests
{
    [Fact]
    public void PreyTable_MapsWolvesToRabbits()
    {
        Assert.True(CosmereLivingWorld.IsPredatorType(typeof(DireWolf)));
        Assert.True(CosmereLivingWorld.IsPredatorType(typeof(TimberWolf)));
        Assert.True(CosmereLivingWorld.IsPredatorType(typeof(GreyWolf)));
        Assert.True(CosmereLivingWorld.IsPredatorType(typeof(WhiteWolf)));
        Assert.False(CosmereLivingWorld.IsPredatorType(typeof(Rabbit)));
        Assert.False(CosmereLivingWorld.IsPredatorType(typeof(TsukiWolf)));

        Assert.True(CosmereLivingWorld.IsPreyType(typeof(Rabbit)));
        Assert.True(CosmereLivingWorld.IsPreyType(typeof(JackRabbit)));
        Assert.False(CosmereLivingWorld.IsPreyType(typeof(DireWolf)));
    }

    [Fact]
    public void CountsAsLivingWorldAccess_PlayerYes_StaffOnlyWhenConfigured()
    {
        var player = new PlayerMobile(World.NewMobile);
        var staff = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        staff.DefaultMobileInit();

        var previousStaffWake = CosmereConfig.StaffWakesLivingWorld;

        try
        {
            player.AccessLevel = AccessLevel.Player;
            staff.AccessLevel = AccessLevel.GameMaster;

            CosmereConfig.SetStaffWakesLivingWorldForTests(false);
            Assert.True(CosmereLivingWorld.CountsAsLivingWorldAccess(player));
            Assert.False(CosmereLivingWorld.CountsAsLivingWorldAccess(staff));

            CosmereConfig.SetStaffWakesLivingWorldForTests(true);
            Assert.True(CosmereLivingWorld.CountsAsLivingWorldAccess(staff));
        }
        finally
        {
            CosmereConfig.SetStaffWakesLivingWorldForTests(previousStaffWake);
            player.Delete();
            staff.Delete();
        }
    }

    [Fact]
    public void TryHunt_SetsCombatantOnNearestPrey()
    {
        var map = Map.Felucca;
        var wolf = new DireWolf();
        var nearRabbit = new Rabbit();
        var farRabbit = new Rabbit();

        try
        {
            wolf.MoveToWorld(new Point3D(100, 100, 0), map);
            nearRabbit.MoveToWorld(new Point3D(102, 100, 0), map);
            farRabbit.MoveToWorld(new Point3D(110, 100, 0), map);

            Assert.True(CosmereLivingWorld.TryHunt(wolf, huntRange: 12));
            Assert.Equal(nearRabbit, wolf.Combatant);
            Assert.True(wolf.Warmode);
        }
        finally
        {
            wolf.Delete();
            nearRabbit.Delete();
            farRabbit.Delete();
        }
    }

    [Fact]
    public void IsEligiblePredator_SkipsControlled()
    {
        var wolf = new DireWolf();
        try
        {
            wolf.Controlled = true;
            Assert.False(CosmereLivingWorld.IsEligiblePredator(wolf));
        }
        finally
        {
            wolf.Delete();
        }
    }
}
