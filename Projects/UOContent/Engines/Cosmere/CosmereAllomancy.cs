using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public class CosmereBurnEffect
{
    public CosmereAllomanticMetal Metal { get; set; }
    public int RoundsRemaining { get; set; }
    public int SpendOnActivate { get; set; }
    public int EnhancedAwareness { get; set; }
    public bool SupernaturalSenses { get; set; }
    public bool SenseOnly { get; set; }
    public DateTime LastTick { get; set; } = Core.Now;
    public bool OverstimulatedThisRound { get; set; }
}

public class CosmereInvestiturePulse
{
    public Mobile Source { get; set; }
    public CosmereAllomanticMetal Metal { get; set; }
    public DateTime When { get; set; }
    public Point3D Location { get; set; }
}

public class CosmereAllomancyState
{
    public List<CosmereBurnEffect> Burns { get; } = [];
    public List<CosmereInvestiturePulse> Pulses { get; } = [];
    public DateTime RiotIgnoreUntil { get; set; } = DateTime.MinValue;
}

public static class CosmereAllomancy
{
    private static readonly List<CosmereInvestiturePulse> _globalPulses = [];
    private const int PulseRetainSeconds = 30;

    public static CosmereAllomancyState GetOrCreateState(PlayerMobile pm)
    {
        pm.CosmereAllomancy ??= new CosmereAllomancyState();
        return pm.CosmereAllomancy;
    }

    public static int GetMetallicArtLimit(int ranks) =>
        ranks switch
        {
            <= 0 => 1,
            1 => 1,
            2 => 2,
            _ => 3
        };

    public static int GetMetallicArtRangeFeet(int ranks) =>
        ranks switch
        {
            <= 0 => 10,
            1 => 20,
            2 => 40,
            _ => 80
        };

    public static int GetMetallicArtDieSides(int ranks) =>
        ranks switch
        {
            <= 0 => 0, // flat 1
            1 => 4,
            2 => 6,
            _ => 8
        };

    public static int GetMetallicArtRangeTiles(int ranks)
    {
        var feet = GetMetallicArtRangeFeet(ranks);
        return Math.Max(1, (int)Math.Ceiling(feet / (double)CosmereConfig.FeetPerTile));
    }

    public static int RollMetallicArtDamage(int ranks)
    {
        var sides = GetMetallicArtDieSides(ranks);
        return sides <= 0 ? 1 : Utility.Random(sides) + 1;
    }

    public static int TinEnhancedFromSpend(int spend) =>
        spend <= 0 ? 0 : (spend + 1) / 2; // ceil(spend/2)

    public static bool TrySpendInvestiture(CosmereSheet sheet, int amount)
    {
        if (sheet == null || amount <= 0)
        {
            return false;
        }

        if (sheet.Investiture < amount)
        {
            return false;
        }

        sheet.Investiture -= amount;
        sheet.ClampPools();
        return true;
    }

    public static bool TrySpendInvestiture(PlayerMobile pm, int amount, string action)
    {
        pm.EnsureCosmereSheet();
        var before = pm.CosmereSheet.Investiture;
        var ok = TrySpendInvestiture(pm.CosmereSheet, amount);
        CosmereDebug.TraceInvestiture(pm, action, before, amount, ok);
        return ok;
    }

    public static void RecordPulse(Mobile source, CosmereAllomanticMetal metal)
    {
        if (source == null || metal == CosmereAllomanticMetal.None)
        {
            return;
        }

        PrunePulses();
        _globalPulses.Add(
            new CosmereInvestiturePulse
            {
                Source = source,
                Metal = metal,
                When = Core.Now,
                Location = source.Location
            }
        );
    }

    public static void PrunePulses()
    {
        var cutoff = Core.Now - TimeSpan.FromSeconds(PulseRetainSeconds);
        for (var i = _globalPulses.Count - 1; i >= 0; i--)
        {
            if (_globalPulses[i].When < cutoff || _globalPulses[i].Source?.Deleted != false)
            {
                _globalPulses.RemoveAt(i);
            }
        }
    }

    public static List<CosmereInvestiturePulse> GetPulsesInRange(Point3D from, Map map, int rangeTiles)
    {
        PrunePulses();
        var results = new List<CosmereInvestiturePulse>();
        for (var i = 0; i < _globalPulses.Count; i++)
        {
            var pulse = _globalPulses[i];
            if (pulse.Source?.Map != map)
            {
                continue;
            }

            if (Utility.InRange(from, pulse.Source.Location, rangeTiles))
            {
                results.Add(pulse);
            }
        }

        return results;
    }

    public static void OnCombatSceneStart(PlayerMobile pm)
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (!sheet.IsMetalborn)
        {
            return;
        }

        if (sheet.HasCondition(CosmereConditionKind.Surprised))
        {
            sheet.Investiture = Math.Min(1, sheet.MaxInvestiture);
        }
        else
        {
            sheet.Investiture = sheet.MaxInvestiture;
        }

        sheet.ClampPools();
        CosmereFacade.Apply(pm);
    }

    public static bool TrySnap(
        PlayerMobile pm,
        CosmereAllomanticMetal metal,
        bool grantStarterExtraRank = true
    )
    {
        if (!CosmereConfig.AllomancyEnabled)
        {
            pm.SendMessage("Allomancy is disabled on this shard.");
            return false;
        }

        if (metal is CosmereAllomanticMetal.None)
        {
            pm.SendMessage("Choose Bronze, Steel, Tin, or Zinc.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (sheet.IsMetalborn)
        {
            pm.SendMessage("You have already Snapped.");
            return false;
        }

        // Soft ancestry check (message only).
        pm.SendMessage("Ancestry check soft-passed (human/koloss-blooded assumed).");

        sheet.IsMetalborn = true;
        sheet.AllomanticMetal = metal;
        sheet.PowerTier = CosmerePowerTier.Nascent;

        var ranks = grantStarterExtraRank ? 2 : 1;
        sheet.SetRank(CosmereSkill.Allomancy, ranks);
        sheet.Investiture = sheet.MaxInvestiture;
        sheet.ClampPools();

        CosmereFacade.Apply(pm);
        CosmereGoals.EnsureTrainYourPower(pm);
        pm.SendMessage(
            $"You Snap as a {sheet.GetColloquialMetalName()} ({metal}, nascent). Allomancy rank {ranks}."
        );
        return true;
    }

    public static void UnlockFullPowerSilent(PlayerMobile pm)
    {
        if (pm == null)
        {
            return;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (!sheet.IsMetalborn)
        {
            return;
        }

        if (sheet.PowerTier == CosmerePowerTier.Full)
        {
            return;
        }

        sheet.PowerTier = CosmerePowerTier.Full;
        pm.SendMessage($"Your {sheet.AllomanticMetal} power is now full (Train Your Power gate cleared).");
    }

    public static bool TryUnlockPower(PlayerMobile pm)
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (!sheet.IsMetalborn)
        {
            pm.SendMessage("Not Metalborn.");
            return false;
        }

        if (sheet.PowerTier == CosmerePowerTier.Full)
        {
            CosmereGoals.MarkTrainYourPowerCompletedIfActive(pm);
            pm.SendMessage("Power already fully unlocked.");
            return false;
        }

        sheet.PowerTier = CosmerePowerTier.Full;
        CosmereGoals.MarkTrainYourPowerCompletedIfActive(pm);
        pm.SendMessage($"Your {sheet.AllomanticMetal} power is now full (Train Your Power gate cleared).");
        return true;
    }

    public static bool TryDrink(PlayerMobile pm, CosmereMetalFlags metalsInVial, bool consumeItem)
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (!sheet.IsMetalborn)
        {
            pm.SendMessage("You have no Investiture pool.");
            return false;
        }

        if (CosmereConfig.RequireVials && metalsInVial == CosmereMetalFlags.None)
        {
            pm.SendMessage("You need a metal vial.");
            return false;
        }

        if (metalsInVial == CosmereMetalFlags.None)
        {
            metalsInVial = CosmereMetalFlags.AllStarter;
        }

        sheet.Investiture = sheet.MaxInvestiture;

        // Clear depleted for metals in vial; deplete known power if not in vial.
        foreach (CosmereAllomanticMetal metal in new[]
                 {
                     CosmereAllomanticMetal.Bronze,
                     CosmereAllomanticMetal.Steel,
                     CosmereAllomanticMetal.Tin,
                     CosmereAllomanticMetal.Zinc
                 })
        {
            var flag = ToFlag(metal);
            if ((metalsInVial & flag) != 0)
            {
                sheet.SetMetalDepleted(metal, false);
            }
            else if (sheet.AllomanticMetal == metal)
            {
                sheet.SetMetalDepleted(metal, true);
            }
        }

        sheet.ClampPools();
        CosmereFacade.Apply(pm);
        pm.SendMessage($"You drink a metal vial. Investiture {sheet.Investiture}/{sheet.MaxInvestiture}.");
        CosmereDebug.Trace(
            pm,
            $"Drink: Inv {sheet.Investiture}/{sheet.MaxInvestiture} metals={metalsInVial} " +
            $"depletedPower={sheet.IsMetalDepleted(sheet.AllomanticMetal)}"
        );
        return true;
    }

    public static CosmereMetalFlags ToFlag(CosmereAllomanticMetal metal) =>
        metal switch
        {
            CosmereAllomanticMetal.Bronze => CosmereMetalFlags.Bronze,
            CosmereAllomanticMetal.Steel => CosmereMetalFlags.Steel,
            CosmereAllomanticMetal.Tin => CosmereMetalFlags.Tin,
            CosmereAllomanticMetal.Zinc => CosmereMetalFlags.Zinc,
            _ => CosmereMetalFlags.None
        };

    public static bool TryParseMetal(string text, out CosmereAllomanticMetal metal)
    {
        metal = CosmereAllomanticMetal.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Enum.TryParse(text.Trim(), true, out metal) && metal != CosmereAllomanticMetal.None;
    }

    public static CosmereBurnEffect FindBurn(PlayerMobile pm, CosmereAllomanticMetal metal)
    {
        var state = pm.CosmereAllomancy;
        if (state == null)
        {
            return null;
        }

        for (var i = 0; i < state.Burns.Count; i++)
        {
            if (state.Burns[i].Metal == metal)
            {
                return state.Burns[i];
            }
        }

        return null;
    }

    public static void TickBurns(PlayerMobile pm)
    {
        var state = pm.CosmereAllomancy;
        if (state == null || state.Burns.Count == 0)
        {
            return;
        }

        pm.EnsureCosmereSheet();
        var inCombat = pm.CosmereCombat is { InCombat: true };
        var interval = inCombat ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(10);
        var now = Core.Now;

        for (var i = state.Burns.Count - 1; i >= 0; i--)
        {
            var burn = state.Burns[i];
            if (now - burn.LastTick < interval)
            {
                continue;
            }

            burn.LastTick = now;
            burn.RoundsRemaining--;
            burn.OverstimulatedThisRound = false;

            if (burn.RoundsRemaining > 0)
            {
                continue;
            }

            EndBurn(pm, burn.Metal, quiet: false);
        }
    }

    public static void EndBurn(PlayerMobile pm, CosmereAllomanticMetal metal, bool quiet)
    {
        var state = GetOrCreateState(pm);
        for (var i = state.Burns.Count - 1; i >= 0; i--)
        {
            if (state.Burns[i].Metal != metal)
            {
                continue;
            }

            state.Burns.RemoveAt(i);
        }

        if (metal == CosmereAllomanticMetal.Tin)
        {
            pm.EnsureCosmereSheet();
            pm.CosmereSheet.SetEnhancedAwareness(0);
        }

        if (!quiet)
        {
            pm.SendMessage($"{metal} burn ends.");
        }

        CosmereDebug.Trace(pm, $"Burn end: {metal} quiet={quiet}");
    }

    public static bool CanUseMetal(PlayerMobile pm, CosmereAllomanticMetal metal, bool requireFull, out string reason)
    {
        reason = null;
        if (!CosmereConfig.AllomancyEnabled)
        {
            reason = "Allomancy disabled";
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (!sheet.IsMetalborn || sheet.AllomanticMetal != metal)
        {
            reason = "Wrong or missing metal power";
            return false;
        }

        if (sheet.Dead || sheet.HasCondition(CosmereConditionKind.Unconscious))
        {
            reason = "Dead or Unconscious";
            return false;
        }

        if (sheet.IsMetalDepleted(metal))
        {
            reason = $"Depleted of {metal}";
            return false;
        }

        if (requireFull && sheet.PowerTier != CosmerePowerTier.Full)
        {
            reason = "Power still nascent";
            return false;
        }

        return true;
    }

    public static bool TryMaintain(PlayerMobile pm, CosmereAllomanticMetal metal, int amount)
    {
        TickBurns(pm);
        if (!CanUseMetal(pm, metal, requireFull: false, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        var burn = FindBurn(pm, metal);
        if (burn == null)
        {
            pm.SendMessage($"You are not burning {metal}.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var limit = GetMetallicArtLimit(sheet.GetRank(CosmereSkill.Allomancy));
        amount = Math.Clamp(amount, 1, limit);

        // Steel sense maintain is free
        if (metal == CosmereAllomanticMetal.Steel && burn.SenseOnly)
        {
            burn.RoundsRemaining = Math.Max(burn.RoundsRemaining, 1);
            burn.LastTick = Core.Now;
            pm.SendMessage("You maintain Steel sense (free).");
            return true;
        }

        if (!TrySpendInvestiture(pm, amount, $"Maintain {metal}"))
        {
            pm.SendMessage("Not enough Investiture to maintain.");
            return false;
        }

        var ranks = Math.Max(1, sheet.GetRank(CosmereSkill.Allomancy));
        burn.RoundsRemaining += ranks;
        burn.LastTick = Core.Now;
        RecordPulse(pm, metal);
        CosmereFacade.Apply(pm);
        pm.SendMessage($"Maintained {metal} (+{ranks} rounds). Inv {sheet.Investiture}/{sheet.MaxInvestiture}.");
        CosmereDebug.Trace(pm, $"Maintain {metal}: +{ranks} rounds → {burn.RoundsRemaining}");
        return true;
    }

    public static bool TryBurn(PlayerMobile pm, CosmereAllomanticMetal metal, int spend)
    {
        TickBurns(pm);
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;

        if (!CanUseMetal(pm, metal, requireFull: false, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        if (sheet.PowerTier == CosmerePowerTier.Nascent)
        {
            return TryNascent(pm, metal);
        }

        return metal switch
        {
            CosmereAllomanticMetal.Steel => TryBurnSteelSense(pm),
            CosmereAllomanticMetal.Tin => TryBurnTin(pm, spend),
            CosmereAllomanticMetal.Bronze => TryBurnBronze(pm, spend),
            CosmereAllomanticMetal.Zinc => false, // Riot command
            _ => false
        };
    }

    private static bool TryNascent(PlayerMobile pm, CosmereAllomanticMetal metal)
    {
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (sheet.Investiture < 1)
        {
            pm.SendMessage("Need at least 1 Investiture for nascent flare.");
            return false;
        }

        if (!TrySpendInvestiture(pm, 1, $"Nascent {metal}"))
        {
            return false;
        }

        RecordPulse(pm, metal);
        var range = Math.Max(1, (int)Math.Ceiling(10.0 / CosmereConfig.FeetPerTile));

        switch (metal)
        {
            case CosmereAllomanticMetal.Bronze:
                {
                    var pulses = GetPulsesInRange(pm.Location, pm.Map, range);
                    var any = false;
                    for (var i = 0; i < pulses.Count; i++)
                    {
                        if (pulses[i].Source != pm)
                        {
                            any = true;
                            break;
                        }
                    }

                    pm.SendMessage(
                        any
                            ? "Nascent Bronze: you sense Allomantic activity nearby."
                            : "Nascent Bronze: no Allomantic pulses within 10 ft."
                    );
                    break;
                }
            case CosmereAllomanticMetal.Steel:
                {
                    var count = CountMetalItemsNear(pm, range);
                    pm.SendMessage($"Nascent Steel: you glimpse {count} metal source(s) nearby.");
                    break;
                }
            case CosmereAllomanticMetal.Tin:
                pm.SendMessage("Nascent Tin: one sense flares supernatural for a moment.");
                break;
            case CosmereAllomanticMetal.Zinc:
                pm.SendMessage("Nascent Zinc: you tug at emotions within 10 ft (no lasting Riot).");
                break;
        }

        CosmereFacade.Apply(pm);
        return true;
    }

    public static bool TryBurnSteelSense(PlayerMobile pm)
    {
        if (!CanUseMetal(pm, CosmereAllomanticMetal.Steel, requireFull: true, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        pm.EnsureCosmereSheet();
        if (pm.CosmereSheet.Investiture < 1)
        {
            pm.SendMessage("Need Investiture ≥ 1 to burn Steel.");
            return false;
        }

        var state = GetOrCreateState(pm);
        EndBurn(pm, CosmereAllomanticMetal.Steel, quiet: true);
        state.Burns.Add(
            new CosmereBurnEffect
            {
                Metal = CosmereAllomanticMetal.Steel,
                RoundsRemaining = 1,
                SenseOnly = true,
                LastTick = Core.Now
            }
        );

        RecordPulse(pm, CosmereAllomanticMetal.Steel);
        var ranks = pm.CosmereSheet.GetRank(CosmereSkill.Allomancy);
        var range = GetMetallicArtRangeTiles(ranks);
        var count = CountMetalItemsNear(pm, range);
        pm.SendMessage($"Burning Steel (sense). {count} metal source(s) within {range} tiles.");
        ListMetalItemsNear(pm, range, maxList: 8);
        return true;
    }

    public static int CountMetalItemsNear(PlayerMobile pm, int range)
    {
        var count = 0;
        foreach (var item in pm.Map.GetItemsInRange(pm.Location, range))
        {
            if (IsMetalItem(item))
            {
                count++;
            }
        }

        return count;
    }

    public static void ListMetalItemsNear(PlayerMobile pm, int range, int maxList)
    {
        var listed = 0;
        foreach (var item in pm.Map.GetItemsInRange(pm.Location, range))
        {
            if (!IsMetalItem(item))
            {
                continue;
            }

            pm.SendMessage($"  • {item.Name ?? item.GetType().Name} @ {item.Location}");
            listed++;
            if (listed >= maxList)
            {
                break;
            }
        }
    }

    public static bool IsMetalItem(Item item)
    {
        if (item == null || item.Deleted)
        {
            return false;
        }

        var name = item.GetType().Name;
        if (name.Contains("Aluminum", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Aluminium", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return item is BaseWeapon or BaseArmor or BaseIngot or BaseOre;
    }

    public static bool TryBurnTin(PlayerMobile pm, int spend)
    {
        if (!CanUseMetal(pm, CosmereAllomanticMetal.Tin, requireFull: true, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var ranks = sheet.GetRank(CosmereSkill.Allomancy);
        var limit = GetMetallicArtLimit(ranks);
        spend = Math.Clamp(spend <= 0 ? 1 : spend, 1, limit);

        if (!TrySpendInvestiture(pm, spend, "Burn Tin"))
        {
            pm.SendMessage("Not enough Investiture.");
            return false;
        }

        var enhanced = TinEnhancedFromSpend(spend);
        var state = GetOrCreateState(pm);
        EndBurn(pm, CosmereAllomanticMetal.Tin, quiet: true);
        state.Burns.Add(
            new CosmereBurnEffect
            {
                Metal = CosmereAllomanticMetal.Tin,
                RoundsRemaining = Math.Max(1, ranks),
                SpendOnActivate = spend,
                EnhancedAwareness = enhanced,
                SupernaturalSenses = true,
                LastTick = Core.Now
            }
        );

        sheet.SetEnhancedAwareness(enhanced);

        RecordPulse(pm, CosmereAllomanticMetal.Tin);
        CosmereFacade.Apply(pm);
        pm.SendMessage(
            $"Burning Tin: Enhanced[Awareness] +{enhanced} for {Math.Max(1, ranks)} rounds. Inv {sheet.Investiture}/{sheet.MaxInvestiture}."
        );
        CosmereDebug.Trace(pm, $"Burn Tin: enhanced={enhanced} rounds={Math.Max(1, ranks)}");
        return true;
    }

    public static void TryOverstimulation(PlayerMobile pm)
    {
        var burn = FindBurn(pm, CosmereAllomanticMetal.Tin);
        if (burn is not { SupernaturalSenses: true } || burn.OverstimulatedThisRound)
        {
            return;
        }

        burn.OverstimulatedThisRound = true;
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var dc = 12 + burn.EnhancedAwareness;
        var result = CosmereTestResolver.Resolve(sheet, CosmereSkill.Discipline, dc);
        if (result.Blocked || result.Success)
        {
            return;
        }

        sheet.EnsureCondition(CosmereConditionKind.Disoriented, source: "tin-overstimulation");
        pm.SendMessage($"Overstimulation! Discipline vs DC {dc} failed — you are Disoriented.");
    }

    public static bool TryBurnBronze(PlayerMobile pm, int spend)
    {
        if (!CanUseMetal(pm, CosmereAllomanticMetal.Bronze, requireFull: true, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var ranks = sheet.GetRank(CosmereSkill.Allomancy);
        var limit = GetMetallicArtLimit(ranks);
        spend = Math.Clamp(spend <= 0 ? 1 : spend, 1, limit);

        if (!TrySpendInvestiture(pm, spend, "Burn Bronze"))
        {
            pm.SendMessage("Not enough Investiture.");
            return false;
        }

        var state = GetOrCreateState(pm);
        EndBurn(pm, CosmereAllomanticMetal.Bronze, quiet: true);
        state.Burns.Add(
            new CosmereBurnEffect
            {
                Metal = CosmereAllomanticMetal.Bronze,
                RoundsRemaining = Math.Max(1, ranks),
                SpendOnActivate = spend,
                LastTick = Core.Now
            }
        );

        RecordPulse(pm, CosmereAllomanticMetal.Bronze);
        CosmereFacade.Apply(pm);

        var range = GetMetallicArtRangeTiles(ranks);
        var pulses = GetPulsesInRange(pm.Location, pm.Map, range);
        var other = 0;
        for (var i = 0; i < pulses.Count; i++)
        {
            if (pulses[i].Source != pm)
            {
                other++;
            }
        }

        pm.SendMessage(
            $"Burning Bronze: sense {other} pulse(s) in range ({range} tiles). Use CosmereSeek to pin. Inv {sheet.Investiture}/{sheet.MaxInvestiture}."
        );
        CosmereDebug.Trace(pm, $"Burn Bronze: pulses={other} rangeTiles={range} rounds={Math.Max(1, ranks)}");
        return true;
    }

    public static bool TrySeek(PlayerMobile pm, bool identify)
    {
        TickBurns(pm);
        if (!CanUseMetal(pm, CosmereAllomanticMetal.Bronze, requireFull: true, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        var burn = FindBurn(pm, CosmereAllomanticMetal.Bronze);
        if (burn == null)
        {
            pm.SendMessage("Burn Bronze first.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var ranks = sheet.GetRank(CosmereSkill.Allomancy);
        var range = GetMetallicArtRangeTiles(ranks);
        var pulses = GetPulsesInRange(pm.Location, pm.Map, range);
        var pinned = 0;
        var maxPin = Math.Max(1, burn.SpendOnActivate);

        for (var i = 0; i < pulses.Count && pinned < maxPin; i++)
        {
            var pulse = pulses[i];
            if (pulse.Source == pm || pulse.Source == null)
            {
                continue;
            }

            var dir = pm.GetDirectionTo(pulse.Source);
            var label = pulse.Metal.ToString();
            if (identify)
            {
                var id = CosmereTestResolver.Resolve(sheet, CosmereSkill.Allomancy, 15);
                label = id.Success ? pulse.Metal.ToString() : "unknown power";
            }

            pm.SendMessage(
                $"Seek: {pulse.Source.Name} ({label}) {dir} @ {pulse.Source.Location}"
            );
            pinned++;
        }

        if (pinned == 0)
        {
            pm.SendMessage("Seek: no foreign pulses to pin.");
        }

        CosmereDebug.Trace(pm, $"Seek: pinned={pinned} maxPin={maxPin} identify={identify} range={range}");
        return true;
    }

    public static bool TryRiot(PlayerMobile pm, string emotion, int spend, bool tryStealth)
    {
        TickBurns(pm);
        if (!CanUseMetal(pm, CosmereAllomanticMetal.Zinc, requireFull: true, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        if (string.IsNullOrWhiteSpace(emotion))
        {
            emotion = "anger";
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var ranks = sheet.GetRank(CosmereSkill.Allomancy);
        var limit = GetMetallicArtLimit(ranks);
        spend = Math.Clamp(spend <= 0 ? 1 : spend, 1, limit);

        if (!TrySpendInvestiture(pm, spend, "Riot Zinc"))
        {
            pm.SendMessage("Not enough Investiture.");
            return false;
        }

        var state = GetOrCreateState(pm);
        EndBurn(pm, CosmereAllomanticMetal.Zinc, quiet: true);
        state.Burns.Add(
            new CosmereBurnEffect
            {
                Metal = CosmereAllomanticMetal.Zinc,
                RoundsRemaining = 2, // until end of next round
                SpendOnActivate = spend,
                LastTick = Core.Now
            }
        );

        RecordPulse(pm, CosmereAllomanticMetal.Zinc);
        var radius = Math.Max(1, (int)Math.Ceiling(10.0 / CosmereConfig.FeetPerTile));
        var affected = 0;

        foreach (var m in pm.Map.GetMobilesInRange(pm.Location, radius))
        {
            if (m == pm || !m.Alive)
            {
                continue;
            }

            if (m is PlayerMobile targetPm)
            {
                targetPm.EnsureCosmereSheet();
                var spiritualDc = targetPm.CosmereSheet.SpiritualDefense;
                var alloState = GetOrCreateState(targetPm);
                if (alloState.RiotIgnoreUntil > Core.Now)
                {
                    continue;
                }

                var combat = CosmereCombat.GetOrCreateState(targetPm);
                if (combat.ReactionAvailable && targetPm.CosmereSheet.Focus >= spend)
                {
                    if (CosmereSocial.TryResistInfluence(targetPm, spend, out var resisted) && resisted)
                    {
                        combat.ReactionAvailable = false;
                        alloState.RiotIgnoreUntil = Core.Now + TimeSpan.FromSeconds(10);
                        targetPm.SendMessage("You resist the Riot (Focus).");
                        continue;
                    }
                }

                if (tryStealth)
                {
                    var stealth = CosmereTestResolver.Resolve(
                        sheet,
                        CosmereSkill.Allomancy,
                        spiritualDc
                    );
                    if (!stealth.Success)
                    {
                        targetPm.SendMessage($"{pm.Name} is Rioting {emotion} nearby.");
                    }
                }

                ApplyRioted(targetPm.CosmereSheet, emotion);
                targetPm.SendMessage($"You feel surging {emotion} (Riot — roleplay only, not mind control).");
                affected++;
            }
            else
            {
                m.PublicOverheadMessage(MessageType.Regular, 0x22, false, $"*feels surging {emotion}*");
                affected++;
            }
        }

        CosmereFacade.Apply(pm);
        pm.SendMessage(
            $"Riot {emotion}: {affected} target(s) in {radius}-tile radius. Inv {sheet.Investiture}/{sheet.MaxInvestiture}."
        );
        CosmereDebug.Trace(pm, $"Riot emotion={emotion} affected={affected} spend={spend} stealth={tryStealth}");
        return true;
    }

    private static void ApplyRioted(CosmereSheet sheet, string emotion)
    {
        for (var i = 0; i < sheet.Conditions.Count; i++)
        {
            if (sheet.Conditions[i].Kind == CosmereConditionKind.Rioted)
            {
                sheet.Conditions[i].Payload = emotion;
                sheet.Conditions[i].Source = "zinc";
                return;
            }
        }

        sheet.Conditions.Add(
            new CosmereCondition
            {
                Kind = CosmereConditionKind.Rioted,
                Payload = emotion,
                Source = "zinc"
            }
        );
    }

    public static bool TrySteelpush(PlayerMobile pm, IEntity target, int spend)
    {
        TickBurns(pm);
        if (!CanUseMetal(pm, CosmereAllomanticMetal.Steel, requireFull: true, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        if (FindBurn(pm, CosmereAllomanticMetal.Steel) == null)
        {
            TryBurnSteelSense(pm);
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var ranks = sheet.GetRank(CosmereSkill.Allomancy);
        var limit = GetMetallicArtLimit(ranks);
        spend = Math.Clamp(spend <= 0 ? 1 : spend, 1, limit);

        if (!TrySpendInvestiture(pm, spend, "Steelpush"))
        {
            pm.SendMessage("Not enough Investiture.");
            return false;
        }

        RecordPulse(pm, CosmereAllomanticMetal.Steel);
        var tiles = 4 * spend; // 20ft * spend / 5
        var map = pm.Map;

        // Worn/affixed or mobile → push self; loose metal item → push item
        if (target is Item item && item.Parent == null && IsMetalItem(item))
        {
            PushAlongLine(item, pm.GetDirectionTo(item), tiles, map);
            pm.SendMessage($"Steelpush propels the {item.Name ?? item.GetType().Name} ({tiles} tiles).");
            CosmereDebug.Trace(pm, $"Steelpush item tiles={tiles} spend={spend}");
        }
        else
        {
            var dir = target != null ? pm.GetDirectionTo(target) : pm.Direction;
            PushAlongLine(pm, dir, tiles, map);
            pm.SendMessage($"Steelpush propels you ({tiles} tiles).");
            CosmereDebug.Trace(pm, $"Steelpush self tiles={tiles} spend={spend} dir={dir}");
        }

        CosmereFacade.Apply(pm);
        return true;
    }

    private static void PushAlongLine(IEntity entity, Direction dir, int tiles, Map map)
    {
        if (map == null || tiles <= 0)
        {
            return;
        }

        var loc = entity.Location;
        for (var i = 0; i < tiles; i++)
        {
            var next = loc;
            Server.Movement.Movement.Offset(dir, ref next);
            if (!map.CanFit(next, 16, true, false))
            {
                // Impassable: if pushing an item, stop; caller may push self separately
                break;
            }

            loc = next;
            if (entity is Mobile m)
            {
                m.Location = loc;
            }
            else if (entity is Item item)
            {
                item.MoveToWorld(loc, map);
            }
        }
    }

    public static bool TryShootCoin(PlayerMobile pm, Mobile defender, int spend)
    {
        TickBurns(pm);
        if (!CanUseMetal(pm, CosmereAllomanticMetal.Steel, requireFull: true, out var reason))
        {
            pm.SendMessage(reason);
            return false;
        }

        if (defender == null || !defender.Alive)
        {
            pm.SendMessage("Invalid target.");
            return false;
        }

        if (FindBurn(pm, CosmereAllomanticMetal.Steel) == null)
        {
            TryBurnSteelSense(pm);
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var ranks = sheet.GetRank(CosmereSkill.Allomancy);
        var range = GetMetallicArtRangeTiles(ranks);
        if (!pm.InRange(defender, range))
        {
            pm.SendMessage("Out of Metallic Art range.");
            return false;
        }

        if (!HasMetalProjectile(pm))
        {
            pm.SendMessage("You need a metal weapon or ingot in your backpack to Shoot Coin.");
            return false;
        }

        var limit = GetMetallicArtLimit(ranks);
        spend = Math.Clamp(spend <= 0 ? 1 : spend, 1, limit);
        if (!TrySpendInvestiture(pm, spend, "Shoot Coin"))
        {
            pm.SendMessage("Not enough Investiture.");
            return false;
        }

        RecordPulse(pm, CosmereAllomanticMetal.Steel);
        CosmereCombat.BeginCombat(pm, defender);

        var dc = CosmereStrike.GetDefenseDc(defender);
        var test = CosmereTestResolver.Resolve(sheet, CosmereSkill.Allomancy, dc);
        CosmereDebug.TraceTest(pm, test);
        if (test.Blocked)
        {
            pm.SendMessage($"Shoot Coin blocked: {test.BlockReason}.");
            CosmereFacade.Apply(pm);
            return true;
        }

        var damageRoll = RollMetallicArtDamage(ranks);
        CosmereStrike.ComputeDamageOutcome(
            sheet,
            test.Success,
            damageRoll,
            CosmereSkill.Allomancy,
            out var outcome,
            out var damage
        );

        if (damage > 0)
        {
            CosmereStrike.ApplyDamage(pm, defender, damage, CosmereDamageType.Impact);
        }

        CosmereFacade.Apply(pm);
        pm.SendMessage(
            $"Shoot Coin vs {defender.Name}: d20={test.D20}+{test.Modifier}={test.Total} vs DC {dc} → {outcome}" +
            (damage > 0 ? $" Impact {damage}" : "")
        );
        CosmereDebug.Trace(
            pm,
            $"Shoot Coin: outcome={outcome} dmgRoll={damageRoll} dmg={damage} spend={spend}"
        );
        return true;
    }

    private static bool HasMetalProjectile(PlayerMobile pm)
    {
        if (pm.Backpack == null)
        {
            return false;
        }

        foreach (var item in pm.Backpack.Items)
        {
            if (IsMetalItem(item))
            {
                return true;
            }
        }

        return pm.Weapon is BaseWeapon and not Fists;
    }

    public static string DescribeStatus(PlayerMobile pm)
    {
        TickBurns(pm);
        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (!sheet.IsMetalborn)
        {
            return "Not Metalborn.";
        }

        var ranks = sheet.GetRank(CosmereSkill.Allomancy);
        var burns = pm.CosmereAllomancy?.Burns;
        var burnText = "(none)";
        if (burns is { Count: > 0 })
        {
            burnText = "";
            for (var i = 0; i < burns.Count; i++)
            {
                var b = burns[i];
                burnText += $"{b.Metal}:{b.RoundsRemaining}r";
                if (b.EnhancedAwareness > 0)
                {
                    burnText += $"+Awa{b.EnhancedAwareness}";
                }

                if (i < burns.Count - 1)
                {
                    burnText += ", ";
                }
            }
        }

        return
            $"{sheet.GetColloquialMetalName()} {sheet.AllomanticMetal} {sheet.PowerTier} " +
            $"Inv {sheet.Investiture}/{sheet.MaxInvestiture} Allomancy r{ranks} " +
            $"limit={GetMetallicArtLimit(ranks)} range={GetMetallicArtRangeFeet(ranks)}ft " +
            $"die={(GetMetallicArtDieSides(ranks) <= 0 ? "1" : $"d{GetMetallicArtDieSides(ranks)}")} " +
            $"burns=[{burnText}] depleted={sheet.IsMetalDepleted(sheet.AllomanticMetal)}";
    }
}
