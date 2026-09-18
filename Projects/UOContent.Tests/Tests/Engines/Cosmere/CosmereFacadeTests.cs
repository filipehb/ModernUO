using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereFacadeTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 28)]
    [InlineData(5, 100)]
    [InlineData(6, 100)]
    public void ToUoStat_UsesPhase1Scale(int attr, int expected) =>
        Assert.Equal(expected, CosmereFacade.ToUoStat(attr));

    [Fact]
    public void Apply_PushesMappedStatsSkillsAndPools()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();

        try
        {
            var sheet = CosmereSheet.CreateMundaneStarter();
            sheet.Strength = 2;
            sheet.Speed = 3;
            sheet.Intellect = 1;
            sheet.SetRank(CosmereSkill.Athletics, 2);
            sheet.SetRank(CosmereSkill.Stealth, 1);
            sheet.Health = 9;
            sheet.Focus = 2;
            pm.CosmereSheet = sheet;

            CosmereFacade.Apply(pm);

            Assert.Equal(CosmereFacade.ToUoStat(2), pm.RawStr);
            Assert.Equal(CosmereFacade.ToUoStat(3), pm.RawDex);
            Assert.Equal(CosmereFacade.ToUoStat(1), pm.RawInt);

            Assert.Equal(40.0, pm.Skills.Wrestling.Base);
            Assert.Equal(20.0, pm.Skills.Hiding.Base);
            Assert.Equal(SkillLock.Locked, pm.Skills.Wrestling.Lock);
            Assert.Equal(SkillLock.Locked, pm.Skills.Hiding.Lock);
            Assert.Equal(0.0, pm.Skills.Swords.Base);
            Assert.Equal(SkillLock.Locked, pm.Skills.Swords.Lock);

            Assert.Equal(12, pm.HitsMax); // MaxHealth = 10 + Strength 2
            Assert.Equal(3, pm.StamMax);  // MaxFocus = 2 + Willpower 1
            Assert.Equal(0, pm.ManaMax);
            Assert.Equal(9, pm.Hits);
            Assert.Equal(2, pm.Stam);
            Assert.Equal(0, pm.Mana);
        }
        finally
        {
            pm.Delete();
        }
    }
}
