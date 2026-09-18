using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public static class CosmereCombat
{
    private static int _nextSceneId = 1;

    public static CosmereCombatState GetOrCreateState(PlayerMobile pm)
    {
        pm.CosmereCombat ??= new CosmereCombatState();
        return pm.CosmereCombat;
    }

    public static void TickIdle(PlayerMobile pm)
    {
        var state = pm.CosmereCombat;
        if (state is not { InCombat: true })
        {
            return;
        }

        var idle = CosmereConfig.CombatIdleSeconds;
        if (idle <= 0)
        {
            return;
        }

        if (Core.Now - state.LastCombatActivity >= TimeSpan.FromSeconds(idle))
        {
            EndScene(pm);
            pm.SendMessage("Cosmere combat scene ended (idle).");
        }
    }

    public static void BeginCombat(PlayerMobile pm, Mobile other = null)
    {
        var state = GetOrCreateState(pm);
        if (!state.InCombat)
        {
            state.BeginScene(_nextSceneId++);
            CosmereAllomancy.OnCombatSceneStart(pm);
        }
        else
        {
            state.Touch();
        }

        if (other is PlayerMobile otherPm)
        {
            var otherState = GetOrCreateState(otherPm);
            if (!otherState.InCombat)
            {
                otherState.BeginScene(state.SceneId);
                CosmereAllomancy.OnCombatSceneStart(otherPm);
            }
            else
            {
                otherState.Touch();
            }
        }
    }

    public static void EndScene(PlayerMobile pm)
    {
        pm.CosmereCombat?.EndScene();
    }

    public static bool CanReact(PlayerMobile pm, CosmereCombatState state)
    {
        if (state is not { InCombat: true, ReactionAvailable: true })
        {
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (sheet.Dead || sheet.HasCondition(CosmereConditionKind.Unconscious) ||
            sheet.HasCondition(CosmereConditionKind.Surprised))
        {
            return false;
        }

        return true;
    }

    public static bool TryConsumeDodge(PlayerMobile defender, CosmereCombatState state)
    {
        if (!CanReact(defender, state))
        {
            return false;
        }

        var shouldTry = CosmereConfig.AutoDodge || state.DodgeArmed;
        if (!shouldTry)
        {
            return false;
        }

        defender.EnsureCosmereSheet();
        if (defender.CosmereSheet.Focus < 1)
        {
            state.DodgeArmed = false;
            return false;
        }

        if (!CosmerePools.SpendFocus(defender, 1))
        {
            state.DodgeArmed = false;
            return false;
        }

        state.ReactionAvailable = false;
        state.DodgeArmed = false;
        state.Touch();
        defender.SendMessage("You Dodge (disadvantage on the attack).");
        CosmereDebug.Trace(defender, "Dodge consumed: Focus -1, reaction spent.");
        return true;
    }

    public static void ApplyCosmereDamage(
        PlayerMobile pm,
        int amount,
        Mobile from,
        CosmereDamageType type = CosmereDamageType.Impact
    )
    {
        pm._cosmereDamageDepth++;
        try
        {
            BeginCombat(pm, from);
            CosmerePools.DamageHealth(pm, amount, type);
        }
        finally
        {
            pm._cosmereDamageDepth--;
        }
    }

    /// <summary>
    /// Returns true if Cosmere handled the swing (caller should skip UO CheckHit/OnHit).
    /// </summary>
    public static bool TryResolveSwing(Mobile attacker, Mobile defender, BaseWeapon weapon)
    {
        if (!CosmereConfig.CombatEnabled || attacker is not PlayerMobile atkPm)
        {
            return false;
        }

        atkPm.EnsureCosmereSheet();
        var sheet = atkPm.CosmereSheet;
        if (sheet.Dead || sheet.HasCondition(CosmereConditionKind.Unconscious))
        {
            atkPm.SendMessage("You cannot Strike while dead or unconscious.");
            return true; // swallow swing
        }

        TickIdle(atkPm);
        BeginCombat(atkPm, defender);

        var atkState = GetOrCreateState(atkPm);
        CosmereCombatState defState = null;
        if (defender is PlayerMobile defPm)
        {
            TickIdle(defPm);
            BeginCombat(defPm, atkPm);
            defState = GetOrCreateState(defPm);
        }

        weapon ??= atkPm.Weapon as BaseWeapon;

        var result = CosmereStrike.Resolve(sheet, atkPm, defender, weapon, atkState, defState);
        NotifyStrike(atkPm, defender, result);

        if (defender is PlayerMobile defNotify)
        {
            NotifyStrike(defNotify, atkPm, result, asDefender: true);
        }

        // Animation / sound without UO damage
        weapon?.PlaySwingAnimation(atkPm);
        if (result.DamageDealt > 0)
        {
            defender.PlaySound(weapon?.GetHitDefendSound(atkPm, defender) ?? 0x29);
            atkPm.PlaySound(weapon?.GetHitAttackSound(atkPm, defender) ?? 0x237);
        }
        else
        {
            atkPm.PlaySound(weapon?.GetMissAttackSound(atkPm, defender) ?? 0x23A);
        }

        CosmereFacade.Apply(atkPm);
        return true;
    }

    public static bool TryReactiveStrike(PlayerMobile reactor, Mobile leaver)
    {
        if (!CosmereConfig.CombatEnabled)
        {
            return false;
        }

        var state = reactor.CosmereCombat;
        if (!CanReact(reactor, state) || reactor.CosmereSheet.Focus < 1)
        {
            return false;
        }

        if (!CosmerePools.SpendFocus(reactor, 1))
        {
            return false;
        }

        state.ReactionAvailable = false;
        state.Touch();

        reactor.EnsureCosmereSheet();
        BeginCombat(reactor, leaver);

        var weapon = reactor.Weapon as BaseWeapon;
        CosmereCombatState defState = null;
        if (leaver is PlayerMobile defPm)
        {
            defState = GetOrCreateState(defPm);
        }

        var result = CosmereStrike.Resolve(
            reactor.CosmereSheet,
            reactor,
            leaver,
            weapon,
            state,
            defState,
            refreshAttackerReaction: false
        );

        reactor.SendMessage("Reactive Strike!");
        NotifyStrike(reactor, leaver, result);
        CosmereFacade.Apply(reactor);
        return true;
    }

    public static bool TrySceneRecover(PlayerMobile pm, int healthShare)
    {
        TickIdle(pm);
        var state = GetOrCreateState(pm);
        if (!state.InCombat)
        {
            pm.SendMessage("Recover is only available during a combat scene. Use CosmereShortRest out of combat.");
            return false;
        }

        if (state.RecoverUsedThisScene)
        {
            pm.SendMessage("You already Recovered this scene.");
            return false;
        }

        pm.EnsureCosmereSheet();
        if (pm.CosmereSheet.Dead)
        {
            pm.SendMessage("Cannot Recover while dead.");
            return false;
        }

        var result = CosmereRest.ShortRest(pm, healthShare);
        if (!result.Success)
        {
            return false;
        }

        state.RecoverUsedThisScene = true;
        state.Touch();
        return true;
    }

    public static bool IsLeavingMeleeReach(Point3D from, Point3D to, Point3D observer) =>
        Utility.InRange(from, observer, 1) && !Utility.InRange(to, observer, 1);

    private static void NotifyStrike(
        PlayerMobile pm,
        Mobile other,
        CosmereStrikeResult result,
        bool asDefender = false
    )
    {
        var role = asDefender ? "vs you" : $"vs {other.Name}";
        if (result.Outcome == CosmereStrikeOutcome.Blocked)
        {
            pm.SendMessage($"Strike blocked ({result.Test.BlockReason}).");
            CosmereDebug.TraceStrike(pm, result, role);
            return;
        }

        pm.SendMessage(
            $"Strike {role}: {CosmereSkills.GetDisplayName(result.AttackSkill)} vs DC {result.Dc} " +
            $"d20={result.Test.D20}+{result.Test.Modifier}" +
            (result.Test.ExhaustedPenalty > 0 ? $"-{result.Test.ExhaustedPenalty}exh" : "") +
            $"={result.Test.Total} → {result.Outcome}" +
            (result.Dodged ? " (Dodged)" : "") +
            (result.DamageDealt > 0 ? $" dmg {result.DamageDealt}" : "")
        );
        CosmereDebug.TraceStrike(pm, result, role);
    }
}
