using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere.Events;

public static class CosmereEventActions
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(CosmereEventActions));

    public static void Execute(
        CosmereEventInstance instance,
        CosmereEventDefinition definition,
        IReadOnlyList<CosmereEventActionDef> actions,
        CosmereEventActionContext context = default
    )
    {
        if (actions == null || actions.Count == 0)
        {
            return;
        }

        for (var i = 0; i < actions.Count; i++)
        {
            ExecuteOne(instance, definition, actions[i], context);
        }
    }

    public static void ExecuteOne(
        CosmereEventInstance instance,
        CosmereEventDefinition definition,
        CosmereEventActionDef action,
        CosmereEventActionContext context = default
    )
    {
        if (action == null || string.IsNullOrWhiteSpace(action.Type))
        {
            return;
        }

        switch (action.Type.Trim())
        {
            case "Announce":
                Announce(definition, action);
                break;
            case "GotoPhase":
                if (!string.IsNullOrWhiteSpace(action.PhaseId))
                {
                    CosmereEventRuntime.GotoPhase(instance, action.PhaseId);
                }

                break;
            case "EndEvent":
                CosmereEventRuntime.End(instance, cancelled: false, action.Reason);
                break;
            case "GrantGoal":
                GrantGoal(definition, action);
                break;
            case "CompleteGoal":
                CompleteOrFailGoal(definition, action, complete: true);
                break;
            case "FailGoal":
                CompleteOrFailGoal(definition, action, complete: false);
                break;
            case "SpawnTemp":
                SpawnTemp(instance, definition, action, context);
                break;
            case "DespawnOwned":
                CosmereEventRuntime.DespawnOwned(instance);
                break;
            case "SetFlag":
                SetFlag(instance, action);
                break;
            default:
                _logger.Warning(
                    "Unknown Cosmere event action type {Type} in {Definition}",
                    action.Type,
                    definition?.Id
                );
                break;
        }
    }

    private static void Announce(CosmereEventDefinition definition, CosmereEventActionDef action)
    {
        var message = action.Message ?? "";
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var audience = action.Audience ?? "All";
        foreach (var state in NetState.Instances)
        {
            if (state.Mobile is not PlayerMobile pm || pm.Deleted)
            {
                continue;
            }

            if (audience.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                if (pm.AccessLevel < AccessLevel.Counselor)
                {
                    continue;
                }
            }
            else if (audience.Equals("Region", StringComparison.OrdinalIgnoreCase) &&
                     definition.Scope == CosmereEventScope.Regional)
            {
                if (!CosmereEventCatalog.Contains(definition, pm.Location, pm.Map))
                {
                    continue;
                }
            }

            pm.SendMessage(0x35, message);
        }
    }

    private static void GrantGoal(CosmereEventDefinition definition, CosmereEventActionDef action)
    {
        if (string.IsNullOrWhiteSpace(action.GoalId))
        {
            return;
        }

        foreach (var state in NetState.Instances)
        {
            if (state.Mobile is not PlayerMobile pm || pm.Deleted)
            {
                continue;
            }

            if (definition.Scope == CosmereEventScope.Regional &&
                !CosmereEventCatalog.Contains(definition, pm.Location, pm.Map))
            {
                continue;
            }

            CosmereGoals.Assign(pm, action.GoalId, action.GoalTitle ?? action.GoalId, CosmereGoalRewardKind.None);
        }
    }

    private static void CompleteOrFailGoal(
        CosmereEventDefinition definition,
        CosmereEventActionDef action,
        bool complete
    )
    {
        if (string.IsNullOrWhiteSpace(action.GoalId))
        {
            return;
        }

        foreach (var state in NetState.Instances)
        {
            if (state.Mobile is not PlayerMobile pm || pm.Deleted)
            {
                continue;
            }

            if (definition.Scope == CosmereEventScope.Regional &&
                !CosmereEventCatalog.Contains(definition, pm.Location, pm.Map))
            {
                continue;
            }

            if (complete)
            {
                CosmereGoals.Complete(pm, action.GoalId);
            }
            else
            {
                CosmereGoals.Fail(pm, action.GoalId);
            }
        }
    }

    private static void SetFlag(CosmereEventInstance instance, CosmereEventActionDef action)
    {
        if (string.IsNullOrWhiteSpace(action.FlagKey))
        {
            return;
        }

        var scope = action.FlagScope ?? "Instance";
        if (scope.Equals("World", StringComparison.OrdinalIgnoreCase))
        {
            CosmereEventRuntime.SetWorldFlag(action.FlagKey, action.FlagValue ?? "1");
        }
        else
        {
            instance.Flags[action.FlagKey] = action.FlagValue ?? "1";
        }
    }

    private static void SpawnTemp(
        CosmereEventInstance instance,
        CosmereEventDefinition definition,
        CosmereEventActionDef action,
        CosmereEventActionContext context
    )
    {
        if (string.IsNullOrWhiteSpace(action.CreatureType))
        {
            _logger.Warning("SpawnTemp missing CreatureType in {Definition}", definition.Id);
            return;
        }

        var type = AssemblyHandler.FindTypeByName(action.CreatureType);
        if (type == null || !typeof(BaseCreature).IsAssignableFrom(type))
        {
            _logger.Warning(
                "SpawnTemp invalid creature type {Type} in {Definition}",
                action.CreatureType,
                definition.Id
            );
            return;
        }

        var count = Math.Clamp(action.Count <= 0 ? 1 : action.Count, 1, 50);
        var mode = action.LocationMode ?? "RandomInRegion";

        for (var i = 0; i < count; i++)
        {
            if (!TryResolveLocation(definition, action, mode, context, out var loc, out var map))
            {
                continue;
            }

            BaseCreature creature;
            try
            {
                creature = type.CreateInstance<BaseCreature>();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "SpawnTemp failed to construct {Type}", action.CreatureType);
                return;
            }

            if (creature == null)
            {
                continue;
            }

            creature.MoveToWorld(loc, map);
            instance.OwnedSerials.Add(creature.Serial);
            CosmereEventRuntime.RegisterOwnedSpawn(creature.Serial, instance.InstanceId);

            if (action.LifespanMinutes is > 0)
            {
                var serial = creature.Serial;
                var instanceId = instance.InstanceId;
                Timer.StartTimer(
                    TimeSpan.FromMinutes(action.LifespanMinutes.Value),
                    () => CosmereEventRuntime.DespawnSerial(instanceId, serial)
                );
            }
        }
    }

    private static bool TryResolveLocation(
        CosmereEventDefinition definition,
        CosmereEventActionDef action,
        string mode,
        CosmereEventActionContext context,
        out Point3D loc,
        out Map map
    )
    {
        loc = Point3D.Zero;
        map = null;

        if (mode.Equals("At", StringComparison.OrdinalIgnoreCase))
        {
            map = CosmereEventCatalog.ResolveMap(action.Map) ??
                  CosmereEventCatalog.ResolveMap(definition.Map) ??
                  Map.Felucca;
            loc = new Point3D(action.X, action.Y, action.Z);
            return map != null && map != Map.Internal;
        }

        if (mode.Equals("NearKiller", StringComparison.OrdinalIgnoreCase) &&
            context.HasLocation)
        {
            map = context.Map;
            loc = context.Location;
            return map != null && map != Map.Internal;
        }

        // RandomInRegion / default
        if (definition.Scope == CosmereEventScope.Regional)
        {
            map = CosmereEventCatalog.ResolveMap(definition.Map);
            if (map == null || map == Map.Internal || definition.Width <= 0 || definition.Height <= 0)
            {
                return false;
            }

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var x = definition.X + Utility.Random(definition.Width);
                var y = definition.Y + Utility.Random(definition.Height);
                var z = map.GetAverageZ(x, y);
                loc = new Point3D(x, y, z);
                if (map.CanSpawnMobile(loc))
                {
                    return true;
                }
            }

            loc = new Point3D(
                definition.X + definition.Width / 2,
                definition.Y + definition.Height / 2,
                map.GetAverageZ(definition.X + definition.Width / 2, definition.Y + definition.Height / 2)
            );
            return true;
        }

        if (context.HasLocation)
        {
            map = context.Map;
            loc = context.Location;
            return map != null && map != Map.Internal;
        }

        map = Map.Felucca;
        loc = new Point3D(1400 + Utility.Random(40), 1600 + Utility.Random(40), 0);
        return true;
    }
}

public readonly struct CosmereEventActionContext
{
    public Point3D Location { get; init; }
    public Map Map { get; init; }
    public Serial KillerSerial { get; init; }
    public bool HasLocation => Map != null && Map != Map.Internal;
}
