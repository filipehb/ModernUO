using System;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public static class CosmereProfession
{
    public const int MaxRank = 5;

    // XP required to reach rank N from rank N-1 (index = target rank).
    private static readonly int[] _xpToRank =
    [
        0,    // 0
        100,  // 1
        250,  // 2
        500,  // 3
        900,  // 4
        1500  // 5
    ];

    public static int GetXpToReachRank(int rank)
    {
        if (rank < 1 || rank > MaxRank)
        {
            return 0;
        }

        return _xpToRank[rank];
    }

    public static int GetRecipeTier(int rank) => Math.Clamp(rank, 0, MaxRank);

    public static double GetTimeMod(int rank) =>
        rank switch
        {
            >= 5 => -0.20,
            4 => -0.15,
            3 => -0.10,
            2 => -0.05,
            _ => 0.0
        };

    public static double GetSaleMod(int rank) =>
        rank switch
        {
            >= 5 => 0.12,
            4 => 0.08,
            3 => 0.05,
            2 => 0.02,
            _ => 0.0
        };

    public static int GetActionXp(CosmereProfessionId profession) =>
        profession switch
        {
            CosmereProfessionId.Producer => 15,
            CosmereProfessionId.Artisan => 20,
            CosmereProfessionId.Merchant => 18,
            _ => 0
        };

    public static bool TryParse(string text, out CosmereProfessionId id)
    {
        id = CosmereProfessionId.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        if (normalized.Equals("none", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            id = CosmereProfessionId.None;
            return true;
        }

        return Enum.TryParse(normalized, true, out id) && id != CosmereProfessionId.None;
    }

    public static string Describe(CosmereSheet sheet)
    {
        if (sheet == null || sheet.ProfessionId == CosmereProfessionId.None)
        {
            return "Profession: (none)";
        }

        var next = sheet.ProfessionRank >= MaxRank
            ? "max"
            : $"{sheet.ProfessionXp}/{GetXpToReachRank(sheet.ProfessionRank + 1)}";

        return
            $"Profession: {sheet.ProfessionId} rank {sheet.ProfessionRank} " +
            $"(tier {GetRecipeTier(sheet.ProfessionRank)}, XP {next}, " +
            $"time {GetTimeMod(sheet.ProfessionRank):P0}, sale {GetSaleMod(sheet.ProfessionRank):P0})";
    }

    public static bool Assign(PlayerMobile pm, CosmereProfessionId id)
    {
        if (pm == null)
        {
            return false;
        }

        if (!CosmereConfig.ProfessionEnabled)
        {
            pm.SendMessage("Cosmere professions are disabled on this shard.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var pendingBefore = sheet.PendingTalentPicks;

        sheet.ProfessionId = id;
        if (id == CosmereProfessionId.None)
        {
            pm.SendMessage("Profession cleared.");
        }
        else if (sheet.ProfessionRank < 1)
        {
            // First assign starts at rank 0 with 0 XP; first successful action ranks up via XP.
            pm.SendMessage($"Profession set to {id} (rank {sheet.ProfessionRank}).");
        }
        else
        {
            pm.SendMessage($"Profession set to {id} (kept rank {sheet.ProfessionRank}, XP {sheet.ProfessionXp}).");
        }

        CosmereDebug.Trace(
            pm,
            $"Profession assign id={id} rank={sheet.ProfessionRank} xp={sheet.ProfessionXp} " +
            $"pendingTalents={sheet.PendingTalentPicks} (was {pendingBefore})"
        );
        return true;
    }

    public static bool AddXp(PlayerMobile pm, int amount, string reason = null)
    {
        if (pm == null || amount <= 0)
        {
            return false;
        }

        if (!CosmereConfig.ProfessionEnabled)
        {
            pm.SendMessage("Cosmere professions are disabled on this shard.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (sheet.ProfessionId == CosmereProfessionId.None)
        {
            pm.SendMessage("Assign a profession first ([CosmereProfession assign …]).");
            return false;
        }

        if (sheet.ProfessionRank >= MaxRank)
        {
            pm.SendMessage($"{sheet.ProfessionId} is already at max rank.");
            return false;
        }

        var scaled = (int)Math.Max(1, Math.Round(amount * CosmereConfig.ProfessionXpMultiplier));
        sheet.ProfessionXp += scaled;
        CosmereDebug.Trace(
            pm,
            $"Profession XP +{scaled} ({reason ?? "grant"}) → {sheet.ProfessionXp} " +
            $"(rank {sheet.ProfessionRank}, {sheet.ProfessionId})"
        );

        ProcessRankUps(pm);
        return true;
    }

    public static bool SetRank(PlayerMobile pm, int rank, bool keepXp = false)
    {
        if (pm == null)
        {
            return false;
        }

        if (!CosmereConfig.ProfessionEnabled)
        {
            pm.SendMessage("Cosmere professions are disabled on this shard.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        sheet.ProfessionRank = Math.Clamp(rank, 0, MaxRank);
        if (!keepXp)
        {
            sheet.ProfessionXp = 0;
        }

        pm.SendMessage(
            $"Profession rank set to {sheet.ProfessionRank} " +
            $"(tier {GetRecipeTier(sheet.ProfessionRank)}, XP {sheet.ProfessionXp})."
        );
        CosmereDebug.Trace(
            pm,
            $"Profession setrank={sheet.ProfessionRank} xp={sheet.ProfessionXp} id={sheet.ProfessionId}"
        );
        return true;
    }

    public static bool TryProfessionAction(PlayerMobile pm, CosmereProfessionId required, string actionLabel)
    {
        if (pm == null)
        {
            return false;
        }

        if (!CosmereConfig.ProfessionEnabled)
        {
            pm.SendMessage("Cosmere professions are disabled on this shard.");
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (sheet.ProfessionId != required)
        {
            pm.SendMessage(
                sheet.ProfessionId == CosmereProfessionId.None
                    ? $"Assign {required} first ([CosmereProfession assign {required}])."
                    : $"{actionLabel} requires {required} (you are {sheet.ProfessionId})."
            );
            CosmereDebug.Trace(pm, $"Profession action blocked: need {required}, have {sheet.ProfessionId}");
            return false;
        }

        var xp = GetActionXp(required);
        pm.SendMessage($"{actionLabel} (stub). +{xp} Profession XP.");
        AddXp(pm, xp, actionLabel);
        return true;
    }

    private static void ProcessRankUps(PlayerMobile pm)
    {
        var sheet = pm.CosmereSheet;
        while (sheet.ProfessionRank < MaxRank)
        {
            var next = sheet.ProfessionRank + 1;
            var need = GetXpToReachRank(next);
            if (sheet.ProfessionXp < need)
            {
                break;
            }

            sheet.ProfessionXp -= need;
            sheet.ProfessionRank = next;
            pm.SendMessage(
                $"{sheet.ProfessionId} reached rank {sheet.ProfessionRank} " +
                $"(recipe tier {GetRecipeTier(sheet.ProfessionRank)})."
            );
            CosmereDebug.Trace(
                pm,
                $"Profession rank-up → {sheet.ProfessionRank} overflowXp={sheet.ProfessionXp} " +
                $"tier={GetRecipeTier(sheet.ProfessionRank)}"
            );
        }

        if (sheet.ProfessionRank >= MaxRank)
        {
            sheet.ProfessionXp = 0;
        }
    }
}
