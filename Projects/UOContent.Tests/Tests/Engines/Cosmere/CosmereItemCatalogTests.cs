using Server.Engines.Cosmere;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereItemCatalogTests
{
    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(2, 0, 1)]
    [InlineData(3, 4, 0)]
    [InlineData(4, 4, 0)]
    [InlineData(5, 8, 0)]
    [InlineData(7, 6, 0)]
    [InlineData(9, 10, 0)]
    public void UnarmedForStrength_MatchesBands(int strength, int expectedSides, int expectedFlat)
    {
        var profile = CosmereItemCatalog.UnarmedForStrength(strength);
        Assert.Equal(CosmereWeaponCatalogId.Unarmed, profile.CatalogId);
        Assert.Equal(expectedSides, profile.DieSides);
        Assert.Equal(expectedFlat, profile.FlatDamage);
        Assert.Equal(CosmereDamageType.Impact, profile.DamageType);
        Assert.Equal(CosmereSkill.Athletics, profile.AttackSkill);
    }

    [Fact]
    public void ResolveWeapon_DaggerIsKnife_KeenOffhand()
    {
        var item = new Dagger();
        try
        {
            var profile = CosmereItemCatalog.ResolveWeapon(item);
            Assert.Equal(CosmereWeaponCatalogId.Knife, profile.CatalogId);
            Assert.Equal(4, profile.DieSides);
            Assert.Equal(CosmereDamageType.Keen, profile.DamageType);
            Assert.True(profile.Has(CosmereWeaponTraits.Offhand));
            Assert.True(profile.Has(CosmereWeaponTraits.Discreet));
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void ResolveWeapon_ClubIsHammer_TwoHanded()
    {
        var item = new Club();
        try
        {
            var profile = CosmereItemCatalog.ResolveWeapon(item);
            Assert.Equal(CosmereWeaponCatalogId.Hammer, profile.CatalogId);
            Assert.Equal(10, profile.DieSides);
            Assert.Equal(CosmereDamageType.Impact, profile.DamageType);
            Assert.True(profile.Has(CosmereWeaponTraits.TwoHanded));
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void ResolveWeapon_KryssIsGlassDagger()
    {
        var item = new Kryss();
        try
        {
            var profile = CosmereItemCatalog.ResolveWeapon(item);
            Assert.Equal(CosmereWeaponCatalogId.GlassDagger, profile.CatalogId);
            Assert.True(profile.Has(CosmereWeaponTraits.Fragile));
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void ResolveArmor_PlateChestIsBreastplate()
    {
        var item = new PlateChest();
        try
        {
            var profile = CosmereItemCatalog.ResolveArmorItem(item);
            Assert.Equal(CosmereArmorCatalogId.Breastplate, profile.CatalogId);
            Assert.Equal(2, profile.Deflect);
            Assert.Equal(3, profile.Cumbersome);
            Assert.False(profile.Presentable);
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void ResolveArmor_LeatherChestIsLeather()
    {
        var item = new LeatherChest();
        try
        {
            var profile = CosmereItemCatalog.ResolveArmorItem(item);
            Assert.Equal(CosmereArmorCatalogId.Leather, profile.CatalogId);
            Assert.Equal(1, profile.Deflect);
            Assert.False(profile.Presentable);
        }
        finally
        {
            item.Delete();
        }
    }
}
