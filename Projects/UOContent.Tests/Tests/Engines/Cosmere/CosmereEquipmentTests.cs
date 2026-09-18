using Server.Engines.Cosmere;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereEquipmentTests
{
    [Theory]
    [InlineData(CosmereDamageType.Impact, 5, 2, 3)]
    [InlineData(CosmereDamageType.Keen, 5, 2, 3)]
    [InlineData(CosmereDamageType.Energy, 5, 2, 3)]
    [InlineData(CosmereDamageType.Spirit, 5, 2, 5)]
    [InlineData(CosmereDamageType.Vital, 5, 2, 5)]
    public void ApplyDeflect_ByDamageType(CosmereDamageType type, int amount, int deflect, int expected)
    {
        Assert.Equal(expected, CosmereEquipment.ApplyDeflect(amount, type, deflect));
    }

    [Fact]
    public void ApplyDeflect_ClampsAtZero()
    {
        Assert.Equal(0, CosmereEquipment.ApplyDeflect(1, CosmereDamageType.Impact, 3));
    }

    [Fact]
    public void GetOffhandStrikeFocusCost_OffhandIs1_Else2()
    {
        var dagger = new Dagger();
        var club = new Club();
        try
        {
            Assert.Equal(1, CosmereEquipment.GetOffhandStrikeFocusCost(dagger));
            Assert.Equal(2, CosmereEquipment.GetOffhandStrikeFocusCost(club));
            Assert.Equal(2, CosmereEquipment.GetOffhandStrikeFocusCost(null)); // unarmed
        }
        finally
        {
            dagger.Delete();
            club.Delete();
        }
    }

    [Fact]
    public void DamageHealth_AppliesDeflectBeforeLoss()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        var before = sheet.Health;

        CosmerePools.DamageHealth(sheet, amount: 5, CosmereDamageType.Impact, deflect: 2);

        Assert.Equal(before - 3, sheet.Health);
    }

    [Fact]
    public void DamageHealth_SpiritIgnoresDeflect()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        var before = sheet.Health;

        CosmerePools.DamageHealth(sheet, amount: 5, CosmereDamageType.Spirit, deflect: 2);

        Assert.Equal(before - 5, sheet.Health);
    }
}
