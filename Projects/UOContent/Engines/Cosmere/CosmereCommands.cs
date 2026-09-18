using System;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Cosmere;

public static class CosmereCommands
{
    public static void Configure()
    {
        CommandSystem.Register("CosmereSheet", AccessLevel.Player, CosmereSheet_OnCommand);
        CommandSystem.Register("CosmereTest", AccessLevel.GameMaster, CosmereTest_OnCommand);
        CommandSystem.Register("CosmereDamage", AccessLevel.GameMaster, CosmereDamage_OnCommand);
        CommandSystem.Register("CosmereHeal", AccessLevel.GameMaster, CosmereHeal_OnCommand);
        CommandSystem.Register("CosmereSpendFocus", AccessLevel.GameMaster, CosmereSpendFocus_OnCommand);
        CommandSystem.Register("CosmereShortRest", AccessLevel.GameMaster, CosmereShortRest_OnCommand);
        CommandSystem.Register("CosmereLongRest", AccessLevel.GameMaster, CosmereLongRest_OnCommand);
        CommandSystem.Register("CosmereCondition", AccessLevel.GameMaster, CosmereCondition_OnCommand);
        CommandSystem.Register("CosmereRez", AccessLevel.GameMaster, CosmereRez_OnCommand);
        CommandSystem.Register("CosmereStrike", AccessLevel.GameMaster, CosmereStrike_OnCommand);
        CommandSystem.Register("CosmereRecover", AccessLevel.Player, CosmereRecover_OnCommand);
        CommandSystem.Register("CosmereEndScene", AccessLevel.GameMaster, CosmereEndScene_OnCommand);
        CommandSystem.Register("CosmereArmDodge", AccessLevel.Player, CosmereArmDodge_OnCommand);
        CommandSystem.Register("CosmereCombat", AccessLevel.GameMaster, CosmereCombat_OnCommand);
        CommandSystem.Register("CosmereItem", AccessLevel.Player, CosmereItem_OnCommand);
        CommandSystem.Register("CosmereBrace", AccessLevel.Player, CosmereBrace_OnCommand);
        CommandSystem.Register("CosmereDraw", AccessLevel.Player, CosmereDraw_OnCommand);
        CommandSystem.Register("CosmereOffhandStrike", AccessLevel.GameMaster, CosmereOffhandStrike_OnCommand);
        CommandSystem.Register("CosmereSnap", AccessLevel.GameMaster, CosmereSnap_OnCommand);
        CommandSystem.Register("CosmereUnlockPower", AccessLevel.GameMaster, CosmereUnlockPower_OnCommand);
        CommandSystem.Register("CosmereDrink", AccessLevel.Player, CosmereDrink_OnCommand);
        CommandSystem.Register("CosmereBurn", AccessLevel.Player, CosmereBurn_OnCommand);
        CommandSystem.Register("CosmereMaintain", AccessLevel.Player, CosmereMaintain_OnCommand);
        CommandSystem.Register("CosmerePush", AccessLevel.Player, CosmerePush_OnCommand);
        CommandSystem.Register("CosmereCoin", AccessLevel.Player, CosmereCoin_OnCommand);
        CommandSystem.Register("CosmereSeek", AccessLevel.Player, CosmereSeek_OnCommand);
        CommandSystem.Register("CosmereRiot", AccessLevel.Player, CosmereRiot_OnCommand);
        CommandSystem.Register("CosmereAllomancy", AccessLevel.Player, CosmereAllomancy_OnCommand);
        CommandSystem.Register("CosmereTalk", AccessLevel.Player, CosmereTalk_OnCommand);
        CommandSystem.Register("CosmereEndTalk", AccessLevel.Player, CosmereEndTalk_OnCommand);
        CommandSystem.Register("CosmereInfluence", AccessLevel.Player, CosmereInfluence_OnCommand);
        CommandSystem.Register("CosmereResist", AccessLevel.Player, CosmereResist_OnCommand);
        CommandSystem.Register("CosmereHelp", AccessLevel.Player, CosmereHelp_OnCommand);
        CommandSystem.Register("CosmereHinder", AccessLevel.Player, CosmereHinder_OnCommand);
        CommandSystem.Register("CosmereBolster", AccessLevel.Player, CosmereBolster_OnCommand);
        CommandSystem.Register("CosmereSocialOptIn", AccessLevel.Player, CosmereSocialOptIn_OnCommand);
        CommandSystem.Register("CosmereNpcFocus", AccessLevel.GameMaster, CosmereNpcFocus_OnCommand);
        CommandSystem.Register("CosmereSocial", AccessLevel.Player, CosmereSocial_OnCommand);
        CommandSystem.Register("CosmereLevelUp", AccessLevel.GameMaster, CosmereLevelUp_OnCommand);
        CommandSystem.Register("CosmereSpendAttr", AccessLevel.Player, CosmereSpendAttr_OnCommand);
        CommandSystem.Register("CosmereSpendSkill", AccessLevel.Player, CosmereSpendSkill_OnCommand);
        CommandSystem.Register("CosmereGrantTalent", AccessLevel.GameMaster, CosmereGrantTalent_OnCommand);
        CommandSystem.Register("CosmereGoal", AccessLevel.GameMaster, CosmereGoal_OnCommand);
        CommandSystem.Register("CosmereAdvancement", AccessLevel.Player, CosmereAdvancement_OnCommand);
        CommandSystem.Register("CosmereDebug", AccessLevel.Player, CosmereDebug_OnCommand);
        CommandSystem.Register("CosmereProfession", AccessLevel.Player, CosmereProfession_OnCommand);
        CommandSystem.Register("CosmereGather", AccessLevel.Player, CosmereGather_OnCommand);
        CommandSystem.Register("CosmereCraft", AccessLevel.Player, CosmereCraft_OnCommand);
        CommandSystem.Register("CosmereTrade", AccessLevel.Player, CosmereTrade_OnCommand);
        CommandSystem.Register("CosmereTime", AccessLevel.Player, CosmereTime_OnCommand);
    }

    [Usage("CosmereSheet")]
    [Description("Opens the Cosmere character sheet gump.")]
    private static void CosmereSheet_OnCommand(CommandEventArgs e) => CosmereSheetGump.DisplayTo(e.Mobile);

    [Usage("CosmereTest <skill> <dc> [adv] [disadv] [stakes]")]
    [Description("Rolls a Cosmere skill test for debugging. Optional: advantage count, disadvantage count, stakes.")]
    private static void CosmereTest_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            e.Mobile.SendMessage("Cosmere tests require a player character.");
            return;
        }

        if (e.Length < 2)
        {
            e.Mobile.SendMessage("Usage: CosmereTest <skill> <dc> [adv] [disadv] [stakes]");
            return;
        }

        if (!CosmereSkills.TryParse(e.GetString(0), out var skill))
        {
            e.Mobile.SendMessage("Unknown Cosmere skill.");
            return;
        }

        var dc = e.GetInt32(1);
        var adv = e.Length > 2 ? Math.Max(0, e.GetInt32(2)) : 0;
        var disadv = e.Length > 3 ? Math.Max(0, e.GetInt32(3)) : 0;
        var stakes = false;

        if (e.Length > 4)
        {
            var stakesArg = e.GetString(4);
            stakes = stakesArg.Equals("stakes", StringComparison.OrdinalIgnoreCase)
                     || stakesArg.Equals("true", StringComparison.OrdinalIgnoreCase)
                     || stakesArg == "1";
        }

        pm.EnsureCosmereSheet();
        var result = CosmereTestResolver.Resolve(pm.CosmereSheet, skill, dc, adv, disadv, stakes);
        CosmereDebug.TraceTest(pm, result);

        if (result.Blocked)
        {
            pm.SendMessage($"Cosmere test blocked: {result.BlockReason}.");
            return;
        }

        var plot = result.RaiseStakes
            ? result.PlotFace switch
            {
                CosmerePlotFace.Blank => "blank",
                CosmerePlotFace.Opportunity => "Opportunity",
                CosmerePlotFace.Complication2 => "Complication (+2)",
                CosmerePlotFace.Complication4 => "Complication (+4)",
                _ => "?"
            }
            : "n/a";

        pm.SendMessage(
            $"{CosmereSkills.GetDisplayName(result.Skill)} vs DC {result.Dc}: d20={result.D20} +{result.Modifier}" +
            (result.ComplicationBonus > 0 ? $" +{result.ComplicationBonus}comp" : "") +
            (result.ExhaustedPenalty > 0 ? $" -{result.ExhaustedPenalty}exh" : "") +
            $" = {result.Total} → {(result.Success ? "SUCCESS" : "FAILURE")}"
        );
        pm.SendMessage(
            $"Plot: {plot}. Opportunities: {result.Opportunities}. Complications: {result.Complications}."
        );
    }

    [Usage("CosmereDamage <amount>")]
    [Description("Applies Cosmere Health damage (triggers Unconscious/injury at 0).")]
    private static void CosmereDamage_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1)
        {
            e.Mobile.SendMessage("Usage: CosmereDamage <amount>");
            return;
        }

        CosmerePools.DamageHealth(pm, Math.Max(0, e.GetInt32(0)));
    }

    [Usage("CosmereHeal <amount>")]
    [Description("Heals Cosmere Health (does not clear Dead).")]
    private static void CosmereHeal_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1)
        {
            e.Mobile.SendMessage("Usage: CosmereHeal <amount>");
            return;
        }

        CosmerePools.HealHealth(pm, Math.Max(0, e.GetInt32(0)));
    }

    [Usage("CosmereSpendFocus <amount>")]
    [Description("Spends Cosmere Focus.")]
    private static void CosmereSpendFocus_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1)
        {
            e.Mobile.SendMessage("Usage: CosmereSpendFocus <amount>");
            return;
        }

        if (!CosmerePools.SpendFocus(pm, Math.Max(0, e.GetInt32(0))))
        {
            pm.SendMessage("Not enough Focus.");
        }
    }

    [Usage("CosmereShortRest <healthShare>")]
    [Description("Short rest: roll recovery die; healthShare goes to Health, remainder to Focus.")]
    private static void CosmereShortRest_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1)
        {
            e.Mobile.SendMessage("Usage: CosmereShortRest <healthShare>");
            return;
        }

        CosmereRest.ShortRest(pm, e.GetInt32(0));
    }

    [Usage("CosmereLongRest")]
    [Description("Long rest: full Health/Focus, Exhausted -1, clear flesh wounds.")]
    private static void CosmereLongRest_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereRest.LongRest(pm);
    }

    [Usage("CosmereCondition add|clear <kind> [magnitude]")]
    [Description("Debug add/clear a Cosmere condition.")]
    private static void CosmereCondition_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 2)
        {
            e.Mobile.SendMessage("Usage: CosmereCondition add|clear <kind> [magnitude]");
            return;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var op = e.GetString(0);
        if (!Enum.TryParse(e.GetString(1), true, out CosmereConditionKind kind))
        {
            pm.SendMessage("Unknown condition kind.");
            return;
        }

        if (op.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            sheet.ClearCondition(kind);
            pm.SendMessage($"Cleared {kind}.");
            return;
        }

        if (op.Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            var mag = e.Length > 2 ? Math.Max(0, e.GetInt32(2)) : (kind == CosmereConditionKind.Exhausted ? 1 : 0);
            if (kind == CosmereConditionKind.Exhausted)
            {
                sheet.AddExhausted(Math.Max(1, mag), "staff");
            }
            else
            {
                sheet.EnsureCondition(kind, mag, source: "staff");
            }

            pm.SendMessage($"Added {kind}{(mag > 0 ? $" [{mag}]" : "")}.");
            return;
        }

        pm.SendMessage("Usage: CosmereCondition add|clear <kind> [magnitude]");
    }

    [Usage("CosmereRez")]
    [Description("Clears Dead/Unconscious and restores Health to max (disabled if PermanentDeath).")]
    private static void CosmereRez_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmerePools.TryRez(pm);
    }

    [Usage("CosmereStrike")]
    [Description("Debug Cosmere Strike against a targeted mobile.")]
    private static void CosmereStrike_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.SendMessage("Target a mobile for Cosmere Strike.");
        pm.Target = new CosmereStrikeTarget(pm);
    }

    [Usage("CosmereRecover <healthShare>")]
    [Description("Once per combat scene: recovery die allocated like a short rest.")]
    private static void CosmereRecover_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1)
        {
            e.Mobile.SendMessage("Usage: CosmereRecover <healthShare>");
            return;
        }

        CosmereCombat.TrySceneRecover(pm, e.GetInt32(0));
    }

    [Usage("CosmereEndScene")]
    [Description("Ends the Cosmere combat scene for the caster.")]
    private static void CosmereEndScene_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereCombat.EndScene(pm);
        pm.SendMessage("Cosmere combat scene ended.");
    }

    [Usage("CosmereArmDodge")]
    [Description("Arms Dodge for the next Strike against you (costs Focus + reaction when used).")]
    private static void CosmereArmDodge_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereCombat.TickIdle(pm);
        var state = CosmereCombat.GetOrCreateState(pm);
        if (!state.InCombat)
        {
            CosmereCombat.BeginCombat(pm);
        }

        state.DodgeArmed = true;
        state.Touch();
        pm.SendMessage("Dodge armed for the next attack against you.");
    }

    [Usage("CosmereCombat")]
    [Description("Dumps Cosmere combat state.")]
    private static void CosmereCombat_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereCombat.TickIdle(pm);
        var state = CosmereCombat.GetOrCreateState(pm);
        pm.SendMessage(
            $"InCombat={state.InCombat} Scene={state.SceneId} RecoverUsed={state.RecoverUsedThisScene} " +
            $"Reaction={state.ReactionAvailable} DodgeArmed={state.DodgeArmed} Braced={state.Braced} " +
            $"AutoGraze={CosmereConfig.AutoGraze} AutoDodge={CosmereConfig.AutoDodge}"
        );
    }

    [Usage("CosmereItem")]
    [Description("Target equipment to print its Cosmere weapon/armor profile.")]
    private static void CosmereItem_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target an item to inspect its Cosmere profile.");
        e.Mobile.Target = new CosmereItemTarget();
    }

    [Usage("CosmereBrace")]
    [Description("Brace behind a Defensive weapon while in combat (attacks against you gain disadvantage).")]
    private static void CosmereBrace_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereCombat.TickIdle(pm);
        var state = CosmereCombat.GetOrCreateState(pm);
        if (!state.InCombat)
        {
            pm.SendMessage("You can only Brace during a combat scene.");
            return;
        }

        pm.EnsureCosmereSheet();
        var weapon = pm.Weapon as BaseWeapon;
        var profile = CosmereEquipment.ResolveWeaponFor(pm, weapon);
        if (!profile.Has(CosmereWeaponTraits.Defensive))
        {
            // Shields often sit on TwoHanded while fists/weapon remain "Weapon"
            if (pm.FindItemOnLayer(Layer.TwoHanded) is BaseWeapon shield)
            {
                profile = CosmereEquipment.ResolveWeaponFor(pm, shield);
            }
        }

        if (!profile.Has(CosmereWeaponTraits.Defensive))
        {
            pm.SendMessage("You need a Defensive weapon (e.g. a shield) to Brace.");
            return;
        }

        state.Braced = true;
        state.Touch();
        pm.SendMessage("You Brace. Attacks against you gain disadvantage until you Strike or move.");
    }

    [Usage("CosmereDraw")]
    [Description("Draw weapon stub: free if Quickdraw, otherwise message only (no action budget yet).")]
    private static void CosmereDraw_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.EnsureCosmereSheet();
        var weapon = pm.Weapon as BaseWeapon;
        var profile = CosmereEquipment.ResolveWeaponFor(pm, weapon);
        if (profile.Has(CosmereWeaponTraits.Quickdraw))
        {
            pm.SendMessage($"You draw your {profile.CatalogId} (Quickdraw — free).");
            return;
        }

        pm.SendMessage($"You draw your {profile.CatalogId} (no action budget in hybrid combat yet).");
    }

    [Usage("CosmereOffhandStrike")]
    [Description("Debug: spend Offhand Focus cost and Strike with the off-hand weapon.")]
    private static void CosmereOffhandStrike_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        var offhand = FindOffhandWeapon(pm);
        if (offhand == null)
        {
            pm.SendMessage("No off-hand weapon equipped.");
            return;
        }

        pm.EnsureCosmereSheet();
        var cost = CosmereEquipment.GetOffhandStrikeFocusCost(offhand, pm.CosmereSheet.Strength);
        if (!CosmerePools.SpendFocus(pm, cost))
        {
            pm.SendMessage($"Not enough Focus for off-hand Strike (cost {cost}).");
            return;
        }

        pm.SendMessage($"Off-hand Strike Focus cost: {cost}. Target a mobile.");
        pm.Target = new CosmereStrikeTarget(pm, offhand);
    }

    [Usage("CosmereSnap <metal>")]
    [Description("GM: Misting Snap — grant nascent Allomantic metal (Bronze/Steel/Tin/Zinc).")]
    private static void CosmereSnap_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1 ||
            !CosmereAllomancy.TryParseMetal(e.GetString(0), out var metal))
        {
            e.Mobile.SendMessage("Usage: CosmereSnap <Bronze|Steel|Tin|Zinc>");
            return;
        }

        CosmereAllomancy.TrySnap(pm, metal);
    }

    [Usage("CosmereUnlockPower")]
    [Description("GM: Unlock nascent Allomantic power to full.")]
    private static void CosmereUnlockPower_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereAllomancy.TryUnlockPower(pm);
    }

    [Usage("CosmereDrink")]
    [Description("Drink a metal vial (or virtual mixed vial if RequireVials is false).")]
    private static void CosmereDrink_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereMetalFlags metals = CosmereMetalFlags.None;
        if (pm.Backpack != null)
        {
            foreach (var item in pm.Backpack.Items)
            {
                if (item is CosmereMetalVial vial)
                {
                    metals = vial.Metals;
                    if (CosmereAllomancy.TryDrink(pm, metals, consumeItem: true))
                    {
                        vial.Consume();
                    }

                    return;
                }
            }
        }

        if (CosmereConfig.RequireVials)
        {
            pm.SendMessage("You need a Cosmere metal vial in your backpack.");
            return;
        }

        CosmereAllomancy.TryDrink(pm, CosmereMetalFlags.AllStarter, consumeItem: false);
    }

    [Usage("CosmereBurn <metal> [spend]")]
    [Description("Start burning your Allomantic metal (nascent or full).")]
    private static void CosmereBurn_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1 ||
            !CosmereAllomancy.TryParseMetal(e.GetString(0), out var metal))
        {
            e.Mobile.SendMessage("Usage: CosmereBurn <metal> [spend]");
            return;
        }

        var spend = e.Length > 1 ? Math.Max(1, e.GetInt32(1)) : 1;
        if (metal == CosmereAllomanticMetal.Zinc)
        {
            pm.SendMessage("Use CosmereRiot <emotion> [spend] for Zinc.");
            return;
        }

        CosmereAllomancy.TryBurn(pm, metal, spend);
    }

    [Usage("CosmereMaintain <metal> [spend]")]
    [Description("Maintain an active Allomantic burn.")]
    private static void CosmereMaintain_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1 ||
            !CosmereAllomancy.TryParseMetal(e.GetString(0), out var metal))
        {
            e.Mobile.SendMessage("Usage: CosmereMaintain <metal> [spend]");
            return;
        }

        var spend = e.Length > 1 ? Math.Max(1, e.GetInt32(1)) : 1;
        CosmereAllomancy.TryMaintain(pm, metal, spend);
    }

    [Usage("CosmerePush [spend]")]
    [Description("Steelpush Propel — target self, item, or mobile.")]
    private static void CosmerePush_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        var spend = e.Length > 0 ? Math.Max(1, e.GetInt32(0)) : 1;
        pm.SendMessage("Target for Steelpush (or target yourself).");
        pm.Target = new CosmerePushTarget(pm, spend);
    }

    [Usage("CosmereCoin [spend]")]
    [Description("Shoot Coin — ranged Allomancy Impact attack.")]
    private static void CosmereCoin_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        var spend = e.Length > 0 ? Math.Max(1, e.GetInt32(0)) : 1;
        pm.SendMessage("Target a mobile for Shoot Coin.");
        pm.Target = new CosmereCoinTarget(pm, spend);
    }

    [Usage("CosmereSeek [identify]")]
    [Description("Bronze Seek — pin Allomantic pulses. Optional identify (DC 15).")]
    private static void CosmereSeek_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        var identify = e.Length > 0 &&
                       (e.GetString(0).Equals("identify", StringComparison.OrdinalIgnoreCase) ||
                        e.GetString(0) == "1");
        CosmereAllomancy.TrySeek(pm, identify);
    }

    [Usage("CosmereRiot <emotion> [spend] [stealth]")]
    [Description("Zinc Riot an emotion in a 10 ft area.")]
    private static void CosmereRiot_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1)
        {
            e.Mobile.SendMessage("Usage: CosmereRiot <emotion> [spend] [stealth]");
            return;
        }

        var emotion = e.GetString(0);
        var spend = e.Length > 1 ? Math.Max(1, e.GetInt32(1)) : 1;
        var stealth = e.Length > 2 &&
                      (e.GetString(2).Equals("stealth", StringComparison.OrdinalIgnoreCase) ||
                       e.GetString(2) == "1");
        CosmereAllomancy.TryRiot(pm, emotion, spend, stealth);
    }

    [Usage("CosmereAllomancy")]
    [Description("Dump Allomancy / Investiture status.")]
    private static void CosmereAllomancy_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.SendMessage(CosmereAllomancy.DescribeStatus(pm));
    }

    [Usage("CosmereTalk [formal]")]
    [Description("Begin a Cosmere conversation scene. Optional: formal.")]
    private static void CosmereTalk_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        var formal = e.Length > 0 && e.GetString(0).Equals("formal", StringComparison.OrdinalIgnoreCase);
        pm.SendMessage(formal ? "Target someone to include (or yourself)." : "Target someone to include (or cancel for solo).");
        pm.Target = new CosmereTalkTarget(pm, formal);
    }

    [Usage("CosmereEndTalk")]
    [Description("End your Cosmere conversation scene.")]
    private static void CosmereEndTalk_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereSocial.EndTalk(pm);
    }

    [Usage("CosmereInfluence <skill> [normal|strong|overwhelm]")]
    [Description("Social influence test against a target.")]
    private static void CosmereInfluence_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm || e.Length < 1 ||
            !CosmereSkills.TryParse(e.GetString(0), out var skill))
        {
            e.Mobile.SendMessage("Usage: CosmereInfluence <skill> [normal|strong|overwhelm]");
            return;
        }

        var persuasiveness = CosmerePersuasiveness.Normal;
        if (e.Length > 1 && !CosmereSocial.TryParsePersuasiveness(e.GetString(1), out persuasiveness))
        {
            e.Mobile.SendMessage("Persuasiveness: normal, strong, or overwhelm.");
            return;
        }

        pm.SendMessage("Target a mobile to influence.");
        pm.Target = new CosmereInfluenceTarget(pm, skill, persuasiveness);
    }

    [Usage("CosmereResist")]
    [Description("Arm Focus resist for the next successful influence against you.")]
    private static void CosmereResist_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereSocial.ArmResist(pm);
    }

    [Usage("CosmereHelp")]
    [Description("Grant +1 advantage on an ally's next influence roll.")]
    private static void CosmereHelp_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.SendMessage("Target an ally to help.");
        pm.Target = new CosmereHelpHinderTarget(pm, help: true);
    }

    [Usage("CosmereHinder")]
    [Description("Grant +1 disadvantage on a target's next influence roll.")]
    private static void CosmereHinder_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.SendMessage("Target someone to hinder.");
        pm.Target = new CosmereHelpHinderTarget(pm, help: false);
    }

    [Usage("CosmereBolster [amount]")]
    [Description("Recover Focus on a target (default 1).")]
    private static void CosmereBolster_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        var amount = e.Length > 0 ? Math.Max(1, e.GetInt32(0)) : 1;
        pm.SendMessage($"Target someone to bolster (+{amount} Focus).");
        pm.Target = new CosmereBolsterTarget(pm, amount);
    }

    [Usage("CosmereSocialOptIn")]
    [Description("Toggle opt-in for PvP social influence.")]
    private static void CosmereSocialOptIn_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        var state = CosmereSocial.GetOrCreateConversation(pm);
        state.SocialOptIn = !state.SocialOptIn;
        pm.SendMessage($"Social PvP opt-in: {(state.SocialOptIn ? "ON" : "OFF")}.");
    }

    [Usage("CosmereNpcFocus [max N|set N|dump]")]
    [Description("GM: set or dump NPC Focus timer.")]
    private static void CosmereNpcFocus_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target an NPC for Focus timer.");
        var mode = e.Length > 0 ? e.GetString(0) : "dump";
        var value = e.Length > 1 ? Math.Max(0, e.GetInt32(1)) : -1;
        e.Mobile.Target = new CosmereNpcFocusTarget(mode, value);
    }

    [Usage("CosmereSocial")]
    [Description("Dump Cosmere conversation / social status.")]
    private static void CosmereSocial_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.SendMessage(CosmereSocial.Describe(pm));
    }

    [Usage("CosmereLevelUp [to]")]
    [Description("GM: Level up +1 or to a target level. Use 'force' to exceed MaxLevel.")]
    private static void CosmereLevelUp_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            e.Mobile.SendMessage("Target a player or use on yourself.");
            return;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var force = false;
        var target = sheet.Level + 1;

        for (var i = 0; i < e.Length; i++)
        {
            var arg = e.GetString(i);
            if (arg.Equals("force", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("beyond", StringComparison.OrdinalIgnoreCase))
            {
                force = true;
                continue;
            }

            if (int.TryParse(arg, out var n) && n > 0)
            {
                target = n;
            }
        }

        CosmereAdvancement.ApplyLevelUp(pm, target, allowBeyondMax: force);
    }

    [Usage("CosmereSpendAttr <attr>")]
    [Description("Spend a pending attribute point (Str/Spd/Int/Wil/Awa/Pre).")]
    private static void CosmereSpendAttr_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        if (e.Length < 1 || !CosmereAdvancement.TryParseAttribute(e.GetString(0), out var attr))
        {
            e.Mobile.SendMessage("Usage: CosmereSpendAttr <Strength|Speed|Intellect|Willpower|Awareness|Presence>");
            return;
        }

        CosmereAdvancement.SpendAttr(pm, attr);
    }

    [Usage("CosmereSpendSkill <skill>")]
    [Description("Spend a pending skill rank.")]
    private static void CosmereSpendSkill_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        if (e.Length < 1 || !CosmereSkills.TryParse(e.GetString(0), out var skill))
        {
            e.Mobile.SendMessage("Usage: CosmereSpendSkill <skill>");
            return;
        }

        CosmereAdvancement.SpendSkill(pm, skill);
    }

    [Usage("CosmereGrantTalent <id> [force]")]
    [Description("GM: Spend a talent pick (or force-grant) an opaque talent id.")]
    private static void CosmereGrantTalent_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        if (e.Length < 1)
        {
            e.Mobile.SendMessage("Usage: CosmereGrantTalent <id> [force]");
            return;
        }

        var force = e.Length > 1 && e.GetString(1).Equals("force", StringComparison.OrdinalIgnoreCase);
        CosmereAdvancement.GrantTalent(pm, e.GetString(0), force);
    }

    [Usage("CosmereGoal assign|complete|fail|list …")]
    [Description("GM: manage personal Cosmere goals.")]
    private static void CosmereGoal_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        if (e.Length < 1)
        {
            e.Mobile.SendMessage(
                "Usage: CosmereGoal list | complete <id> | fail <id> | assign <id> <title> [reward] [payload]"
            );
            return;
        }

        var verb = e.GetString(0);
        if (verb.Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            CosmereGoals.List(pm);
            return;
        }

        if (verb.Equals("complete", StringComparison.OrdinalIgnoreCase))
        {
            if (e.Length < 2)
            {
                e.Mobile.SendMessage("Usage: CosmereGoal complete <id>");
                return;
            }

            CosmereGoals.Complete(pm, e.GetString(1));
            return;
        }

        if (verb.Equals("fail", StringComparison.OrdinalIgnoreCase))
        {
            if (e.Length < 2)
            {
                e.Mobile.SendMessage("Usage: CosmereGoal fail <id>");
                return;
            }

            CosmereGoals.Fail(pm, e.GetString(1));
            return;
        }

        if (verb.Equals("assign", StringComparison.OrdinalIgnoreCase))
        {
            if (e.Length < 3)
            {
                e.Mobile.SendMessage("Usage: CosmereGoal assign <id> <title> [rewardKind] [payload]");
                return;
            }

            var id = e.GetString(1);
            var title = e.GetString(2);
            var kind = CosmereGoalRewardKind.None;
            string payload = null;
            if (e.Length > 3)
            {
                CosmereGoals.TryParseRewardKind(e.GetString(3), out kind);
            }

            if (e.Length > 4)
            {
                payload = e.GetString(4);
            }

            CosmereGoals.Assign(pm, id, title, kind, payload);
            return;
        }

        e.Mobile.SendMessage(
            "Usage: CosmereGoal list | complete <id> | fail <id> | assign <id> <title> [reward] [payload]"
        );
    }

    [Usage("CosmereAdvancement")]
    [Description("Dump level, pending pools, health bonus, talents, and goals.")]
    private static void CosmereAdvancement_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        pm.SendMessage(
            $"Level {sheet.Level} (max skill rank {sheet.MaxSkillRank}). " +
            $"HealthBonus {sheet.LevelHealthBonus} MaxHealth {sheet.MaxHealth}. " +
            $"Pending attr {sheet.PendingAttrPoints} / skills {sheet.PendingSkillRanks} / talents {sheet.PendingTalentPicks}."
        );
        pm.SendMessage($"Talents ({sheet.Talents.Count}): {(sheet.Talents.Count == 0 ? "(none)" : string.Join(", ", sheet.Talents))}");
        pm.SendMessage(CosmereProfession.Describe(sheet));
        CosmereGoals.List(pm);
    }

    [Usage("CosmereDebug")]
    [Description("Toggle session Cosmere debug SysMessages for this character.")]
    private static void CosmereDebug_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        if (!CosmereConfig.DebugEnabled)
        {
            pm.SendMessage("Cosmere debug is disabled on this shard.");
            return;
        }

        pm.CosmereDebug = !pm.CosmereDebug;
        pm.SendMessage(pm.CosmereDebug ? "Cosmere debug ON." : "Cosmere debug OFF.");
    }

    [Usage("CosmereTime")]
    [Description("Shows the current Cosmere day period and UO clock hours.")]
    private static void CosmereTime_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        if (!CosmereConfig.DayPeriodsEnabled)
        {
            pm.SendMessage("Cosmere day periods are disabled on this shard.");
            return;
        }

        Clock.GetTime(pm.Map, pm.X, pm.Y, out var hours, out int minutes);
        var period = CosmereTime.GetPeriod(hours);
        pm.SendMessage(
            $"Cosmere time: {CosmereTime.Describe(period)} ({hours:D2}:{minutes:D2})."
        );
    }

    [Usage("CosmereProfession [assign|xp|setrank …]")]
    [Description("Dump or manage the parallel Profession track (Artisan/Producer/Merchant).")]
    private static void CosmereProfession_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.EnsureCosmereSheet();

        if (e.Length == 0)
        {
            pm.SendMessage(CosmereProfession.Describe(pm.CosmereSheet));
            return;
        }

        var verb = e.GetString(0);
        if (verb.Equals("assign", StringComparison.OrdinalIgnoreCase))
        {
            if (e.Length < 2 || !CosmereProfession.TryParse(e.GetString(1), out var id))
            {
                pm.SendMessage("Usage: CosmereProfession assign <Artisan|Producer|Merchant|none>");
                return;
            }

            CosmereProfession.Assign(pm, id);
            return;
        }

        if (verb.Equals("xp", StringComparison.OrdinalIgnoreCase))
        {
            if (e.Mobile.AccessLevel < AccessLevel.GameMaster || e.Length < 2)
            {
                pm.SendMessage("Usage (GM): CosmereProfession xp <amount>");
                return;
            }

            CosmereProfession.AddXp(pm, Math.Max(0, e.GetInt32(1)), "GM grant");
            return;
        }

        if (verb.Equals("setrank", StringComparison.OrdinalIgnoreCase))
        {
            if (e.Mobile.AccessLevel < AccessLevel.GameMaster || e.Length < 2)
            {
                pm.SendMessage("Usage (GM): CosmereProfession setrank <0-5>");
                return;
            }

            CosmereProfession.SetRank(pm, e.GetInt32(1));
            return;
        }

        pm.SendMessage("Usage: CosmereProfession | assign <id> | xp <n> | setrank <n>");
    }

    [Usage("CosmereGather")]
    [Description("Stub Producer gather action — awards Profession XP if Profession is Producer.")]
    private static void CosmereGather_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereProfession.TryProfessionAction(pm, CosmereProfessionId.Producer, "Gather");
    }

    [Usage("CosmereCraft")]
    [Description("Stub Artisan craft action — awards Profession XP if Profession is Artisan.")]
    private static void CosmereCraft_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereProfession.TryProfessionAction(pm, CosmereProfessionId.Artisan, "Craft");
    }

    [Usage("CosmereTrade")]
    [Description("Stub Merchant trade action — awards Profession XP if Profession is Merchant.")]
    private static void CosmereTrade_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        CosmereProfession.TryProfessionAction(pm, CosmereProfessionId.Merchant, "Trade");
    }

    private static BaseWeapon FindOffhandWeapon(PlayerMobile pm)
    {
        var main = pm.Weapon as BaseWeapon;
        var one = pm.FindItemOnLayer(Layer.OneHanded) as BaseWeapon;
        var two = pm.FindItemOnLayer(Layer.TwoHanded) as BaseWeapon;

        if (one != null && one != main && one is not Fists)
        {
            return one;
        }

        if (two != null && two != main && two is not Fists && two is not BaseRanged)
        {
            return two;
        }

        return null;
    }

    private class CosmerePushTarget : Target
    {
        private readonly PlayerMobile _pm;
        private readonly int _spend;

        public CosmerePushTarget(PlayerMobile pm, int spend) : base(12, false, TargetFlags.None)
        {
            _pm = pm;
            _spend = spend;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is IEntity entity)
            {
                CosmereAllomancy.TrySteelpush(_pm, entity, _spend);
                return;
            }

            from.SendMessage("Invalid Steelpush target.");
        }
    }

    private class CosmereCoinTarget : Target
    {
        private readonly PlayerMobile _pm;
        private readonly int _spend;

        public CosmereCoinTarget(PlayerMobile pm, int spend) : base(12, false, TargetFlags.Harmful)
        {
            _pm = pm;
            _spend = spend;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile defender)
            {
                from.SendMessage("Must target a mobile.");
                return;
            }

            if (!_pm.HarmfulCheck(defender))
            {
                return;
            }

            CosmereAllomancy.TryShootCoin(_pm, defender, _spend);
        }
    }

    private class CosmereItemTarget : Target
    {
        public CosmereItemTarget() : base(12, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Item item)
            {
                from.SendMessage("Must target an item.");
                return;
            }

            if (item is BaseWeapon weapon)
            {
                var strength = 1;
                if (from is PlayerMobile pm)
                {
                    pm.EnsureCosmereSheet();
                    strength = pm.CosmereSheet.Strength;
                }

                var profile = CosmereItemCatalog.ResolveWeapon(weapon, strength);
                from.SendMessage(
                    $"{item.GetType().Name}: {profile.CatalogId} {profile.DescribeDamage()} {profile.DamageType} " +
                    $"{CosmereSkills.GetDisplayName(profile.AttackSkill)} traits={profile.Traits}"
                );
                return;
            }

            if (CosmereItemCatalog.TryResolveArmorItem(item, out var armor))
            {
                from.SendMessage(
                    $"{item.GetType().Name}: {armor.CatalogId} Deflect={armor.Deflect} " +
                    $"Cumbersome={armor.Cumbersome} Presentable={armor.Presentable}"
                );
                return;
            }

            from.SendMessage("No Cosmere weapon/armor profile for that item.");
        }
    }

    private class CosmereStrikeTarget : Target
    {
        private readonly PlayerMobile _attacker;
        private readonly BaseWeapon _weaponOverride;

        public CosmereStrikeTarget(PlayerMobile attacker, BaseWeapon weaponOverride = null) :
            base(12, false, TargetFlags.Harmful)
        {
            _attacker = attacker;
            _weaponOverride = weaponOverride;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile defender)
            {
                from.SendMessage("Must target a mobile.");
                return;
            }

            if (!_attacker.HarmfulCheck(defender))
            {
                return;
            }

            var weapon = _weaponOverride ?? _attacker.Weapon as BaseWeapon;
            CosmereCombat.TryResolveSwing(_attacker, defender, weapon);
        }
    }

    private class CosmereTalkTarget : Target
    {
        private readonly PlayerMobile _pm;
        private readonly bool _formal;

        public CosmereTalkTarget(PlayerMobile pm, bool formal) : base(12, false, TargetFlags.None)
        {
            _pm = pm;
            _formal = formal;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Mobile m)
            {
                CosmereSocial.BeginTalk(_pm, _formal, m);
                return;
            }

            CosmereSocial.BeginTalk(_pm, _formal);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType) =>
            CosmereSocial.BeginTalk(_pm, _formal);
    }

    private class CosmereInfluenceTarget : Target
    {
        private readonly PlayerMobile _pm;
        private readonly CosmereSkill _skill;
        private readonly CosmerePersuasiveness _persuasiveness;

        public CosmereInfluenceTarget(PlayerMobile pm, CosmereSkill skill, CosmerePersuasiveness persuasiveness) :
            base(12, false, TargetFlags.None)
        {
            _pm = pm;
            _skill = skill;
            _persuasiveness = persuasiveness;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile target)
            {
                from.SendMessage("Must target a mobile.");
                return;
            }

            CosmereSocial.TryInfluence(_pm, target, _skill, _persuasiveness);
        }
    }

    private class CosmereHelpHinderTarget : Target
    {
        private readonly PlayerMobile _pm;
        private readonly bool _help;

        public CosmereHelpHinderTarget(PlayerMobile pm, bool help) : base(12, false, TargetFlags.None)
        {
            _pm = pm;
            _help = help;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not PlayerMobile ally)
            {
                from.SendMessage("Must target a player.");
                return;
            }

            CosmereSocial.TryHelpOrHinder(_pm, ally, _help);
        }
    }

    private class CosmereBolsterTarget : Target
    {
        private readonly PlayerMobile _pm;
        private readonly int _amount;

        public CosmereBolsterTarget(PlayerMobile pm, int amount) : base(12, false, TargetFlags.None)
        {
            _pm = pm;
            _amount = amount;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not PlayerMobile target)
            {
                from.SendMessage("Must target a player.");
                return;
            }

            CosmereSocial.TryBolster(_pm, target, _amount);
        }
    }

    private class CosmereNpcFocusTarget : Target
    {
        private readonly string _mode;
        private readonly int _value;

        public CosmereNpcFocusTarget(string mode, int value) : base(12, false, TargetFlags.None)
        {
            _mode = mode;
            _value = value;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile m || m is PlayerMobile)
            {
                from.SendMessage("Must target an NPC.");
                return;
            }

            var state = CosmereSocial.GetOrCreateNpc(m);
            if (_mode.Equals("max", StringComparison.OrdinalIgnoreCase) && _value >= 0)
            {
                state.MaxFocus = _value;
                state.Focus = Math.Min(state.Focus, state.MaxFocus);
                state.Conceded = state.Focus <= 0;
                from.SendMessage($"{m.Name} MaxFocus={state.MaxFocus}, Focus={state.Focus}.");
                return;
            }

            if (_mode.Equals("set", StringComparison.OrdinalIgnoreCase) && _value >= 0)
            {
                state.Focus = Math.Min(_value, state.MaxFocus);
                state.Conceded = state.Focus <= 0;
                from.SendMessage($"{m.Name} Focus={state.Focus}/{state.MaxFocus} Conceded={state.Conceded}.");
                return;
            }

            from.SendMessage(
                $"{m.Name}: Focus {state.Focus}/{state.MaxFocus} Conceded={state.Conceded}"
            );
        }
    }
}
