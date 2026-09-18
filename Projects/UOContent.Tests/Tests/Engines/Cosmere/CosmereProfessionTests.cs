using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereProfessionTests
{
    [Fact]
    public void Assign_DoesNotTouchTalentPicks()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.PendingTalentPicks = 3;
            sheet.PendingAttrPoints = 2;
            sheet.PendingSkillRanks = 4;
            pm.CosmereSheet = sheet;

            Assert.True(CosmereProfession.Assign(pm, CosmereProfessionId.Artisan));
            Assert.Equal(CosmereProfessionId.Artisan, sheet.ProfessionId);
            Assert.Equal(3, sheet.PendingTalentPicks);
            Assert.Equal(2, sheet.PendingAttrPoints);
            Assert.Equal(4, sheet.PendingSkillRanks);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void AddXp_RanksUpWithOverflow()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereProfession.Assign(pm, CosmereProfessionId.Producer);

            Assert.True(CosmereProfession.AddXp(pm, 130)); // need 100 → rank 1, overflow 30
            Assert.Equal(1, pm.CosmereSheet.ProfessionRank);
            Assert.Equal(30, pm.CosmereSheet.ProfessionXp);
            Assert.Equal(1, CosmereProfession.GetRecipeTier(pm.CosmereSheet.ProfessionRank));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void AddXp_CanSkipMultipleRanks()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereProfession.Assign(pm, CosmereProfessionId.Merchant);

            // 100 + 250 + 50 = 400 → rank 2 with 50 overflow
            Assert.True(CosmereProfession.AddXp(pm, 400));
            Assert.Equal(2, pm.CosmereSheet.ProfessionRank);
            Assert.Equal(50, pm.CosmereSheet.ProfessionXp);
            Assert.Equal(-0.05, CosmereProfession.GetTimeMod(2));
            Assert.Equal(0.02, CosmereProfession.GetSaleMod(2));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void ApplyLevelUp_DoesNotGrantProfessionXp()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereProfession.Assign(pm, CosmereProfessionId.Artisan);
            CosmereProfession.AddXp(pm, 40);

            Assert.True(CosmereAdvancement.ApplyLevelUp(pm, 2));
            Assert.Equal(40, pm.CosmereSheet.ProfessionXp);
            Assert.Equal(0, pm.CosmereSheet.ProfessionRank);
            Assert.Equal(CosmereProfessionId.Artisan, pm.CosmereSheet.ProfessionId);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void TryProfessionAction_WrongProfession_GrantsNoXp()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereProfession.Assign(pm, CosmereProfessionId.Merchant);

            Assert.False(CosmereProfession.TryProfessionAction(pm, CosmereProfessionId.Artisan, "Craft"));
            Assert.Equal(0, pm.CosmereSheet.ProfessionXp);
            Assert.Equal(0, pm.CosmereSheet.ProfessionRank);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void TryProfessionAction_Matching_AwardsActionXp()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereProfession.Assign(pm, CosmereProfessionId.Producer);

            Assert.True(CosmereProfession.TryProfessionAction(pm, CosmereProfessionId.Producer, "Gather"));
            Assert.Equal(CosmereProfession.GetActionXp(CosmereProfessionId.Producer), pm.CosmereSheet.ProfessionXp);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void SetRank_ClampsAndResetsXp()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            CosmereProfession.Assign(pm, CosmereProfessionId.Artisan);
            CosmereProfession.AddXp(pm, 50);

            Assert.True(CosmereProfession.SetRank(pm, 99));
            Assert.Equal(5, pm.CosmereSheet.ProfessionRank);
            Assert.Equal(0, pm.CosmereSheet.ProfessionXp);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void ProfessionDisabled_BlocksAssignAndXp()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        var previous = true;

        try
        {
            previous = CosmereConfig.ProfessionEnabled;
            CosmereConfig.SetProfessionEnabledForTests(false);

            pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
            Assert.False(CosmereProfession.Assign(pm, CosmereProfessionId.Artisan));
            Assert.Equal(CosmereProfessionId.None, pm.CosmereSheet.ProfessionId);

            pm.CosmereSheet.ProfessionId = CosmereProfessionId.Artisan;
            Assert.False(CosmereProfession.AddXp(pm, 50));
            Assert.Equal(0, pm.CosmereSheet.ProfessionXp);
        }
        finally
        {
            CosmereConfig.SetProfessionEnabledForTests(previous);
            pm.Delete();
        }
    }

    [Theory]
    [InlineData("artisan", CosmereProfessionId.Artisan)]
    [InlineData("Producer", CosmereProfessionId.Producer)]
    [InlineData("MERCHANT", CosmereProfessionId.Merchant)]
    [InlineData("none", CosmereProfessionId.None)]
    public void TryParse_AcceptsKnownIds(string text, CosmereProfessionId expected)
    {
        Assert.True(CosmereProfession.TryParse(text, out var id));
        Assert.Equal(expected, id);
    }
}
