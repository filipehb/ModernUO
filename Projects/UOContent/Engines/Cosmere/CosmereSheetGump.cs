using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public class CosmereSheetGump : DynamicGump
{
    private readonly PlayerMobile _player;

    public override bool Singleton => true;

    private CosmereSheetGump(PlayerMobile player) : base(50, 50) => _player = player;

    public static void DisplayTo(Mobile from)
    {
        if (from?.NetState == null || from is not PlayerMobile pm)
        {
            return;
        }

        pm.EnsureCosmereSheet();
        from.SendGump(new CosmereSheetGump(pm));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var sheet = _player.CosmereSheet;
        const int width = 520;
        const int height = 860;

        builder.AddPage();
        builder.AddBackground(0, 0, width, height, 0x13BE);
        builder.AddImageTiled(10, 10, width - 20, 25, 0xA40);
        builder.AddHtml(15, 12, width - 30, 20, "<CENTER>Cosmere Sheet</CENTER>");

        var y = 45;
        builder.AddLabel(20, y, 0x480, $"Level {sheet.Level}  MaxRank {sheet.MaxSkillRank}{(sheet.Dead ? "  [DEAD]" : "")}");
        y += 18;
        builder.AddLabel(
            20,
            y,
            0x481,
            $"Pending Attr {sheet.PendingAttrPoints}  Skills {sheet.PendingSkillRanks}  Talents {sheet.PendingTalentPicks}"
        );
        y += 18;
        builder.AddLabel(
            20,
            y,
            0x481,
            $"Hunger {_player.Hunger}/20  Thirst {_player.Thirst}/20  Time {CosmereTime.Describe(CosmereTime.GetPeriod(_player))}"
        );
        y += 18;
        if (sheet.ProfessionId == CosmereProfessionId.None)
        {
            builder.AddLabel(20, y, 0x481, "Profession: (none)");
        }
        else
        {
            var nextXp = sheet.ProfessionRank >= CosmereProfession.MaxRank
                ? "max"
                : $"{sheet.ProfessionXp}/{CosmereProfession.GetXpToReachRank(sheet.ProfessionRank + 1)}";
            builder.AddLabel(
                20,
                y,
                0x481,
                $"Profession {sheet.ProfessionId} R{sheet.ProfessionRank} Tier {CosmereProfession.GetRecipeTier(sheet.ProfessionRank)} XP {nextXp}"
            );
        }

        y += 22;

        builder.AddLabel(20, y, 0x480, "Attributes");
        y += 20;
        builder.AddLabel(30, y, 0x481, $"Str {sheet.Strength}  Spd {sheet.Speed}  Int {sheet.Intellect}");
        y += 18;
        builder.AddLabel(30, y, 0x481, $"Wil {sheet.Willpower}  Awa {sheet.Awareness}  Pre {sheet.Presence}");
        y += 22;

        builder.AddLabel(20, y, 0x480, "Defenses");
        y += 20;
        builder.AddLabel(
            30,
            y,
            0x481,
            $"Phys {sheet.PhysicalDefense}  Cog {sheet.CognitiveDefense}  Spi {sheet.SpiritualDefense}"
        );
        y += 22;

        builder.AddLabel(20, y, 0x480, "Pools");
        y += 20;
        builder.AddLabel(
            30,
            y,
            0x481,
            $"Health {sheet.Health}/{sheet.MaxHealth} (+{sheet.LevelHealthBonus})  Focus {sheet.Focus}/{sheet.MaxFocus}  Inv {sheet.Investiture}/{sheet.MaxInvestiture}"
        );
        y += 20;

        var combat = _player.CosmereCombat;
        CosmereCombat.TickIdle(_player);
        combat = _player.CosmereCombat;
        builder.AddLabel(20, y, 0x480, "Combat");
        y += 18;
        if (combat is { InCombat: true })
        {
            builder.AddLabel(
                30,
                y,
                0x481,
                $"Scene {combat.SceneId}  React={(combat.ReactionAvailable ? "yes" : "no")}  Recover={(combat.RecoverUsedThisScene ? "used" : "ready")}  Dodge={(combat.DodgeArmed ? "armed" : "-")}  Brace={(combat.Braced ? "yes" : "-")}"
            );
        }
        else
        {
            builder.AddLabel(30, y, 0x481, "(out of combat)");
        }

        y += 20;

        builder.AddLabel(20, y, 0x480, "Equipment");
        y += 18;
        var deflect = CosmereEquipment.GetDeflect(_player);
        var cumbersome = CosmereEquipment.IsCumbersomeActive(_player);
        var presentable = CosmereEquipment.TryGetActiveArmor(_player, out var armor) && armor.Presentable;
        builder.AddLabel(
            30,
            y,
            0x481,
            $"Deflect {deflect}  Cumbersome={(cumbersome ? "yes" : "no")}  Presentable={(presentable ? "yes" : "no")}"
        );
        y += 20;

        CosmereSocial.TickIdle(_player);
        var social = _player.CosmereConversation;
        builder.AddLabel(20, y, 0x480, "Social");
        y += 18;
        if (social is { InConversation: true })
        {
            builder.AddLabel(
                30,
                y,
                0x481,
                $"Scene {social.SceneId}  Formal={(social.Formal ? "yes" : "no")}  Resist={(social.ResistArmed ? "armed" : "-")}  OptIn={(social.SocialOptIn ? "yes" : "no")}"
            );
        }
        else
        {
            builder.AddLabel(
                30,
                y,
                0x481,
                $"(out of conversation)  OptIn={(social?.SocialOptIn == true ? "yes" : "no")}"
            );
        }

        y += 20;

        CosmereAllomancy.TickBurns(_player);
        builder.AddLabel(20, y, 0x480, "Allomancy");
        y += 18;
        if (sheet.IsMetalborn)
        {
            var ranks = sheet.GetRank(CosmereSkill.Allomancy);
            builder.AddLabel(
                30,
                y,
                0x481,
                $"{sheet.GetColloquialMetalName()} {sheet.AllomanticMetal} {sheet.PowerTier}  " +
                $"limit {CosmereAllomancy.GetMetallicArtLimit(ranks)}  " +
                $"range {CosmereAllomancy.GetMetallicArtRangeFeet(ranks)}ft  " +
                $"die {(CosmereAllomancy.GetMetallicArtDieSides(ranks) <= 0 ? "1" : $"d{CosmereAllomancy.GetMetallicArtDieSides(ranks)}")}"
            );
            y += 16;
            var burns = _player.CosmereAllomancy?.Burns;
            if (burns is { Count: > 0 })
            {
                var burnLabel = "";
                for (var i = 0; i < burns.Count; i++)
                {
                    var b = burns[i];
                    burnLabel += $"{b.Metal}:{b.RoundsRemaining}r";
                    if (b.EnhancedAwareness > 0)
                    {
                        burnLabel += $"+{b.EnhancedAwareness}Awa";
                    }

                    if (i < burns.Count - 1)
                    {
                        burnLabel += " ";
                    }
                }

                builder.AddLabel(30, y, 0x481, $"Burns: {burnLabel}");
            }
            else
            {
                builder.AddLabel(30, y, 0x481, "Burns: (none)");
            }

            y += 16;
            builder.AddLabel(
                30,
                y,
                0x481,
                $"Depleted={(sheet.IsMetalDepleted(sheet.AllomanticMetal) ? sheet.AllomanticMetal.ToString() : "no")}"
            );
            y += 20;
        }
        else
        {
            builder.AddLabel(30, y, 0x481, "(not Metalborn)");
            y += 20;
        }

        builder.AddLabel(20, y, 0x480, $"Talents ({sheet.Talents.Count})");
        y += 18;
        if (sheet.Talents.Count == 0)
        {
            builder.AddLabel(30, y, 0x481, "(none)");
            y += 18;
        }
        else
        {
            var shownTalents = Math.Min(sheet.Talents.Count, 4);
            for (var i = 0; i < shownTalents; i++)
            {
                builder.AddLabel(30, y, 0x481, sheet.Talents[i]);
                y += 16;
            }
        }

        builder.AddLabel(20, y, 0x480, "Goals");
        y += 18;
        var activeGoals = 0;
        for (var i = 0; i < sheet.Goals.Count && activeGoals < 4; i++)
        {
            var g = sheet.Goals[i];
            if (g.Status != CosmereGoalStatus.Active)
            {
                continue;
            }

            builder.AddLabel(30, y, 0x481, g.Title);
            y += 16;
            activeGoals++;
        }

        if (activeGoals == 0)
        {
            builder.AddLabel(30, y, 0x481, "(none active)");
            y += 18;
        }

        builder.AddLabel(20, y, 0x480, "Conditions");
        y += 18;
        if (sheet.Conditions.Count == 0)
        {
            builder.AddLabel(30, y, 0x481, "(none)");
            y += 18;
        }
        else
        {
            var shown = Math.Min(sheet.Conditions.Count, 6);
            for (var i = 0; i < shown; i++)
            {
                var c = sheet.Conditions[i];
                var label = c.Magnitude > 0 ? $"{c.Kind} [{c.Magnitude}]" : c.Kind.ToString();
                if (!string.IsNullOrEmpty(c.Payload))
                {
                    label = $"{label} ({c.Payload})";
                }

                builder.AddLabel(30, y, 0x481, label);
                y += 16;
            }
        }

        builder.AddLabel(20, y, 0x480, "Injuries");
        y += 18;
        var active = 0;
        for (var i = 0; i < sheet.Injuries.Count && active < 4; i++)
        {
            var inj = sheet.Injuries[i];
            if (inj.Cleared)
            {
                continue;
            }

            builder.AddLabel(30, y, 0x481, $"#{inj.Id} {inj.Severity}");
            y += 16;
            active++;
        }

        if (active == 0)
        {
            builder.AddLabel(30, y, 0x481, "(none)");
            y += 18;
        }

        y += 6;
        builder.AddLabel(20, y, 0x480, "Skills (rank / modifier)");
        y += 18;

        for (var i = 0; i < CosmereSkills.Count; i++)
        {
            var skill = (CosmereSkill)i;
            var name = CosmereSkills.GetDisplayName(skill);
            var rank = sheet.GetRank(skill);
            var mod = sheet.GetModifier(skill);
            var col = i % 2;
            var row = i / 2;
            var x = 30 + col * 240;
            var sy = y + row * 16;
            builder.AddLabel(x, sy, 0x481, $"{name}: {rank} / +{mod}");
        }

        builder.AddButton(width / 2 - 40, height - 40, 0xF9, 0xF8, 0); // OK
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
    }
}
