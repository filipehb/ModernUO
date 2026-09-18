using System;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public static class CosmereGoals
{
    public const string TrainYourPowerId = "train-your-power";
    public const string TrainYourPowerTitle = "Train Your Power";

    public static CosmereGoalRecord Assign(
        PlayerMobile pm,
        string id,
        string title,
        CosmereGoalRewardKind rewardKind,
        string rewardPayload = null
    )
    {
        if (pm == null || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var existing = sheet.FindGoal(id);
        if (existing != null)
        {
            return existing;
        }

        var goal = new CosmereGoalRecord
        {
            Id = id.Trim(),
            Title = string.IsNullOrWhiteSpace(title) ? id.Trim() : title.Trim(),
            Status = CosmereGoalStatus.Active,
            RewardKind = rewardKind,
            RewardPayload = rewardPayload ?? ""
        };

        sheet.Goals.Add(goal);
        pm.SendMessage($"Goal assigned: {goal.Title}.");
        CosmereDebug.Trace(
            pm,
            $"Goal assign id={goal.Id} reward={goal.RewardKind} payload={goal.RewardPayload ?? ""}"
        );
        return goal;
    }

    public static CosmereGoalRecord EnsureTrainYourPower(PlayerMobile pm)
    {
        if (pm == null)
        {
            return null;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        if (!sheet.IsMetalborn || sheet.PowerTier != CosmerePowerTier.Nascent)
        {
            return sheet.FindGoal(TrainYourPowerId);
        }

        return Assign(
            pm,
            TrainYourPowerId,
            TrainYourPowerTitle,
            CosmereGoalRewardKind.UnlockFullAllomancy
        );
    }

    public static bool Complete(PlayerMobile pm, string id)
    {
        if (pm == null || string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;
        var goal = sheet.FindGoal(id);
        if (goal == null)
        {
            pm.SendMessage($"No goal '{id}'.");
            return false;
        }

        if (goal.Status == CosmereGoalStatus.Completed)
        {
            pm.SendMessage($"Goal '{goal.Title}' already completed.");
            return false;
        }

        goal.Status = CosmereGoalStatus.Completed;
        ApplyReward(pm, goal);
        pm.SendMessage($"Goal completed: {goal.Title}.");
        CosmereDebug.Trace(
            pm,
            $"Goal complete id={goal.Id} reward={goal.RewardKind} payload={goal.RewardPayload ?? ""}"
        );
        return true;
    }

    public static bool Fail(PlayerMobile pm, string id)
    {
        if (pm == null || string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        pm.EnsureCosmereSheet();
        var goal = pm.CosmereSheet.FindGoal(id);
        if (goal == null)
        {
            pm.SendMessage($"No goal '{id}'.");
            return false;
        }

        if (goal.Status != CosmereGoalStatus.Active)
        {
            pm.SendMessage($"Goal '{goal.Title}' is not active.");
            return false;
        }

        goal.Status = CosmereGoalStatus.Failed;
        pm.SendMessage($"Goal failed: {goal.Title}.");
        return true;
    }

    public static void MarkTrainYourPowerCompletedIfActive(PlayerMobile pm)
    {
        if (pm?.CosmereSheet == null)
        {
            return;
        }

        var goal = pm.CosmereSheet.FindGoal(TrainYourPowerId);
        if (goal is { Status: CosmereGoalStatus.Active })
        {
            goal.Status = CosmereGoalStatus.Completed;
            pm.SendMessage($"Goal completed: {goal.Title}.");
        }
    }

    public static void List(PlayerMobile pm)
    {
        if (pm == null)
        {
            return;
        }

        pm.EnsureCosmereSheet();
        var goals = pm.CosmereSheet.Goals;
        if (goals.Count == 0)
        {
            pm.SendMessage("No Cosmere goals.");
            return;
        }

        pm.SendMessage($"Cosmere goals ({goals.Count}):");
        for (var i = 0; i < goals.Count; i++)
        {
            var g = goals[i];
            pm.SendMessage($"  [{g.Status}] {g.Id}: {g.Title} ({g.RewardKind})");
        }
    }

    private static void ApplyReward(PlayerMobile pm, CosmereGoalRecord goal)
    {
        switch (goal.RewardKind)
        {
            case CosmereGoalRewardKind.UnlockFullAllomancy:
                CosmereAllomancy.UnlockFullPowerSilent(pm);
                break;
            case CosmereGoalRewardKind.GrantTalent:
                if (!string.IsNullOrWhiteSpace(goal.RewardPayload))
                {
                    var sheet = pm.CosmereSheet;
                    if (!sheet.HasTalent(goal.RewardPayload))
                    {
                        sheet.Talents.Add(goal.RewardPayload.Trim());
                        pm.SendMessage($"Talent granted: {goal.RewardPayload}.");
                    }
                }

                break;
            case CosmereGoalRewardKind.GrantItemStub:
                pm.SendMessage(
                    string.IsNullOrWhiteSpace(goal.RewardPayload)
                        ? "Reward: item stub (no payload)."
                        : $"Would grant item '{goal.RewardPayload}' (stub)."
                );
                break;
        }
    }

    public static bool TryParseRewardKind(string text, out CosmereGoalRewardKind kind)
    {
        kind = CosmereGoalRewardKind.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.Equals("unlock", StringComparison.OrdinalIgnoreCase)
            || text.Equals("allomancy", StringComparison.OrdinalIgnoreCase)
            || text.Equals("full", StringComparison.OrdinalIgnoreCase))
        {
            kind = CosmereGoalRewardKind.UnlockFullAllomancy;
            return true;
        }

        if (text.Equals("talent", StringComparison.OrdinalIgnoreCase))
        {
            kind = CosmereGoalRewardKind.GrantTalent;
            return true;
        }

        if (text.Equals("item", StringComparison.OrdinalIgnoreCase)
            || text.Equals("itemstub", StringComparison.OrdinalIgnoreCase))
        {
            kind = CosmereGoalRewardKind.GrantItemStub;
            return true;
        }

        return Enum.TryParse(text.Trim(), true, out kind);
    }
}
