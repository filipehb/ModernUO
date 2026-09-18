using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereGoalsTests
{
    [Fact]
    public void Snap_AssignsTrainYourPowerGoal()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            Assert.True(CosmereAllomancy.TrySnap(pm, CosmereAllomanticMetal.Steel));

            var goal = pm.CosmereSheet.FindGoal(CosmereGoals.TrainYourPowerId);
            Assert.NotNull(goal);
            Assert.Equal(CosmereGoalStatus.Active, goal.Status);
            Assert.Equal(CosmereGoalRewardKind.UnlockFullAllomancy, goal.RewardKind);
            Assert.Equal(CosmerePowerTier.Nascent, pm.CosmereSheet.PowerTier);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void CompleteTrainYourPower_UnlocksFullAllomancy()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereAllomancy.TrySnap(pm, CosmereAllomanticMetal.Tin);

            Assert.True(CosmereGoals.Complete(pm, CosmereGoals.TrainYourPowerId));
            Assert.Equal(CosmerePowerTier.Full, pm.CosmereSheet.PowerTier);
            Assert.Equal(
                CosmereGoalStatus.Completed,
                pm.CosmereSheet.FindGoal(CosmereGoals.TrainYourPowerId)!.Status
            );
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void UnlockPower_CompletesActiveTrainYourPower()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereAllomancy.TrySnap(pm, CosmereAllomanticMetal.Bronze);

            Assert.True(CosmereAllomancy.TryUnlockPower(pm));
            Assert.Equal(CosmerePowerTier.Full, pm.CosmereSheet.PowerTier);
            Assert.Equal(
                CosmereGoalStatus.Completed,
                pm.CosmereSheet.FindGoal(CosmereGoals.TrainYourPowerId)!.Status
            );
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Assign_IsIdempotent()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereGoals.Assign(pm, "quest-a", "Quest A", CosmereGoalRewardKind.None);
            CosmereGoals.Assign(pm, "quest-a", "Quest A again", CosmereGoalRewardKind.GrantTalent, "x");

            Assert.Single(pm.CosmereSheet.Goals);
            Assert.Equal("Quest A", pm.CosmereSheet.Goals[0].Title);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void GrantTalentReward_DoesNotConsumePendingPicks()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.PendingTalentPicks = 2;
            pm.CosmereSheet = sheet;

            CosmereGoals.Assign(pm, "t", "T", CosmereGoalRewardKind.GrantTalent, "pathfinder");
            CosmereGoals.Complete(pm, "t");

            Assert.Equal(2, sheet.PendingTalentPicks);
            Assert.True(sheet.HasTalent("pathfinder"));
        }
        finally
        {
            pm.Delete();
        }
    }
}
