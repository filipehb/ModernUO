using System;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public static class CosmereSustenance
{
    public const string ExhaustedSource = "sustenance";
    public const int LowBandThreshold = 5;
    private static readonly TimeSpan ReminderThrottle = TimeSpan.FromMinutes(3);

    public static bool IsEmpty(Mobile m) => m.Hunger <= 0 || m.Thirst <= 0;

    public static bool IsLow(Mobile m) => m.Hunger < LowBandThreshold || m.Thirst < LowBandThreshold;

    public static void Sync(PlayerMobile pm)
    {
        if (pm == null)
        {
            return;
        }

        pm.EnsureCosmereSheet();
        var sheet = pm.CosmereSheet;

        if (!CosmereConfig.HungerEffectsEnabled)
        {
            ClearSustenanceExhausted(sheet);
            if (sheet.FocusSoftPenalty != 0)
            {
                sheet.FocusSoftPenalty = 0;
                sheet.ClampPools();
            }

            return;
        }

        var empty = IsEmpty(pm);
        var low = IsLow(pm);

        if (empty)
        {
            EnsureSustenanceExhausted(sheet);
        }
        else
        {
            ClearSustenanceExhausted(sheet);
        }

        var penalty = low ? 1 : 0;
        if (sheet.FocusSoftPenalty != penalty)
        {
            sheet.FocusSoftPenalty = penalty;
            sheet.ClampPools();
            CosmereDebug.Trace(pm, $"Sustenance FocusSoftPenalty={penalty} MaxFocus={sheet.MaxFocus}");
        }

        MaybeRemind(pm, empty, low);
    }

    private static void EnsureSustenanceExhausted(CosmereSheet sheet)
    {
        for (var i = 0; i < sheet.Conditions.Count; i++)
        {
            if (sheet.Conditions[i].Kind == CosmereConditionKind.Exhausted
                && sheet.Conditions[i].Source == ExhaustedSource)
            {
                return;
            }
        }

        sheet.AddExhausted(1, ExhaustedSource);
    }

    private static void ClearSustenanceExhausted(CosmereSheet sheet)
    {
        for (var i = sheet.Conditions.Count - 1; i >= 0; i--)
        {
            var condition = sheet.Conditions[i];
            if (condition.Kind != CosmereConditionKind.Exhausted
                || condition.Source != ExhaustedSource)
            {
                continue;
            }

            condition.Magnitude -= 1;
            if (condition.Magnitude <= 0)
            {
                sheet.Conditions.RemoveAt(i);
            }
            else
            {
                // Leave remaining Exhausted from other causes without sustenance tag.
                condition.Source = null;
            }
        }
    }

    private static void MaybeRemind(PlayerMobile pm, bool empty, bool low)
    {
        if (!low)
        {
            return;
        }

        if (Core.Now - pm.CosmereSustenanceLastReminder < ReminderThrottle)
        {
            return;
        }

        pm.CosmereSustenanceLastReminder = Core.Now;

        if (empty)
        {
            pm.SendMessage("You are starving or parched — Exhausted until you eat and drink.");
        }
        else
        {
            pm.SendMessage("You feel hungry or thirsty. Your Focus suffers until you recover.");
        }
    }
}
