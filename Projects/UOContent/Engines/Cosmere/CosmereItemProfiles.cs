using System;

namespace Server.Engines.Cosmere;

[Flags]
public enum CosmereWeaponTraits
{
    None = 0,
    Offhand = 1 << 0,
    TwoHanded = 1 << 1,
    Quickdraw = 1 << 2,
    Defensive = 1 << 3,
    Discreet = 1 << 4,
    Fragile = 1 << 5,
    Loaded = 1 << 6,
    Ranged = 1 << 7,
    Unarmed = 1 << 8
}

public enum CosmereWeaponCatalogId
{
    Unarmed,
    Knife,
    GlassDagger,
    Sidesword,
    Shortspear,
    Hammer,
    Shield,
    Shortbow,
    Longbow,
    Crossbow,
    MappedGeneric
}

public enum CosmereArmorCatalogId
{
    None,
    Uniform,
    Leather,
    Breastplate
}

public readonly struct CosmereWeaponProfile
{
    public required CosmereWeaponCatalogId CatalogId { get; init; }
    public required int DieCount { get; init; }
    public required int DieSides { get; init; }
    public required int FlatDamage { get; init; }
    public required CosmereDamageType DamageType { get; init; }
    public required CosmereSkill AttackSkill { get; init; }
    public required CosmereWeaponTraits Traits { get; init; }

    public bool Has(CosmereWeaponTraits trait) => (Traits & trait) != 0;

    public string DescribeDamage()
    {
        if (DieSides <= 0)
        {
            return FlatDamage.ToString();
        }

        return DieCount > 1 ? $"{DieCount}d{DieSides}" : $"1d{DieSides}";
    }
}

public readonly struct CosmereArmorProfile
{
    public required CosmereArmorCatalogId CatalogId { get; init; }
    public required int Deflect { get; init; }
    public required int Cumbersome { get; init; }
    public required bool Presentable { get; init; }
}
