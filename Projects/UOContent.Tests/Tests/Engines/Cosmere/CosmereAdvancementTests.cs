using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereAdvancementTests
{
    [Fact]
    public void ComputeHealthGain_L2_IsPlus5()
    {
        Assert.Equal(5, CosmereAdvancement.ComputeHealthGain(2, strength: 3));
    }

    [Fact]
    public void ComputeHealthGain_L6_IsPlus4PlusStrength()
    {
        Assert.Equal(7, CosmereAdvancement.ComputeHealthGain(6, strength: 3));
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(6, 3)]
    [InlineData(11, 4)]
    [InlineData(16, 5)]
    public void GetMaxSkillRank_FollowsTiers(int level, int expected) =>
        Assert.Equal(expected, CosmereAdvancement.GetMaxSkillRank(level));

    [Fact]
    public void ApplyLevelUp_To2_GrantsPendingAndHealthBonus()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            Assert.True(CosmereAdvancement.ApplyLevelUp(pm, 2));

            var sheet = pm.CosmereSheet;
            Assert.Equal(2, sheet.Level);
            Assert.Equal(5, sheet.LevelHealthBonus);
            Assert.Equal(0, sheet.PendingAttrPoints);
            Assert.Equal(2, sheet.PendingSkillRanks);
            Assert.Equal(1, sheet.PendingTalentPicks);
            Assert.Equal(16, sheet.MaxHealth); // 10 + Str1 + 5
            Assert.Equal(16, sheet.Health); // was at max
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void ApplyLevelUp_To6_AddsStrengthToHealthBonus()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.Strength = 2;
            sheet.FillPoolsToMax();
            pm.CosmereSheet = sheet;

            Assert.True(CosmereAdvancement.ApplyLevelUp(pm, 6));

            // L2–5: 5*4=20; L6: 4+Str2=6 → total 26
            Assert.Equal(26, sheet.LevelHealthBonus);
            Assert.Equal(2, sheet.PendingAttrPoints); // L3 + L6
            Assert.Equal(10, sheet.PendingSkillRanks); // 5 levels * 2
            Assert.Equal(6, sheet.PendingTalentPicks); // 5*1 + ancestry at 6
            Assert.Equal(3, sheet.MaxSkillRank);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void ApplyLevelUp_DoesNotHealIfNotAtMax()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.Health = 5;
            pm.CosmereSheet = sheet;

            Assert.True(CosmereAdvancement.ApplyLevelUp(pm, 2));
            Assert.Equal(5, sheet.Health);
            Assert.Equal(16, sheet.MaxHealth);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void SpendAttrAndSkill_ConsumePendingPools()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.PendingAttrPoints = 1;
            sheet.PendingSkillRanks = 1;
            pm.CosmereSheet = sheet;

            Assert.True(CosmereAdvancement.SpendAttr(pm, CosmereAttribute.Strength));
            Assert.Equal(0, sheet.PendingAttrPoints);
            Assert.Equal(2, sheet.Strength);

            Assert.True(CosmereAdvancement.SpendSkill(pm, CosmereSkill.Athletics));
            Assert.Equal(0, sheet.PendingSkillRanks);
            Assert.Equal(1, sheet.GetRank(CosmereSkill.Athletics));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void SpendSkill_FailsAtTierCap()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.PendingSkillRanks = 1;
            sheet.SetRank(CosmereSkill.Lore, 2);
            pm.CosmereSheet = sheet;

            Assert.False(CosmereAdvancement.SpendSkill(pm, CosmereSkill.Lore));
            Assert.Equal(1, sheet.PendingSkillRanks);
            Assert.Equal(2, sheet.GetRank(CosmereSkill.Lore));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void GrantTalent_ConsumesPendingPick()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.PendingTalentPicks = 1;
            pm.CosmereSheet = sheet;

            Assert.True(CosmereAdvancement.GrantTalent(pm, "steelrunner"));
            Assert.Equal(0, sheet.PendingTalentPicks);
            Assert.True(sheet.HasTalent("steelrunner"));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void SetRank_RespectsCapAfterLevelUp()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.SetRank(CosmereSkill.Lore, 2);
            pm.CosmereSheet = sheet;

            CosmereAdvancement.ApplyLevelUp(pm, 6);
            sheet.SetRank(CosmereSkill.Lore, 99);
            Assert.Equal(3, sheet.GetRank(CosmereSkill.Lore));
        }
        finally
        {
            pm.Delete();
        }
    }
}
