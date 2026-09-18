using System;
using System.Collections.Generic;
using Server.Engines.PartySystem;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public static class CosmereSocial
{
    private static int _nextSceneId = 1;
    private static readonly Dictionary<Serial, CosmereNpcSocialState> _npcStates = new();

    public const int InfluenceRange = 8;

    public static CosmereConversationState GetOrCreateConversation(PlayerMobile pm)
    {
        pm.CosmereConversation ??= new CosmereConversationState();
        return pm.CosmereConversation;
    }

    public static CosmereNpcSocialState GetOrCreateNpc(Mobile npc)
    {
        if (npc == null || npc is PlayerMobile)
        {
            return null;
        }

        if (!_npcStates.TryGetValue(npc.Serial, out var state))
        {
            var max = CosmereConfig.DefaultNpcMaxFocus;
            state = new CosmereNpcSocialState
            {
                MaxFocus = max,
                Focus = max,
                Conceded = false
            };
            _npcStates[npc.Serial] = state;
        }

        return state;
    }

    public static void ClearNpcState(Serial serial) => _npcStates.Remove(serial);

    public static int GetResistFocusCost(CosmerePersuasiveness persuasiveness) =>
        persuasiveness switch
        {
            CosmerePersuasiveness.Strong => 4,
            CosmerePersuasiveness.Overwhelm => 0,
            _ => 2
        };

    public static CosmereSocialDefense GetDefaultDefense(CosmereSkill skill) =>
        skill switch
        {
            CosmereSkill.Insight or CosmereSkill.Deduction => CosmereSocialDefense.Cognitive,
            CosmereSkill.Persuasion or CosmereSkill.Deception or CosmereSkill.Leadership
                or CosmereSkill.Intimidation => CosmereSocialDefense.Spiritual,
            _ => CosmereSocialDefense.Spiritual
        };

    public static int GetDefenseDc(Mobile target, CosmereSocialDefense defense)
    {
        if (target is PlayerMobile pm)
        {
            pm.EnsureCosmereSheet();
            return defense == CosmereSocialDefense.Cognitive
                ? pm.CosmereSheet.CognitiveDefense
                : pm.CosmereSheet.SpiritualDefense;
        }

        return defense == CosmereSocialDefense.Cognitive
            ? CosmereConfig.NpcCognitiveDefense
            : CosmereConfig.NpcSpiritualDefense;
    }

    public static bool IsPresentable(PlayerMobile pm) =>
        CosmereEquipment.TryGetActiveArmor(pm, out var armor) && armor.Presentable;

    public static bool TryResistInfluence(Mobile target, int focusCost, out bool resisted)
    {
        resisted = false;

        if (focusCost <= 0)
        {
            // Auto-concede: not resisted
            if (target is not PlayerMobile && GetOrCreateNpc(target) is { } npcState)
            {
                npcState.Conceded = true;
                npcState.Focus = 0;
            }

            return true;
        }

        if (target is PlayerMobile pm)
        {
            pm.EnsureCosmereSheet();
            if (pm.CosmereSheet.Focus < focusCost)
            {
                return true; // handled, but not resisted
            }

            if (!CosmerePools.SpendFocus(pm, focusCost))
            {
                return true;
            }

            resisted = true;
            return true;
        }

        var state = GetOrCreateNpc(target);
        if (state == null)
        {
            return false;
        }

        if (state.Focus < focusCost)
        {
            state.Conceded = true;
            state.Focus = 0;
            return true;
        }

        state.Focus -= focusCost;
        state.Clamp();
        resisted = true;
        if (state.Focus <= 0)
        {
            state.Conceded = true;
            state.Focus = 0;
        }

        return true;
    }

    public static void TickIdle(PlayerMobile pm)
    {
        var state = pm.CosmereConversation;
        if (state is not { InConversation: true })
        {
            return;
        }

        var idle = CosmereConfig.SocialIdleSeconds;
        if (idle <= 0)
        {
            return;
        }

        if (Core.Now - state.LastActivity >= TimeSpan.FromSeconds(idle))
        {
            EndTalk(pm, idleMessage: true);
        }
    }

    public static bool BeginTalk(PlayerMobile pm, bool formal, Mobile joinTarget = null)
    {
        if (!CosmereConfig.SocialEnabled)
        {
            pm.SendMessage("Cosmere social is disabled on this shard.");
            return false;
        }

        var state = GetOrCreateConversation(pm);
        if (!state.InConversation)
        {
            state.Begin(_nextSceneId++, formal);
        }
        else
        {
            state.Formal = formal || state.Formal;
            state.Touch();
        }

        pm.SendMessage(
            formal
                ? $"Conversation begun (formal, scene {state.SceneId})."
                : $"Conversation begun (scene {state.SceneId})."
        );

        if (joinTarget is PlayerMobile otherPm)
        {
            var other = GetOrCreateConversation(otherPm);
            if (!other.InConversation)
            {
                other.Begin(state.SceneId, formal);
            }
            else
            {
                other.SceneId = state.SceneId;
                other.Formal = formal || other.Formal;
                other.Touch();
            }

            otherPm.SendMessage($"{pm.Name} brought you into a Cosmere conversation (scene {state.SceneId}).");
            pm.SendMessage($"{otherPm.Name} joined the conversation.");
        }
        else if (joinTarget != null)
        {
            GetOrCreateNpc(joinTarget);
            pm.SendMessage($"{joinTarget.Name} is in the conversation (NPC Focus timer ready).");
        }

        return true;
    }

    public static void EndTalk(PlayerMobile pm, bool idleMessage = false)
    {
        var state = pm.CosmereConversation;
        if (state is not { InConversation: true })
        {
            if (!idleMessage)
            {
                pm.SendMessage("You are not in a Cosmere conversation.");
            }

            return;
        }

        pm.EnsureCosmereSheet();
        pm.CosmereSheet.ClearCondition(CosmereConditionKind.Influenced);
        state.End();
        pm.SendMessage(idleMessage ? "Conversation ended (idle)." : "Conversation ended.");
    }

    public static bool CanInfluenceTarget(PlayerMobile from, Mobile target, out string reason)
    {
        reason = null;

        if (!CosmereConfig.SocialEnabled)
        {
            reason = "Social disabled";
            return false;
        }

        TickIdle(from);
        var conv = from.CosmereConversation;
        if (conv is not { InConversation: true } && from.AccessLevel < AccessLevel.GameMaster)
        {
            reason = "Start a conversation with CosmereTalk first";
            return false;
        }

        if (target == null || target == from || !target.Alive)
        {
            reason = "Invalid target";
            return false;
        }

        if (from.Map != target.Map || !from.InRange(target, InfluenceRange))
        {
            reason = "Out of influence range";
            return false;
        }

        if (target is PlayerMobile targetPm && CosmereConfig.SocialPvpOptIn)
        {
            var opted = targetPm.CosmereConversation?.SocialOptIn == true;
            var party = Party.Get(from);
            var sameParty = party != null && party.Contains(targetPm);
            if (!opted && !sameParty && from.AccessLevel < AccessLevel.GameMaster)
            {
                reason = "Target has not opted into social PvP (CosmereSocialOptIn) and is not in your party";
                return false;
            }
        }

        return true;
    }

    public static bool TryInfluence(
        PlayerMobile from,
        Mobile target,
        CosmereSkill skill,
        CosmerePersuasiveness persuasiveness,
        CosmereSocialDefense? defenseOverride = null
    )
    {
        if (!CanInfluenceTarget(from, target, out var reason))
        {
            from.SendMessage(reason);
            return false;
        }

        from.EnsureCosmereSheet();
        var sheet = from.CosmereSheet;
        if (sheet.Dead || sheet.HasCondition(CosmereConditionKind.Unconscious))
        {
            from.SendMessage("Cannot influence while dead or unconscious.");
            return false;
        }

        var conv = GetOrCreateConversation(from);
        conv.Touch();

        // Conceded NPC: auto-succeed, no resist
        if (target is not PlayerMobile && GetOrCreateNpc(target) is { Conceded: true })
        {
            ApplyInfluenced(target, skill);
            conv.LastInfluenceSummary = $"auto vs {target.Name} (conceded)";
            from.SendMessage($"{target.Name} folds — influence succeeds without resist.");
            return true;
        }

        var defense = defenseOverride ?? GetDefaultDefense(skill);
        var dc = GetDefenseDc(target, defense);

        var adv = Math.Min(1, conv.NextTestAdv);
        var disadv = Math.Min(1, conv.NextTestDisadv);
        conv.NextTestAdv = 0;
        conv.NextTestDisadv = 0;

        if (conv.Formal && !IsPresentable(from))
        {
            disadv++;
        }

        var test = CosmereTestResolver.Resolve(sheet, skill, dc, adv, disadv);
        CosmereDebug.TraceTest(from, test);
        CosmereDebug.Trace(
            from,
            $"Influence context: defense={defense} adv={adv} disadv={disadv} formal={conv.Formal} persuasiveness={persuasiveness}"
        );
        if (test.Blocked)
        {
            from.SendMessage($"Influence blocked: {test.BlockReason}.");
            return true;
        }

        var summary =
            $"{CosmereSkills.GetDisplayName(skill)} vs {defense} DC {dc}: d20={test.D20}+{test.Modifier}={test.Total} → {(test.Success ? "SUCCESS" : "FAILURE")}";
        conv.LastInfluenceSummary = summary;
        from.SendMessage($"Influence {summary}");

        if (!test.Success)
        {
            return true;
        }

        var cost = GetResistFocusCost(persuasiveness);
        ResolveResistAfterSuccess(from, target, skill, cost, persuasiveness);
        return true;
    }

    private static void ResolveResistAfterSuccess(
        PlayerMobile from,
        Mobile target,
        CosmereSkill skill,
        int cost,
        CosmerePersuasiveness persuasiveness
    )
    {
        if (target is PlayerMobile targetPm)
        {
            targetPm.EnsureCosmereSheet();
            TickIdle(targetPm);
            var tConv = GetOrCreateConversation(targetPm);
            tConv.Touch();

            targetPm.SendMessage(
                $"{from.Name} successfully influenced you ({skill}). You are not forced to change your mind."
            );

            if (tConv.ResistArmed)
            {
                tConv.ResistArmed = false;
                TryResistInfluence(targetPm, cost, out var resisted);
                if (resisted)
                {
                    targetPm.SendMessage($"You resist (spent {cost} Focus).");
                    from.SendMessage($"{targetPm.Name} resists the influence.");
                    CosmereDebug.Trace(from, $"Resist: {targetPm.Name} spent {cost} Focus — influence blocked.");
                    CosmereDebug.Trace(
                        targetPm,
                        $"Resist success: cost={cost} Focus now {targetPm.CosmereSheet.Focus}/{targetPm.CosmereSheet.MaxFocus}"
                    );
                    return;
                }

                targetPm.SendMessage($"Could not resist (need {cost} Focus).");
                CosmereDebug.Trace(targetPm, $"Resist FAILED: need {cost}, have {targetPm.CosmereSheet.Focus}");
            }
            else
            {
                targetPm.SendMessage(
                    $"Use CosmereResist before the next influence to spend Focus ({cost} for {persuasiveness})."
                );
            }

            ApplyInfluenced(targetPm, skill);
            from.SendMessage($"{targetPm.Name} does not resist (narrative Influenced).");
            CosmereDebug.Trace(from, $"No resist from {targetPm.Name}; Influenced applied.");
            return;
        }

        // NPC
        var npcState = GetOrCreateNpc(target);
        if (cost <= 0 || persuasiveness == CosmerePersuasiveness.Overwhelm)
        {
            TryResistInfluence(target, 0, out _);
            ApplyInfluenced(target, skill);
            from.SendMessage($"{target.Name} is overwhelmed and concedes.");
            CosmereDebug.Trace(from, $"NPC {target.Name} Overwhelm — Focus {npcState.Focus}/{npcState.MaxFocus} Conceded={npcState.Conceded}");
            target.PublicOverheadMessage(
                MessageType.Regular,
                0x22,
                false,
                "*concedes*"
            );
            return;
        }

        if (CosmereConfig.NpcAutoResist && npcState.Focus >= cost)
        {
            TryResistInfluence(target, cost, out var resisted);
            if (resisted)
            {
                from.SendMessage(
                    $"{target.Name} resists ({cost} Focus). NPC Focus {npcState.Focus}/{npcState.MaxFocus}" +
                    (npcState.Conceded ? " — conceded." : ".")
                );
                CosmereDebug.Trace(
                    from,
                    $"NPC resist: cost={cost} Focus {npcState.Focus}/{npcState.MaxFocus} Conceded={npcState.Conceded}"
                );
                if (npcState.Conceded)
                {
                    target.PublicOverheadMessage(
                        MessageType.Regular,
                        0x22,
                        false,
                        "*too drained to argue*"
                    );
                }

                return;
            }
        }

        // Cannot or will not resist
        if (npcState.Focus < cost)
        {
            npcState.Conceded = true;
            npcState.Focus = 0;
        }

        ApplyInfluenced(target, skill);
        from.SendMessage(
            $"{target.Name} fails to resist. Focus {npcState.Focus}/{npcState.MaxFocus}" +
            (npcState.Conceded ? " — conceded." : ".")
        );
        CosmereDebug.Trace(
            from,
            $"NPC no/fail resist: cost={cost} Focus {npcState.Focus}/{npcState.MaxFocus} Conceded={npcState.Conceded}"
        );
    }

    public static void ApplyInfluenced(Mobile target, CosmereSkill skill)
    {
        var payload = CosmereSkills.GetDisplayName(skill);
        if (target is PlayerMobile pm)
        {
            pm.EnsureCosmereSheet();
            SetInfluenced(pm.CosmereSheet, payload);
            return;
        }

        // NPCs: overhead only (no sheet)
        target.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"*influenced ({payload})*");
    }

    private static void SetInfluenced(CosmereSheet sheet, string payload)
    {
        for (var i = 0; i < sheet.Conditions.Count; i++)
        {
            if (sheet.Conditions[i].Kind == CosmereConditionKind.Influenced)
            {
                sheet.Conditions[i].Payload = payload;
                sheet.Conditions[i].Source = "social";
                return;
            }
        }

        sheet.Conditions.Add(
            new CosmereCondition
            {
                Kind = CosmereConditionKind.Influenced,
                Payload = payload,
                Source = "social"
            }
        );
    }

    public static bool ArmResist(PlayerMobile pm)
    {
        TickIdle(pm);
        var state = GetOrCreateConversation(pm);
        if (!state.InConversation)
        {
            // Allow arming even out of conversation so they're ready
            BeginTalk(pm, formal: false);
        }

        state.ResistArmed = true;
        state.Touch();
        pm.SendMessage(
            "Resist armed: you will spend Focus on the next successful influence against you (never forced to change your mind)."
        );
        return true;
    }

    public static bool TryHelpOrHinder(PlayerMobile from, PlayerMobile ally, bool help)
    {
        TickIdle(from);
        var fromConv = GetOrCreateConversation(from);
        if (!fromConv.InConversation && from.AccessLevel < AccessLevel.GameMaster)
        {
            from.SendMessage("Start a conversation first.");
            return false;
        }

        if (ally == null || ally == from)
        {
            from.SendMessage("Invalid ally.");
            return false;
        }

        TickIdle(ally);
        var allyConv = GetOrCreateConversation(ally);
        if (!allyConv.InConversation)
        {
            allyConv.Begin(fromConv.InConversation ? fromConv.SceneId : _nextSceneId++, fromConv.Formal);
        }

        if (help)
        {
            allyConv.NextTestAdv = 1;
            allyConv.NextTestDisadv = 0;
            from.SendMessage($"You help {ally.Name} (+1 advantage on their next influence).");
            ally.SendMessage($"{from.Name} helps you (+1 advantage on your next influence).");
        }
        else
        {
            allyConv.NextTestDisadv = 1;
            from.SendMessage($"You hinder {ally.Name} (+1 disadvantage on their next influence).");
            ally.SendMessage($"{from.Name} hinders you (+1 disadvantage on your next influence).");
        }

        fromConv.Touch();
        allyConv.Touch();
        return true;
    }

    public static bool TryBolster(PlayerMobile from, PlayerMobile target, int amount)
    {
        amount = Math.Max(1, amount);
        if (target == null)
        {
            from.SendMessage("Invalid target.");
            return false;
        }

        CosmerePools.RecoverFocus(target, amount);
        from.SendMessage($"Bolstered {target.Name} (+{amount} Focus).");
        if (target != from)
        {
            target.SendMessage($"{from.Name} bolsters you (+{amount} Focus).");
        }

        return true;
    }

    public static string Describe(PlayerMobile pm)
    {
        TickIdle(pm);
        var conv = pm.CosmereConversation;
        var presentable = IsPresentable(pm);
        if (conv is not { InConversation: true })
        {
            return $"Social: out of conversation  Presentable={presentable}  OptIn={conv?.SocialOptIn == true}";
        }

        return
            $"Social: scene {conv.SceneId} Formal={conv.Formal} ResistArmed={conv.ResistArmed} " +
            $"Help={conv.NextTestAdv} Hinder={conv.NextTestDisadv} Presentable={presentable} " +
            $"OptIn={conv.SocialOptIn} Last=[{conv.LastInfluenceSummary ?? "-"}]";
    }

    public static bool TryParsePersuasiveness(string text, out CosmerePersuasiveness value)
    {
        value = CosmerePersuasiveness.Normal;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return Enum.TryParse(text.Trim(), true, out value);
    }
}
