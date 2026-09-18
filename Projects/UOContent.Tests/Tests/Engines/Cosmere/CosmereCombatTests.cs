using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereCombatTests
{
    [Fact]
    public void IsLeavingMeleeReach_DetectsAdjacentToNot()
    {
        var observer = new Point3D(100, 100, 0);
        var from = new Point3D(101, 100, 0);
        var to = new Point3D(103, 100, 0);

        Assert.True(CosmereCombat.IsLeavingMeleeReach(from, to, observer));
        Assert.False(CosmereCombat.IsLeavingMeleeReach(from, new Point3D(100, 101, 0), observer));
    }

    [Fact]
    public void BeginCombat_SetsReactionAndScene()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            CosmereCombat.BeginCombat(pm);
            var state = pm.CosmereCombat;

            Assert.NotNull(state);
            Assert.True(state.InCombat);
            Assert.True(state.ReactionAvailable);
            Assert.False(state.RecoverUsedThisScene);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void TrySceneRecover_OnlyOncePerScene()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.EnsureCosmereSheet();
            pm.CosmereSheet.Health = 5;
            CosmereCombat.BeginCombat(pm);

            Assert.True(CosmereCombat.TrySceneRecover(pm, healthShare: 0));
            Assert.True(pm.CosmereCombat.RecoverUsedThisScene);
            Assert.False(CosmereCombat.TrySceneRecover(pm, healthShare: 0));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void CanReact_FalseWhenSurprised()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            CosmereCombat.BeginCombat(pm);
            pm.EnsureCosmereSheet();
            pm.CosmereSheet.EnsureCondition(CosmereConditionKind.Surprised);

            Assert.False(CosmereCombat.CanReact(pm, pm.CosmereCombat));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void EndScene_ClearsFlags()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            CosmereCombat.BeginCombat(pm);
            pm.CosmereCombat.DodgeArmed = true;
            CosmereCombat.EndScene(pm);

            Assert.False(pm.CosmereCombat.InCombat);
            Assert.False(pm.CosmereCombat.DodgeArmed);
            Assert.False(pm.CosmereCombat.ReactionAvailable);
        }
        finally
        {
            pm.Delete();
        }
    }
}
