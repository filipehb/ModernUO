using System;
using Server.Items;

namespace Server.Engines.Cosmere;

public static class CosmereItemCatalog
{
    public static CosmereWeaponProfile UnarmedForStrength(int strength) =>
        strength switch
        {
            <= 2 => new CosmereWeaponProfile
            {
                CatalogId = CosmereWeaponCatalogId.Unarmed,
                DieCount = 0,
                DieSides = 0,
                FlatDamage = 1,
                DamageType = CosmereDamageType.Impact,
                AttackSkill = CosmereSkill.Athletics,
                Traits = CosmereWeaponTraits.Unarmed
            },
            <= 4 => new CosmereWeaponProfile
            {
                CatalogId = CosmereWeaponCatalogId.Unarmed,
                DieCount = 1,
                DieSides = 4,
                FlatDamage = 0,
                DamageType = CosmereDamageType.Impact,
                AttackSkill = CosmereSkill.Athletics,
                Traits = CosmereWeaponTraits.Unarmed
            },
            <= 6 => new CosmereWeaponProfile
            {
                CatalogId = CosmereWeaponCatalogId.Unarmed,
                DieCount = 1,
                DieSides = 8,
                FlatDamage = 0,
                DamageType = CosmereDamageType.Impact,
                AttackSkill = CosmereSkill.Athletics,
                Traits = CosmereWeaponTraits.Unarmed
            },
            <= 8 => new CosmereWeaponProfile
            {
                CatalogId = CosmereWeaponCatalogId.Unarmed,
                DieCount = 2,
                DieSides = 6,
                FlatDamage = 0,
                DamageType = CosmereDamageType.Impact,
                AttackSkill = CosmereSkill.Athletics,
                Traits = CosmereWeaponTraits.Unarmed
            },
            _ => new CosmereWeaponProfile
            {
                CatalogId = CosmereWeaponCatalogId.Unarmed,
                DieCount = 2,
                DieSides = 10,
                FlatDamage = 0,
                DamageType = CosmereDamageType.Impact,
                AttackSkill = CosmereSkill.Athletics,
                Traits = CosmereWeaponTraits.Unarmed
            }
        };

    public static CosmereWeaponProfile ResolveWeapon(BaseWeapon weapon, int attackerStrength = 1)
    {
        if (weapon == null || weapon is Fists)
        {
            return UnarmedForStrength(attackerStrength);
        }

        var name = weapon.GetType().Name;
        var nameLower = name.ToLowerInvariant();

        if (nameLower.Contains("kryss", StringComparison.Ordinal))
        {
            return GlassDagger();
        }

        if (weapon is BaseKnife || nameLower.Contains("dagger", StringComparison.Ordinal) ||
            nameLower.Contains("knife", StringComparison.Ordinal))
        {
            return Knife();
        }

        if (nameLower.Contains("spear", StringComparison.Ordinal) || nameLower.Contains("pike", StringComparison.Ordinal))
        {
            return Shortspear();
        }

        if (nameLower.Contains("shield", StringComparison.Ordinal))
        {
            return Shield();
        }

        if (weapon is BaseRanged)
        {
            if (nameLower.Contains("crossbow", StringComparison.Ordinal))
            {
                return Crossbow();
            }

            if (nameLower.Contains("composite", StringComparison.Ordinal) ||
                nameLower.Contains("long", StringComparison.Ordinal))
            {
                return Longbow();
            }

            return Shortbow();
        }

        return weapon.Skill switch
        {
            SkillName.Fencing => Knife(),
            SkillName.Swords => Sidesword(),
            SkillName.Macing => Hammer(),
            SkillName.Lumberjacking => Hammer(),
            SkillName.Archery => Shortbow(),
            SkillName.Wrestling => UnarmedForStrength(attackerStrength),
            _ => MappedGenericMelee()
        };
    }

    public static CosmereArmorProfile ResolveArmorItem(Item item)
    {
        if (item is not BaseArmor armor)
        {
            // Clothes / robes as Uniform
            if (item.Layer is Layer.InnerTorso or Layer.MiddleTorso or Layer.OuterTorso or Layer.Shirt)
            {
                return Uniform();
            }

            return default;
        }

        return armor.MaterialType switch
        {
            ArmorMaterialType.Cloth => Uniform(),
            ArmorMaterialType.Leather or ArmorMaterialType.Studded or ArmorMaterialType.Spined
                or ArmorMaterialType.Horned or ArmorMaterialType.Barbed => Leather(),
            ArmorMaterialType.Bone or ArmorMaterialType.Ringmail or ArmorMaterialType.Chainmail
                or ArmorMaterialType.Plate or ArmorMaterialType.Dragon => Breastplate(),
            _ => Leather()
        };
    }

    public static bool TryResolveArmorItem(Item item, out CosmereArmorProfile profile)
    {
        profile = ResolveArmorItem(item);
        return profile.CatalogId != CosmereArmorCatalogId.None &&
               (item is BaseArmor || item.Layer is Layer.InnerTorso or Layer.MiddleTorso or Layer.OuterTorso or Layer.Shirt);
    }

    private static CosmereWeaponProfile Knife() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.Knife,
            DieCount = 1,
            DieSides = 4,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Keen,
            AttackSkill = CosmereSkill.LightWeaponry,
            Traits = CosmereWeaponTraits.Discreet | CosmereWeaponTraits.Offhand
        };

    private static CosmereWeaponProfile GlassDagger() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.GlassDagger,
            DieCount = 1,
            DieSides = 4,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Keen,
            AttackSkill = CosmereSkill.LightWeaponry,
            Traits = CosmereWeaponTraits.Discreet | CosmereWeaponTraits.Fragile | CosmereWeaponTraits.Offhand
        };

    private static CosmereWeaponProfile Sidesword() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.Sidesword,
            DieCount = 1,
            DieSides = 6,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Keen,
            AttackSkill = CosmereSkill.LightWeaponry,
            Traits = CosmereWeaponTraits.Quickdraw
        };

    private static CosmereWeaponProfile Shortspear() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.Shortspear,
            DieCount = 1,
            DieSides = 8,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Keen,
            AttackSkill = CosmereSkill.LightWeaponry,
            Traits = CosmereWeaponTraits.TwoHanded
        };

    private static CosmereWeaponProfile Hammer() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.Hammer,
            DieCount = 1,
            DieSides = 10,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Impact,
            AttackSkill = CosmereSkill.HeavyWeaponry,
            Traits = CosmereWeaponTraits.TwoHanded
        };

    private static CosmereWeaponProfile Shield() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.Shield,
            DieCount = 1,
            DieSides = 4,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Impact,
            AttackSkill = CosmereSkill.HeavyWeaponry,
            Traits = CosmereWeaponTraits.Defensive | CosmereWeaponTraits.Offhand
        };

    private static CosmereWeaponProfile Shortbow() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.Shortbow,
            DieCount = 1,
            DieSides = 6,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Keen,
            AttackSkill = CosmereSkill.LightWeaponry,
            Traits = CosmereWeaponTraits.TwoHanded | CosmereWeaponTraits.Ranged
        };

    private static CosmereWeaponProfile Longbow() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.Longbow,
            DieCount = 1,
            DieSides = 6,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Keen,
            AttackSkill = CosmereSkill.LightWeaponry,
            Traits = CosmereWeaponTraits.TwoHanded | CosmereWeaponTraits.Ranged
        };

    private static CosmereWeaponProfile Crossbow() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.Crossbow,
            DieCount = 1,
            DieSides = 8,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Keen,
            AttackSkill = CosmereSkill.HeavyWeaponry,
            Traits = CosmereWeaponTraits.TwoHanded | CosmereWeaponTraits.Ranged | CosmereWeaponTraits.Loaded
        };

    private static CosmereWeaponProfile MappedGenericMelee() =>
        new()
        {
            CatalogId = CosmereWeaponCatalogId.MappedGeneric,
            DieCount = 1,
            DieSides = 6,
            FlatDamage = 0,
            DamageType = CosmereDamageType.Impact,
            AttackSkill = CosmereSkill.HeavyWeaponry,
            Traits = CosmereWeaponTraits.None
        };

    private static CosmereArmorProfile Uniform() =>
        new()
        {
            CatalogId = CosmereArmorCatalogId.Uniform,
            Deflect = 0,
            Cumbersome = 0,
            Presentable = true
        };

    private static CosmereArmorProfile Leather() =>
        new()
        {
            CatalogId = CosmereArmorCatalogId.Leather,
            Deflect = 1,
            Cumbersome = 0,
            Presentable = false
        };

    private static CosmereArmorProfile Breastplate() =>
        new()
        {
            CatalogId = CosmereArmorCatalogId.Breastplate,
            Deflect = 2,
            Cumbersome = 3,
            Presentable = false
        };
}
