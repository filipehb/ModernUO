using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public static class CosmereTime
{
    private static CosmereDayPeriodTimer _timer;

    public static void Initialize()
    {
        _timer?.Stop();
        _timer = new CosmereDayPeriodTimer();
        _timer.Start();
    }

    public static CosmereDayPeriod GetPeriod(int hours) =>
        hours switch
        {
            >= 6 and <= 11 => CosmereDayPeriod.Morning,
            >= 12 and <= 16 => CosmereDayPeriod.Afternoon,
            >= 17 and <= 21 => CosmereDayPeriod.Evening,
            _ => CosmereDayPeriod.Night
        };

    public static CosmereDayPeriod GetPeriod(Map map, int x, int y)
    {
        Clock.GetTime(map, x, y, out var hours, out int _);
        return GetPeriod(hours);
    }

    public static CosmereDayPeriod GetPeriod(Mobile m)
    {
        if (m?.Map == null)
        {
            return CosmereDayPeriod.Night;
        }

        return GetPeriod(m.Map, m.X, m.Y);
    }

    public static string Describe(CosmereDayPeriod period) =>
        period switch
        {
            CosmereDayPeriod.Morning => "morning",
            CosmereDayPeriod.Afternoon => "afternoon",
            CosmereDayPeriod.Evening => "evening",
            _ => "night"
        };

    public static void TickPeriods()
    {
        if (!CosmereConfig.DayPeriodsEnabled)
        {
            return;
        }

        foreach (var state in NetState.Instances)
        {
            if (state.Mobile is not PlayerMobile pm || pm.Map == null)
            {
                continue;
            }

            var period = GetPeriod(pm);
            if (pm.CosmereLastDayPeriod is not { } previous)
            {
                pm.CosmereLastDayPeriod = period;
                continue;
            }

            if (previous == period)
            {
                continue;
            }

            pm.CosmereLastDayPeriod = period;
            OnPeriodChanged(pm, previous, period);
        }
    }

    private static void OnPeriodChanged(PlayerMobile pm, CosmereDayPeriod previous, CosmereDayPeriod next)
    {
        CosmereDebug.Trace(pm, $"Day period {previous} → {next}");

        if (CosmereConfig.AnnouncePeriodChange)
        {
            pm.SendMessage(
                next switch
                {
                    CosmereDayPeriod.Morning => "Dawn breaks over the ash — it is morning.",
                    CosmereDayPeriod.Afternoon => "The day settles into afternoon.",
                    CosmereDayPeriod.Evening => "Evening gathers under the ashen sky.",
                    _ => "Night falls. Mist may rise."
                }
            );
        }

        // Night light bonus is applied in ComputeBaseLightLevels; force a resend on transition.
        pm.CheckLightLevels(true);
    }

    private class CosmereDayPeriodTimer : Timer
    {
        public CosmereDayPeriodTimer() : base(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5.0))
        {
        }

        protected override void OnTick() => TickPeriods();
    }
}
