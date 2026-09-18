using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public static class CosmereLivingWorld
{
    public const int MaxHuntAttemptsPerTick = 16;

    private static readonly Type[] _preyTypes = [typeof(Rabbit), typeof(JackRabbit)];

    private static readonly HashSet<Type> _predatorTypes =
    [
        typeof(DireWolf),
        typeof(TimberWolf),
        typeof(GreyWolf),
        typeof(WhiteWolf)
    ];

    private static Timer _timer;

    public static void Initialize()
    {
        _timer?.Stop();
        var seconds = CosmereConfig.LivingWorldTickSeconds;
        if (!CosmereConfig.LivingWorldEnabled || seconds <= 0)
        {
            return;
        }

        var interval = TimeSpan.FromSeconds(seconds);
        _timer = new LivingWorldTimer(interval);
        _timer.Start();
    }

    public static bool IsLivingWorldPresence(Mobile m)
    {
        if (m?.Deleted != false || m.NetState == null || m is not PlayerMobile)
        {
            return false;
        }

        return CountsAsLivingWorldAccess(m);
    }

    /// <summary>Access-level half of presence (unit-testable without a NetState).</summary>
    public static bool CountsAsLivingWorldAccess(Mobile m)
    {
        if (m == null)
        {
            return false;
        }

        if (m.AccessLevel == AccessLevel.Player)
        {
            return true;
        }

        return CosmereConfig.StaffWakesLivingWorld;
    }

    public static bool IsPredatorType(Type type) => type != null && _predatorTypes.Contains(type);

    public static bool IsPreyType(Type type)
    {
        if (type == null)
        {
            return false;
        }

        for (var i = 0; i < _preyTypes.Length; i++)
        {
            if (_preyTypes[i] == type)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsEligiblePredator(BaseCreature creature)
    {
        if (creature?.Deleted != false || !creature.Alive)
        {
            return false;
        }

        if (!IsPredatorType(creature.GetType()))
        {
            return false;
        }

        if (creature.Controlled || creature.Summoned)
        {
            return false;
        }

        var combatant = creature.Combatant;
        if (combatant is { Deleted: false, Alive: true })
        {
            return false;
        }

        return true;
    }

    public static bool IsEligiblePrey(BaseCreature creature)
    {
        if (creature?.Deleted != false || !creature.Alive)
        {
            return false;
        }

        if (!IsPreyType(creature.GetType()))
        {
            return false;
        }

        return !creature.Controlled && !creature.Summoned;
    }

    public static void Tick()
    {
        if (!CosmereConfig.LivingWorldEnabled || CosmereConfig.LivingWorldTickSeconds <= 0)
        {
            return;
        }

        var playerRange = CosmereConfig.LivingWorldPlayerRange;
        var huntRange = CosmereConfig.LivingWorldHuntRange;
        if (playerRange < 1 || huntRange < 1)
        {
            return;
        }

        var attempts = 0;
        // Avoid hunting the same predator twice when two players share range.
        Span<Serial> hunted = stackalloc Serial[MaxHuntAttemptsPerTick];
        var huntedCount = 0;

        foreach (var state in NetState.Instances)
        {
            if (attempts >= MaxHuntAttemptsPerTick)
            {
                break;
            }

            var m = state.Mobile;
            if (!IsLivingWorldPresence(m) || m.Map == null || m.Map == Map.Internal)
            {
                continue;
            }

            var map = m.Map;
            foreach (var creature in map.GetMobilesInRange<BaseCreature>(m.Location, playerRange))
            {
                if (attempts >= MaxHuntAttemptsPerTick)
                {
                    break;
                }

                if (!IsEligiblePredator(creature))
                {
                    continue;
                }

                if (AlreadyHunted(hunted, huntedCount, creature.Serial))
                {
                    continue;
                }

                if (!TryHunt(creature, huntRange, m as PlayerMobile))
                {
                    continue;
                }

                if (huntedCount < hunted.Length)
                {
                    hunted[huntedCount++] = creature.Serial;
                }

                attempts++;
            }
        }
    }

    public static bool TryHunt(BaseCreature predator, int huntRange, PlayerMobile nearbyPlayer = null)
    {
        if (!IsEligiblePredator(predator) || predator.Map == null)
        {
            return false;
        }

        BaseCreature bestPrey = null;
        var bestDist = double.MaxValue;

        foreach (var candidate in predator.Map.GetMobilesInRange<BaseCreature>(predator.Location, huntRange))
        {
            if (candidate == predator || !IsEligiblePrey(candidate))
            {
                continue;
            }

            var dist = predator.GetDistanceToSqrt(candidate);
            if (dist >= bestDist)
            {
                continue;
            }

            bestDist = dist;
            bestPrey = candidate;
        }

        if (bestPrey == null)
        {
            return false;
        }

        predator.Warmode = true;
        predator.Combatant = bestPrey;

        if (nearbyPlayer != null)
        {
            CosmereDebug.Trace(
                nearbyPlayer,
                $"LivingWorld hunt: {predator.GetType().Name} → {bestPrey.GetType().Name}"
            );
        }

        return true;
    }

    private static bool AlreadyHunted(Span<Serial> hunted, int count, Serial serial)
    {
        for (var i = 0; i < count; i++)
        {
            if (hunted[i] == serial)
            {
                return true;
            }
        }

        return false;
    }

    private class LivingWorldTimer : Timer
    {
        public LivingWorldTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Tick();
    }
}
