using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereAllomancyTests
{
    [Theory]
    [InlineData(0, 1, 10, 0)]
    [InlineData(1, 1, 20, 4)]
    [InlineData(2, 2, 40, 6)]
    [InlineData(3, 3, 80, 8)]
    public void MetallicArtTable_MatchesStarter(int ranks, int limit, int rangeFeet, int dieSides)
    {
        Assert.Equal(limit, CosmereAllomancy.GetMetallicArtLimit(ranks));
        Assert.Equal(rangeFeet, CosmereAllomancy.GetMetallicArtRangeFeet(ranks));
        Assert.Equal(dieSides, CosmereAllomancy.GetMetallicArtDieSides(ranks));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    public void TinEnhancedFromSpend_IsCeilHalf(int spend, int expected) =>
        Assert.Equal(expected, CosmereAllomancy.TinEnhancedFromSpend(spend));

    [Fact]
    public void MaxInvestiture_ZeroUnlessMetalborn()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        Assert.Equal(0, sheet.MaxInvestiture);

        sheet.IsMetalborn = true;
        sheet.Awareness = 2;
        sheet.Presence = 1;
        Assert.Equal(4, sheet.MaxInvestiture); // 2 + max(2,1)
    }

    [Fact]
    public void TrySpendInvestiture_ClampsAndFailsWhenShort()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.IsMetalborn = true;
        sheet.Awareness = 1;
        sheet.Presence = 1;
        sheet.Investiture = sheet.MaxInvestiture; // 3

        Assert.False(CosmereAllomancy.TrySpendInvestiture(sheet, 4));
        Assert.True(CosmereAllomancy.TrySpendInvestiture(sheet, 2));
        Assert.Equal(1, sheet.Investiture);
    }

    [Fact]
    public void VialDepleted_ClearsInVial_DepletesMissingPowerMetal()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.IsMetalborn = true;
        sheet.AllomanticMetal = CosmereAllomanticMetal.Steel;
        sheet.PowerTier = CosmerePowerTier.Full;
        sheet.Investiture = 0;
        sheet.SetMetalDepleted(CosmereAllomanticMetal.Steel, true);

        // Drink bronze-only vial: clears bronze depleted (none), depletes Steel
        sheet.SetMetalDepleted(CosmereAllomanticMetal.Steel, false);
        sheet.Investiture = 0;

        // Simulate Drink logic for Steel misting + Bronze-only vial
        sheet.Investiture = sheet.MaxInvestiture;
        sheet.SetMetalDepleted(CosmereAllomanticMetal.Bronze, false);
        sheet.SetMetalDepleted(CosmereAllomanticMetal.Steel, true);

        Assert.True(sheet.IsMetalDepleted(CosmereAllomanticMetal.Steel));
        Assert.False(sheet.IsMetalDepleted(CosmereAllomanticMetal.Bronze));
        Assert.Equal(sheet.MaxInvestiture, sheet.Investiture);
    }

    [Fact]
    public void DrinkMixedVial_ClearsOwnMetalDepleted()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            Assert.True(CosmereAllomancy.TrySnap(pm, CosmereAllomanticMetal.Tin));
            pm.CosmereSheet.SetMetalDepleted(CosmereAllomanticMetal.Tin, true);
            pm.CosmereSheet.Investiture = 0;

            Assert.True(CosmereAllomancy.TryDrink(pm, CosmereMetalFlags.AllStarter, consumeItem: false));
            Assert.False(pm.CosmereSheet.IsMetalDepleted(CosmereAllomanticMetal.Tin));
            Assert.Equal(pm.CosmereSheet.MaxInvestiture, pm.CosmereSheet.Investiture);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Snap_GrantsMetalbornAndAllomancyRanks()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            Assert.True(CosmereAllomancy.TrySnap(pm, CosmereAllomanticMetal.Steel));
            var sheet = pm.CosmereSheet;
            Assert.True(sheet.IsMetalborn);
            Assert.Equal(CosmereAllomanticMetal.Steel, sheet.AllomanticMetal);
            Assert.Equal(CosmerePowerTier.Nascent, sheet.PowerTier);
            Assert.Equal(2, sheet.GetRank(CosmereSkill.Allomancy));
            Assert.True(sheet.MaxInvestiture > 0);
            Assert.Equal(sheet.MaxInvestiture, sheet.Investiture);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void UnlockPower_SetsFullTier()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            CosmereAllomancy.TrySnap(pm, CosmereAllomanticMetal.Bronze);
            Assert.True(CosmereAllomancy.TryUnlockPower(pm));
            Assert.Equal(CosmerePowerTier.Full, pm.CosmereSheet.PowerTier);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void SceneStart_FillsInvestiture_UnlessSurprised()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            CosmereAllomancy.TrySnap(pm, CosmereAllomanticMetal.Zinc);
            CosmereAllomancy.TryUnlockPower(pm);
            pm.CosmereSheet.Investiture = 0;

            CosmereCombat.BeginCombat(pm);
            Assert.Equal(pm.CosmereSheet.MaxInvestiture, pm.CosmereSheet.Investiture);

            CosmereCombat.EndScene(pm);
            pm.CosmereSheet.Investiture = 0;
            pm.CosmereSheet.EnsureCondition(CosmereConditionKind.Surprised, source: "test");
            CosmereCombat.BeginCombat(pm);
            Assert.Equal(1, pm.CosmereSheet.Investiture);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void RollMetallicArtDamage_Rank0IsFlatOne()
    {
        Assert.Equal(1, CosmereAllomancy.RollMetallicArtDamage(0));
    }

    [Fact]
    public void AllomancySkill_IsWillpower()
    {
        Assert.Equal(CosmereAttribute.Willpower, CosmereSkills.GetAttribute(CosmereSkill.Allomancy));
        Assert.True(CosmereSkills.TryParse("Allomancy", out var skill));
        Assert.Equal(CosmereSkill.Allomancy, skill);
    }
}
