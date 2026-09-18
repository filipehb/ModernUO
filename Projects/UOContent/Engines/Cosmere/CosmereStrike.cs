using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public enum CosmereStrikeOutcome
{
    Hit,
    Graze,
    Miss,
    Blocked
}

public readonly struct CosmereStrikeResult
{
    public required CosmereStrikeOutcome Outcome { get; init; }
    public required CosmereTestResult Test { get; init; }
    public required int DamageRoll { get; init; }
    public required int DamageDealt { get; init; }
    public required bool Dodged { get; init; }
    public required CosmereSkill AttackSkill { get; init; }
    public required int Dc { get; init; }
    public required CosmereDamageType DamageType { get; init; }
}

public static class CosmereStrike
{
    public static CosmereSkill GetAttackSkill(BaseWeapon weapon, int strength = 1) =>
        CosmereItemCatalog.ResolveWeapon(weapon, strength).AttackSkill;

    public static int GetDamageDieSides(BaseWeapon weapon, int strength = 1)
    {
        var profile = CosmereItemCatalog.ResolveWeapon(weapon, strength);
        return profile.DieSides > 0 ? profile.DieSides : 0;
    }

    public static bool IsRanged(BaseWeapon weapon) =>
        CosmereItemCatalog.ResolveWeapon(weapon).Has(CosmereWeaponTraits.Ranged) || weapon is BaseRanged;

    public static int GetDefenseDc(Mobile defender)
    {
        if (defender is PlayerMobile pm)
        {
            pm.EnsureCosmereSheet();
            return pm.CosmereSheet.PhysicalDefense;
        }

        return CosmereConfig.NpcPhysicalDefense;
    }

    public static CosmereStrikeResult Resolve(
        CosmereSheet attackerSheet,
        Mobile attacker,
        Mobile defender,
        BaseWeapon weapon,
        CosmereCombatState attackerState,
        CosmereCombatState defenderState,
        bool refreshAttackerReaction = true
    )
    {
        var profile = CosmereEquipment.ResolveWeaponFor(attacker, weapon);
        var skill = profile.AttackSkill;
        var dc = GetDefenseDc(defender);
        var disadv = 0;
        var dodged = false;

        if (defender is PlayerMobile defPm && defenderState != null)
        {
            dodged = CosmereCombat.TryConsumeDodge(defPm, defenderState);
            if (dodged)
            {
                disadv++;
            }

            if (defenderState.Braced)
            {
                disadv++;
            }
        }

        if (profile.Has(CosmereWeaponTraits.Ranged) && attacker.InRange(defender, 1))
        {
            disadv++;
        }

        var test = CosmereTestResolver.Resolve(attackerSheet, skill, dc, advantages: 0, disadvantages: disadv);
        if (test.Blocked)
        {
            return new CosmereStrikeResult
            {
                Outcome = CosmereStrikeOutcome.Blocked,
                Test = test,
                DamageRoll = 0,
                DamageDealt = 0,
                Dodged = dodged,
                AttackSkill = skill,
                Dc = dc,
                DamageType = profile.DamageType
            };
        }

        var damageRoll = CosmereEquipment.RollWeaponDamage(profile);
        ComputeDamageOutcome(
            attackerSheet,
            test.Success,
            damageRoll,
            skill,
            out var outcome,
            out var damage
        );

        if (damage > 0)
        {
            ApplyDamage(attacker, defender, damage, profile.DamageType);
        }

        if (defender is PlayerMobile defOverstim)
        {
            CosmereAllomancy.TryOverstimulation(defOverstim);
        }

        // Attacking ends Brace on attacker
        if (attackerState != null)
        {
            attackerState.Braced = false;
            attackerState.Touch();
            if (refreshAttackerReaction)
            {
                attackerState.ReactionAvailable = true;
            }
        }

        defenderState?.Touch();

        return new CosmereStrikeResult
        {
            Outcome = outcome,
            Test = test,
            DamageRoll = damageRoll,
            DamageDealt = damage,
            Dodged = dodged,
            AttackSkill = skill,
            Dc = dc,
            DamageType = profile.DamageType
        };
    }

    public static void ComputeDamageOutcome(
        CosmereSheet attackerSheet,
        bool testSuccess,
        int damageRoll,
        CosmereSkill skill,
        out CosmereStrikeOutcome outcome,
        out int damage
    )
    {
        outcome = CosmereStrikeOutcome.Miss;
        damage = 0;

        if (testSuccess)
        {
            outcome = CosmereStrikeOutcome.Hit;
            damage = damageRoll + attackerSheet.GetModifier(skill);
            return;
        }

        if (CosmereConfig.AutoGraze && attackerSheet.Focus >= 1 && CosmerePools.SpendFocus(attackerSheet, 1))
        {
            outcome = CosmereStrikeOutcome.Graze;
            damage = damageRoll;
        }
    }

    public static void ApplyDamage(
        Mobile attacker,
        Mobile defender,
        int damage,
        CosmereDamageType type = CosmereDamageType.Impact
    )
    {
        if (damage <= 0)
        {
            return;
        }

        if (defender is PlayerMobile pm)
        {
            CosmereCombat.ApplyCosmereDamage(pm, damage, attacker, type);
            return;
        }

        defender.Damage(damage, attacker);
    }
}
