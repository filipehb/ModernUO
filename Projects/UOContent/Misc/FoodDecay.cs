using System;
using Server.Engines.Cosmere;
using Server.Network;

namespace Server.Misc
{
    public class FoodDecayTimer : Timer
    {
        public FoodDecayTimer() : base(GetInterval(), GetInterval())
        {
        }

        public static void Initialize()
        {
            new FoodDecayTimer().Start();
        }

        private static TimeSpan GetInterval()
        {
            var minutes = CosmereConfig.HungerDecayMinutes;
            if (minutes <= 0 || CosmereConfig.HungerDecayAmount <= 0)
            {
                // Keep a slow heartbeat so config reloads are not required to wake a stopped timer.
                return TimeSpan.FromHours(1);
            }

            return TimeSpan.FromMinutes(minutes);
        }

        protected override void OnTick()
        {
            // Interval is fixed at construction; re-read amount/disable each tick.
            if (CosmereConfig.HungerDecayMinutes <= 0 || CosmereConfig.HungerDecayAmount <= 0)
            {
                return;
            }

            FoodDecay();
        }

        public static void FoodDecay()
        {
            var amount = CosmereConfig.HungerDecayAmount;
            if (amount <= 0)
            {
                return;
            }

            foreach (var state in NetState.Instances)
            {
                HungerDecay(state.Mobile, amount);
                ThirstDecay(state.Mobile, amount);
            }
        }

        public static void HungerDecay(Mobile m, int amount = 1)
        {
            if (m == null || amount <= 0 || m.Hunger < 1)
            {
                return;
            }

            m.Hunger = Math.Max(0, m.Hunger - amount);
        }

        public static void ThirstDecay(Mobile m, int amount = 1)
        {
            if (m == null || amount <= 0 || m.Thirst < 1)
            {
                return;
            }

            m.Thirst = Math.Max(0, m.Thirst - amount);
        }
    }
}
