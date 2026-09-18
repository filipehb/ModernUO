using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmerePossessTests
{
    [Fact]
    public void TryValidateTarget_RejectsPlayer()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            Assert.False(CosmerePossess.TryValidateTarget(pm, out _, out var reason));
            Assert.Contains("player", reason, System.StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void TryValidateTarget_RejectsControlledPet()
    {
        var wolf = new DireWolf();
        try
        {
            wolf.Controlled = true;
            Assert.False(CosmerePossess.TryValidateTarget(wolf, out _, out var reason));
            Assert.Contains("pet", reason, System.StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            wolf.Delete();
        }
    }

    [Fact]
    public void TryValidateTarget_RejectsSummoned()
    {
        var wolf = new DireWolf();
        try
        {
            wolf.Summoned = true;
            Assert.False(CosmerePossess.TryValidateTarget(wolf, out _, out var reason));
            Assert.Contains("pet", reason, System.StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            wolf.Delete();
        }
    }

    [Fact]
    public void TryValidateTarget_RejectsVendor()
    {
        var vendor = new Weaver();
        try
        {
            vendor.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);
            Assert.False(CosmerePossess.TryValidateTarget(vendor, out _, out var reason));
            Assert.Contains("vendor", reason, System.StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            vendor.Delete();
        }
    }

    [Fact]
    public void TryValidateTarget_AcceptsWildCreatureOnMap()
    {
        var wolf = new DireWolf();
        try
        {
            wolf.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);
            Assert.True(CosmerePossess.TryValidateTarget(wolf, out var creature, out _));
            Assert.Same(wolf, creature);
        }
        finally
        {
            wolf.Delete();
        }
    }
}
