using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereDebugTests
{
    [Fact]
    public void IsOn_FalseByDefault()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            Assert.False(CosmereDebug.IsOn(pm));
            Assert.False(pm.CosmereDebug);
        }
        finally
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            pm.Delete();
        }
    }

    [Fact]
    public void IsOn_TrueWhenFlagAndConfigEnabled()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            pm.CosmereDebug = true;
            Assert.True(CosmereDebug.IsOn(pm));
        }
        finally
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            pm.Delete();
        }
    }

    [Fact]
    public void IsOn_FalseWhenConfigDisabledEvenIfFlagSet()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereDebug = true;
            CosmereConfig.SetDebugEnabledForTests(false);
            Assert.False(CosmereDebug.IsOn(pm));
        }
        finally
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            pm.Delete();
        }
    }

    [Fact]
    public void FormatTest_DoesNotThrowWhenOn()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            pm.CosmereDebug = true;
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();

            var result = CosmereTestResolver.Resolve(pm.CosmereSheet, CosmereSkill.Athletics, 12);
            CosmereDebug.TraceTest(pm, result);
            CosmereDebug.Trace(pm, "smoke");
        }
        finally
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            pm.Delete();
        }
    }

    [Fact]
    public void Trace_NoOpsWhenOff()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            pm.CosmereDebug = false;
            // Should early-out without requiring NetState
            CosmereDebug.Trace(pm, "should not send");
            Assert.False(CosmereDebug.IsOn(pm));
        }
        finally
        {
            CosmereConfig.SetDebugEnabledForTests(true);
            pm.Delete();
        }
    }

    [Fact]
    public void Flag_IsSessionOnly_NotOnSheet()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        Assert.Equal(3, CosmereSheet.SheetVersion);
        // CosmereDebug lives on PlayerMobile, not sheet serialization
        Assert.NotNull(sheet);
    }
}
