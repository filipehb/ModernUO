using Server.Mobiles;

namespace Server.Engines.Cosmere;

public static class CosmerePools
{
    public static int ApplyIncomingDamage(
        CosmereSheet sheet,
        int amount,
        CosmereDamageType type,
        int deflect
    )
    {
        if (sheet == null || amount <= 0)
        {
            return 0;
        }

        var reduced = CosmereEquipment.ApplyDeflect(amount, type, deflect);
        if (reduced <= 0)
        {
            return 0;
        }

        sheet.Health = System.Math.Max(0, sheet.Health - reduced);
        sheet.ClampPools();

        if (sheet.Health <= 0)
        {
            sheet.SetFlagCondition(CosmereConditionKind.Unconscious, true, "zero-health");
            CosmereInjury.ResolveInjury(sheet);
        }

        return reduced;
    }

    public static void DamageHealth(
        CosmereSheet sheet,
        int amount,
        CosmereDamageType type = CosmereDamageType.Impact,
        int deflect = 0
    ) =>
        ApplyIncomingDamage(sheet, amount, type, deflect);

    public static void DamageHealth(
        PlayerMobile pm,
        int amount,
        CosmereDamageType type = CosmereDamageType.Impact
    )
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var healthBefore = sheet.Health;
        var focusBefore = sheet.Focus;
        var deflect = CosmereEquipment.GetDeflect(pm);
        var dealt = ApplyIncomingDamage(sheet, amount, type, deflect);
        CosmereFacade.Apply(pm);
        NotifyPools(pm);
        CosmereDebug.Trace(
            pm,
            $"DamageHealth raw={amount} type={type} deflect={deflect} dealt={dealt} " +
            $"Health {healthBefore}→{sheet.Health}/{sheet.MaxHealth} Focus {focusBefore}→{sheet.Focus}"
        );
    }

    public static void HealHealth(CosmereSheet sheet, int amount)
    {
        if (amount <= 0 || sheet == null)
        {
            return;
        }

        sheet.Health = System.Math.Min(sheet.MaxHealth, sheet.Health + amount);
        sheet.ClampPools();

        if (sheet.Health >= 1)
        {
            sheet.ClearCondition(CosmereConditionKind.Unconscious);
        }
    }

    public static void HealHealth(PlayerMobile pm, int amount)
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var healthBefore = sheet.Health;
        var focusBefore = sheet.Focus;
        HealHealth(sheet, amount);
        CosmereFacade.Apply(pm);
        NotifyPools(pm);
        CosmereDebug.TracePools(pm, $"HealHealth +{amount}", healthBefore, focusBefore);
    }

    public static bool SpendFocus(CosmereSheet sheet, int amount)
    {
        if (sheet == null || amount <= 0)
        {
            return false;
        }

        if (sheet.Focus < amount)
        {
            return false;
        }

        sheet.Focus -= amount;
        sheet.ClampPools();
        return true;
    }

    public static bool SpendFocus(PlayerMobile pm, int amount)
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var healthBefore = sheet.Health;
        var focusBefore = sheet.Focus;
        var ok = SpendFocus(sheet, amount);
        if (ok)
        {
            CosmereFacade.Apply(pm);
            NotifyPools(pm);
            CosmereDebug.TracePools(pm, $"SpendFocus -{amount}", healthBefore, focusBefore);
        }
        else
        {
            CosmereDebug.Trace(pm, $"SpendFocus -{amount} FAILED (have {focusBefore})");
        }

        return ok;
    }

    public static void RecoverFocus(CosmereSheet sheet, int amount)
    {
        if (sheet == null || amount <= 0)
        {
            return;
        }

        sheet.Focus = System.Math.Min(sheet.MaxFocus, sheet.Focus + amount);
        sheet.ClampPools();
    }

    public static void RecoverFocus(PlayerMobile pm, int amount)
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var healthBefore = sheet.Health;
        var focusBefore = sheet.Focus;
        RecoverFocus(sheet, amount);
        CosmereFacade.Apply(pm);
        NotifyPools(pm);
        CosmereDebug.TracePools(pm, $"RecoverFocus +{amount}", healthBefore, focusBefore);
    }

    public static void Rez(CosmereSheet sheet)
    {
        if (sheet == null)
        {
            return;
        }

        sheet.Dead = false;
        sheet.ClearCondition(CosmereConditionKind.Unconscious);
        sheet.Health = sheet.MaxHealth;
        sheet.ClampPools();
    }

    public static bool TryRez(PlayerMobile pm)
    {
        if (CosmereConfig.PermanentDeath)
        {
            pm.SendMessage("Permanent death is enabled on this shard; resurrection is not available.");
            return false;
        }

        pm.EnsureCosmereSheet();
        Rez(pm.CosmereSheet);
        pm.Frozen = false;
        CosmereFacade.Apply(pm);
        pm.SendMessage("You have been restored (Cosmere rez).");
        NotifyPools(pm);
        return true;
    }

    private static void NotifyPools(PlayerMobile pm)
    {
        var sheet = pm.CosmereSheet;
        pm.SendMessage(
            $"Cosmere pools: Health {sheet.Health}/{sheet.MaxHealth}, Focus {sheet.Focus}/{sheet.MaxFocus}."
        );
    }
}
