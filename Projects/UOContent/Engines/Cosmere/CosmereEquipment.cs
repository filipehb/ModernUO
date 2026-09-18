using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public static class CosmereEquipment
{
    public static CosmereWeaponProfile ResolveWeaponFor(Mobile attacker, BaseWeapon weapon)
    {
        var strength = 1;
        if (attacker is PlayerMobile pm)
        {
            pm.EnsureCosmereSheet();
            strength = pm.CosmereSheet.Strength;
        }

        return CosmereItemCatalog.ResolveWeapon(weapon, strength);
    }

    public static int GetOffhandStrikeFocusCost(BaseWeapon weapon, int attackerStrength = 1)
    {
        var profile = CosmereItemCatalog.ResolveWeapon(weapon, attackerStrength);
        return profile.Has(CosmereWeaponTraits.Offhand) ? 1 : 2;
    }

    public static bool TryGetActiveArmor(PlayerMobile pm, out CosmereArmorProfile profile)
    {
        profile = default;
        CosmereArmorProfile best = default;
        var found = false;

        foreach (var item in pm.Items)
        {
            if (item is BaseShield)
            {
                continue;
            }

            if (item is BaseArmor armor && armor.BodyPosition != ArmorBodyType.Chest)
            {
                continue;
            }

            if (!CosmereItemCatalog.TryResolveArmorItem(item, out var candidate))
            {
                continue;
            }

            if (candidate.CatalogId == CosmereArmorCatalogId.None)
            {
                continue;
            }

            if (!found ||
                candidate.Deflect > best.Deflect ||
                (candidate.Deflect == best.Deflect && candidate.Cumbersome > best.Cumbersome) ||
                (candidate.Deflect == best.Deflect && candidate.Cumbersome == best.Cumbersome &&
                 candidate.Presentable && !best.Presentable))
            {
                best = candidate;
                found = true;
            }
        }

        if (!found)
        {
            return false;
        }

        profile = best;
        return true;
    }

    public static int GetDeflect(PlayerMobile pm) =>
        pm != null && TryGetActiveArmor(pm, out var profile) ? profile.Deflect : 0;

    public static int ApplyDeflect(int amount, CosmereDamageType type, int deflect)
    {
        if (amount <= 0)
        {
            return 0;
        }

        if (type is CosmereDamageType.Spirit or CosmereDamageType.Vital)
        {
            return amount;
        }

        if (type is CosmereDamageType.Impact or CosmereDamageType.Keen or CosmereDamageType.Energy)
        {
            return Math.Max(0, amount - Math.Max(0, deflect));
        }

        return amount;
    }

    public static void SyncCumbersome(PlayerMobile pm)
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;

        if (IsCumbersomeActive(pm))
        {
            sheet.EnsureCondition(CosmereConditionKind.Slowed, source: "cumbersome");
            return;
        }

        for (var i = sheet.Conditions.Count - 1; i >= 0; i--)
        {
            var condition = sheet.Conditions[i];
            if (condition.Kind == CosmereConditionKind.Slowed &&
                condition.Source == "cumbersome")
            {
                sheet.Conditions.RemoveAt(i);
            }
        }
    }

    public static bool IsCumbersomeActive(PlayerMobile pm)
    {
        pm.EnsureCosmereSheet();
        return TryGetActiveArmor(pm, out var armor) &&
               armor.Cumbersome > 0 &&
               pm.CosmereSheet.Strength < armor.Cumbersome;
    }

    public static bool CheckTwoHandedEquip(PlayerMobile pm, Item item)
    {
        if (item is not BaseWeapon newWeapon)
        {
            return true;
        }

        pm.EnsureCosmereSheet();
        var strength = pm.CosmereSheet.Strength;
        var newProfile = CosmereItemCatalog.ResolveWeapon(newWeapon, strength);

        var main = pm.FindItemOnLayer(Layer.OneHanded) as BaseWeapon
                   ?? pm.FindItemOnLayer(Layer.TwoHanded) as BaseWeapon;

        if (main == null || main == newWeapon)
        {
            return true;
        }

        var mainProfile = CosmereItemCatalog.ResolveWeapon(main, strength);

        if (mainProfile.Has(CosmereWeaponTraits.TwoHanded) && newWeapon.Layer == Layer.OneHanded)
        {
            pm.SendMessage("Your main weapon is Two-Handed; you cannot equip another weapon in the off hand.");
            return false;
        }

        if (newProfile.Has(CosmereWeaponTraits.TwoHanded) && main.Layer == Layer.OneHanded)
        {
            pm.SendMessage("You cannot equip a Two-Handed weapon while holding another weapon.");
            return false;
        }

        return true;
    }

    public static int RollWeaponDamage(CosmereWeaponProfile profile)
    {
        if (profile.DieSides <= 0)
        {
            return Math.Max(0, profile.FlatDamage);
        }

        var total = 0;
        var count = Math.Max(1, profile.DieCount);
        for (var i = 0; i < count; i++)
        {
            total += Utility.Random(profile.DieSides) + 1;
        }

        return total;
    }
}
