using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.Events;
using Server.Logging;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere.Events;

public static class CosmereEventRuntime
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(CosmereEventRuntime));
    private static readonly List<CosmereEventInstance> _instances = [];
    private static readonly Dictionary<Serial, int> _ownedSpawnToInstance = [];
    private static readonly Dictionary<string, string> _worldFlags = new(StringComparer.OrdinalIgnoreCase);
    private static int _nextInstanceId = 1;
    private static readonly List<BaseScheduledEvent> _definitionSchedules = [];

    public static IReadOnlyList<CosmereEventInstance> Instances => _instances;

    public static void Configure()
    {
        CosmereEventPersistence.Configure();
    }

    public static void Initialize()
    {
        if (!CosmereConfig.EventsEnabled)
        {
            return;
        }

        ArmDefinitionSchedules();
        for (var i = 0; i < _instances.Count; i++)
        {
            var inst = _instances[i];
            if (inst.State != CosmereEventInstanceState.Running)
            {
                continue;
            }

            ReconcileOwned(inst);
            ArmInstanceTimers(inst);
        }
    }

    public static IEnumerable<CosmereEventInstance> GetRunning()
    {
        for (var i = 0; i < _instances.Count; i++)
        {
            if (_instances[i].State == CosmereEventInstanceState.Running)
            {
                yield return _instances[i];
            }
        }
    }

    public static int CountRunning(string definitionId)
    {
        var count = 0;
        for (var i = 0; i < _instances.Count; i++)
        {
            var inst = _instances[i];
            if (inst.State == CosmereEventInstanceState.Running &&
                inst.DefinitionId.Equals(definitionId, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    public static bool TryStart(
        string definitionId,
        out CosmereEventInstance instance,
        out string error,
        CosmereEventActionContext context = default
    )
    {
        instance = null;
        error = null;

        if (!CosmereConfig.EventsEnabled)
        {
            error = "Cosmere events are disabled.";
            return false;
        }

        if (!CosmereEventCatalog.TryGet(definitionId, out var def) || !def.Enabled)
        {
            error = "Event definition not found or disabled.";
            return false;
        }

        if (CountRunning(def.Id) >= def.MaxConcurrentInstances)
        {
            error = "Max concurrent instances reached.";
            return false;
        }

        if (def.Phases.Count == 0)
        {
            error = "Event has no phases.";
            return false;
        }

        instance = new CosmereEventInstance
        {
            InstanceId = _nextInstanceId++,
            DefinitionId = def.Id,
            State = CosmereEventInstanceState.Running,
            PhaseId = def.Phases[0].Id,
            StartedUtc = DateTime.UtcNow,
            PhaseEnteredUtc = DateTime.UtcNow,
            TriggerContextSerial = context.KillerSerial,
            TriggerContextLocation = context.Location,
            TriggerContextMap = context.Map?.Name
        };

        if (def.EndMode == CosmereEventEndMode.Duration && def.DurationMinutes is > 0)
        {
            instance.EndsUtc = instance.StartedUtc.AddMinutes(def.DurationMinutes.Value);
        }
        else if (def.EndMode == CosmereEventEndMode.Deadline && def.DeadlineUtc is { } deadline)
        {
            instance.EndsUtc = deadline.Kind == DateTimeKind.Utc
                ? deadline
                : deadline.ToUniversalTime();
        }

        _instances.Add(instance);
        EnterPhase(instance, def, def.Phases[0], context);
        ArmInstanceTimers(instance);
        return true;
    }

    public static bool TryStopDefinition(string definitionId, bool cancel, out int stopped)
    {
        stopped = 0;
        for (var i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            if (inst.State != CosmereEventInstanceState.Running ||
                !inst.DefinitionId.Equals(definitionId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            End(inst, cancel, cancel ? "Cancelled by staff" : "Stopped by staff");
            stopped++;
        }

        return stopped > 0;
    }

    public static void End(CosmereEventInstance instance, bool cancelled, string reason = null)
    {
        if (instance == null ||
            instance.State is CosmereEventInstanceState.Ended or CosmereEventInstanceState.Cancelled)
        {
            return;
        }

        CancelInstanceTimers(instance);
        DespawnOwned(instance);
        instance.State = cancelled ? CosmereEventInstanceState.Cancelled : CosmereEventInstanceState.Ended;
        _logger.Information(
            "Cosmere event {Definition} instance {Id} ended ({State}): {Reason}",
            instance.DefinitionId,
            instance.InstanceId,
            instance.State,
            reason ?? ""
        );
    }

    public static void GotoPhase(CosmereEventInstance instance, string phaseId)
    {
        if (instance?.State != CosmereEventInstanceState.Running)
        {
            return;
        }

        if (!CosmereEventCatalog.TryGet(instance.DefinitionId, out var def))
        {
            return;
        }

        CosmereEventPhaseDef phase = null;
        for (var i = 0; i < def.Phases.Count; i++)
        {
            if (def.Phases[i].Id.Equals(phaseId, StringComparison.OrdinalIgnoreCase))
            {
                phase = def.Phases[i];
                break;
            }
        }

        if (phase == null)
        {
            _logger.Warning("GotoPhase unknown phase {Phase} in {Definition}", phaseId, def.Id);
            return;
        }

        CancelPhaseAdvance(instance);
        instance.PhaseId = phase.Id;
        instance.PhaseEnteredUtc = DateTime.UtcNow;
        EnterPhase(instance, def, phase, BuildContext(instance));
        ArmPhaseAdvance(instance, def, phase);
    }

    public static void DespawnOwned(CosmereEventInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        for (var i = instance.OwnedSerials.Count - 1; i >= 0; i--)
        {
            var serial = instance.OwnedSerials[i];
            DespawnSerial(instance.InstanceId, serial);
        }

        instance.OwnedSerials.Clear();
    }

    public static void DespawnSerial(int instanceId, Serial serial)
    {
        _ownedSpawnToInstance.Remove(serial);
        var mob = World.FindMobile(serial);
        if (mob is { Deleted: false })
        {
            mob.Delete();
        }

        for (var i = 0; i < _instances.Count; i++)
        {
            var inst = _instances[i];
            if (inst.InstanceId != instanceId)
            {
                continue;
            }

            inst.OwnedSerials.Remove(serial);
            break;
        }
    }

    public static void RegisterOwnedSpawn(Serial serial, int instanceId) =>
        _ownedSpawnToInstance[serial] = instanceId;

    public static void SetWorldFlag(string key, string value) => _worldFlags[key] = value;

    public static bool TryGetWorldFlag(string key, out string value) => _worldFlags.TryGetValue(key, out value);

    public static void HandleTrigger(
        CosmereEventInstance instance,
        CosmereEventDefinition definition,
        CosmereEventTriggerDef trigger,
        CosmereEventActionContext context
    )
    {
        if (trigger.Actions is { Count: > 0 })
        {
            CosmereEventActions.Execute(instance, definition, trigger.Actions, context);
        }

        if (!string.IsNullOrWhiteSpace(trigger.GotoPhase))
        {
            GotoPhase(instance, trigger.GotoPhase);
        }

        if (trigger.EndEvent)
        {
            End(instance, cancelled: false, "Trigger ended event");
        }
    }

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        if (!CosmereConfig.EventsEnabled || bc == null || bc.Deleted)
        {
            return;
        }

        var context = new CosmereEventActionContext
        {
            Location = bc.Location,
            Map = bc.Map,
            KillerSerial = bc.LastKiller?.Serial ?? Serial.Zero
        };

        // Start triggers on definitions not yet running
        foreach (var pair in CosmereEventCatalog.Definitions)
        {
            var def = pair.Value;
            if (!def.Enabled)
            {
                continue;
            }

            if (def.StartMode is not (CosmereEventStartMode.Trigger or CosmereEventStartMode.Composite))
            {
                continue;
            }

            if (CountRunning(def.Id) >= def.MaxConcurrentInstances)
            {
                continue;
            }

            if (!MatchesDeathTrigger(def.StartTriggers, bc, def, out var startTrigger))
            {
                continue;
            }

            if (TryStart(def.Id, out var started, out _, context) && startTrigger != null)
            {
                HandleTrigger(started, def, startTrigger, context);
            }
        }

        // Phase triggers on running instances
        for (var i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            if (inst.State != CosmereEventInstanceState.Running)
            {
                continue;
            }

            if (!CosmereEventCatalog.TryGet(inst.DefinitionId, out var def))
            {
                continue;
            }

            var phase = FindPhase(def, inst.PhaseId);
            if (phase?.Triggers == null)
            {
                continue;
            }

            if (!MatchesDeathTrigger(phase.Triggers, bc, def, out var trigger) || trigger == null)
            {
                continue;
            }

            HandleTrigger(inst, def, trigger, context);
        }

        if (_ownedSpawnToInstance.Remove(bc.Serial, out var instanceId))
        {
            for (var i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].InstanceId == instanceId)
                {
                    _instances[i].OwnedSerials.Remove(bc.Serial);
                    break;
                }
            }
        }
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnPlayerLogin(PlayerMobile pm)
    {
        if (!CosmereConfig.EventsEnabled || pm == null)
        {
            return;
        }

        // EnterBounds check once on login for regional running events
        for (var i = 0; i < _instances.Count; i++)
        {
            var inst = _instances[i];
            if (inst.State != CosmereEventInstanceState.Running)
            {
                continue;
            }

            if (!CosmereEventCatalog.TryGet(inst.DefinitionId, out var def))
            {
                continue;
            }

            if (def.Scope != CosmereEventScope.Regional ||
                !CosmereEventCatalog.Contains(def, pm.Location, pm.Map))
            {
                continue;
            }

            FireEnterBounds(inst, def, pm);
        }
    }

    public static void NotifyMovement(PlayerMobile pm, Point3D from, Point3D to, Map map)
    {
        if (!CosmereConfig.EventsEnabled || pm == null)
        {
            return;
        }

        for (var i = 0; i < _instances.Count; i++)
        {
            var inst = _instances[i];
            if (inst.State != CosmereEventInstanceState.Running)
            {
                continue;
            }

            if (!CosmereEventCatalog.TryGet(inst.DefinitionId, out var def) ||
                def.Scope != CosmereEventScope.Regional)
            {
                continue;
            }

            var wasInside = CosmereEventCatalog.Contains(def, from, map);
            var isInside = CosmereEventCatalog.Contains(def, to, map);
            if (!wasInside && isInside)
            {
                FireEnterBounds(inst, def, pm);
            }
        }
    }

    private static void FireEnterBounds(CosmereEventInstance inst, CosmereEventDefinition def, PlayerMobile pm)
    {
        var phase = FindPhase(def, inst.PhaseId);
        if (phase?.Triggers == null)
        {
            return;
        }

        var context = new CosmereEventActionContext
        {
            Location = pm.Location,
            Map = pm.Map,
            KillerSerial = pm.Serial
        };

        for (var t = 0; t < phase.Triggers.Count; t++)
        {
            var trigger = phase.Triggers[t];
            if (!string.Equals(trigger.Type, "EnterBounds", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            HandleTrigger(inst, def, trigger, context);
        }
    }

    private static bool MatchesDeathTrigger(
        List<CosmereEventTriggerDef> triggers,
        BaseCreature bc,
        CosmereEventDefinition def,
        out CosmereEventTriggerDef matched
    )
    {
        matched = null;
        if (triggers == null)
        {
            return false;
        }

        for (var i = 0; i < triggers.Count; i++)
        {
            var trigger = triggers[i];
            if (!string.Equals(trigger.Type, "CreatureDeath", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trigger.RegionalFilter && def.Scope == CosmereEventScope.Regional &&
                !CosmereEventCatalog.Contains(def, bc.Location, bc.Map))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(trigger.CreatureType))
            {
                var type = AssemblyHandler.FindTypeByName(trigger.CreatureType);
                if (type == null || !type.IsInstanceOfType(bc))
                {
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(trigger.CreatureName) &&
                !string.Equals(bc.Name, trigger.CreatureName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            matched = trigger;
            return true;
        }

        return false;
    }

    private static void EnterPhase(
        CosmereEventInstance instance,
        CosmereEventDefinition def,
        CosmereEventPhaseDef phase,
        CosmereEventActionContext context
    )
    {
        CosmereEventActions.Execute(instance, def, phase.OnEnter, context);
        ArmPhaseAdvance(instance, def, phase);
    }

    private static void ArmInstanceTimers(CosmereEventInstance instance)
    {
        if (!CosmereEventCatalog.TryGet(instance.DefinitionId, out var def))
        {
            return;
        }

        CancelInstanceTimers(instance);

        if (instance.EndsUtc != DateTime.MinValue && instance.EndsUtc > DateTime.UtcNow)
        {
            var delay = instance.EndsUtc - DateTime.UtcNow;
            Timer.StartTimer(
                delay,
                () =>
                {
                    if (instance.State == CosmereEventInstanceState.Running)
                    {
                        End(instance, cancelled: false, "Duration/deadline elapsed");
                    }
                },
                out instance.DurationToken
            );
        }

        var phase = FindPhase(def, instance.PhaseId);
        if (phase != null)
        {
            ArmPhaseAdvance(instance, def, phase);
            ArmPhaseDurationTriggers(instance, def, phase);
        }
    }

    private static void ArmPhaseAdvance(
        CosmereEventInstance instance,
        CosmereEventDefinition def,
        CosmereEventPhaseDef phase
    )
    {
        CancelPhaseAdvance(instance);
        if (phase.AutoAdvanceAfterMinutes is not > 0)
        {
            return;
        }

        Timer.StartTimer(
            TimeSpan.FromMinutes(phase.AutoAdvanceAfterMinutes.Value),
            () =>
            {
                if (instance.State != CosmereEventInstanceState.Running)
                {
                    return;
                }

                var index = def.Phases.FindIndex(
                    p => p.Id.Equals(phase.Id, StringComparison.OrdinalIgnoreCase)
                );
                if (index >= 0 && index + 1 < def.Phases.Count)
                {
                    GotoPhase(instance, def.Phases[index + 1].Id);
                }
                else
                {
                    End(instance, cancelled: false, "Final phase auto-advanced");
                }
            },
            out instance.PhaseAdvanceToken
        );
    }

    private static void ArmPhaseDurationTriggers(
        CosmereEventInstance instance,
        CosmereEventDefinition def,
        CosmereEventPhaseDef phase
    )
    {
        if (phase.Triggers == null)
        {
            return;
        }

        for (var i = 0; i < phase.Triggers.Count; i++)
        {
            var trigger = phase.Triggers[i];
            if (!string.Equals(trigger.Type, "DurationElapsed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trigger.AfterMinutes is not > 0)
            {
                continue;
            }

            var captured = trigger;
            Timer.StartTimer(
                TimeSpan.FromMinutes(trigger.AfterMinutes.Value),
                () =>
                {
                    if (instance.State != CosmereEventInstanceState.Running ||
                        !instance.PhaseId.Equals(phase.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    HandleTrigger(instance, def, captured, BuildContext(instance));
                }
            );
        }
    }

    private static void ArmDefinitionSchedules()
    {
        if (EventScheduler.Shared == null)
        {
            return;
        }

        for (var i = 0; i < _definitionSchedules.Count; i++)
        {
            _definitionSchedules[i].Cancel();
        }

        _definitionSchedules.Clear();

        foreach (var pair in CosmereEventCatalog.Definitions)
        {
            var def = pair.Value;
            if (!def.Enabled ||
                def.StartMode is not (CosmereEventStartMode.Schedule or CosmereEventStartMode.Composite))
            {
                continue;
            }

            if (def.StartTriggers == null)
            {
                continue;
            }

            for (var t = 0; t < def.StartTriggers.Count; t++)
            {
                var trigger = def.StartTriggers[t];
                if (!string.Equals(trigger.Type, "Schedule", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TimeOnly.TryParse(trigger.ScheduleTime, out var time))
                {
                    continue;
                }

                var recurrence = ResolveRecurrence(trigger.ScheduleRecurrence);
                var defId = def.Id;
                var scheduled = EventScheduler.Shared.ScheduleEvent(
                    DateTime.UtcNow,
                    time,
                    () =>
                    {
                        TryStart(defId, out _, out _);
                    },
                    recurrence
                );
                _definitionSchedules.Add(scheduled);
            }
        }
    }

    private static IRecurrencePattern ResolveRecurrence(string name) =>
        name?.Trim().ToLowerInvariant() switch
        {
            "weekly" => EventScheduler.Weekly,
            "hourly" => EventScheduler.Hourly,
            "once" => null,
            _ => EventScheduler.Daily
        };

    private static void CancelInstanceTimers(CosmereEventInstance instance)
    {
        instance.DurationToken.Cancel();
        CancelPhaseAdvance(instance);
        for (var i = 0; i < instance.ScheduledHandles.Count; i++)
        {
            instance.ScheduledHandles[i].Cancel();
        }

        instance.ScheduledHandles.Clear();
    }

    private static void CancelPhaseAdvance(CosmereEventInstance instance) =>
        instance.PhaseAdvanceToken.Cancel();

    private static CosmereEventPhaseDef FindPhase(CosmereEventDefinition def, string phaseId)
    {
        for (var i = 0; i < def.Phases.Count; i++)
        {
            if (def.Phases[i].Id.Equals(phaseId, StringComparison.OrdinalIgnoreCase))
            {
                return def.Phases[i];
            }
        }

        return null;
    }

    private static CosmereEventActionContext BuildContext(CosmereEventInstance instance)
    {
        Map map = null;
        if (!string.IsNullOrWhiteSpace(instance.TriggerContextMap))
        {
            map = CosmereEventCatalog.ResolveMap(instance.TriggerContextMap);
        }

        return new CosmereEventActionContext
        {
            Location = instance.TriggerContextLocation,
            Map = map,
            KillerSerial = instance.TriggerContextSerial
        };
    }

    private static void ReconcileOwned(CosmereEventInstance instance)
    {
        for (var i = instance.OwnedSerials.Count - 1; i >= 0; i--)
        {
            var serial = instance.OwnedSerials[i];
            var mob = World.FindMobile(serial);
            if (mob == null || mob.Deleted)
            {
                instance.OwnedSerials.RemoveAt(i);
                _ownedSpawnToInstance.Remove(serial);
            }
            else
            {
                _ownedSpawnToInstance[serial] = instance.InstanceId;
            }
        }
    }

    internal static void LoadState(
        int nextId,
        List<CosmereEventInstance> instances,
        Dictionary<string, string> worldFlags
    )
    {
        _nextInstanceId = Math.Max(1, nextId);
        _instances.Clear();
        _instances.AddRange(instances);
        _worldFlags.Clear();
        foreach (var pair in worldFlags)
        {
            _worldFlags[pair.Key] = pair.Value;
        }

        _ownedSpawnToInstance.Clear();
        for (var i = 0; i < _instances.Count; i++)
        {
            var inst = _instances[i];
            for (var s = 0; s < inst.OwnedSerials.Count; s++)
            {
                _ownedSpawnToInstance[inst.OwnedSerials[s]] = inst.InstanceId;
            }
        }
    }

    internal static void CaptureState(
        out int nextId,
        out List<CosmereEventInstance> instances,
        out Dictionary<string, string> worldFlags
    )
    {
        nextId = _nextInstanceId;
        instances = _instances;
        worldFlags = _worldFlags;
    }

    public static void ReloadSchedules() => ArmDefinitionSchedules();

    internal static void ResetForTests()
    {
        for (var i = _instances.Count - 1; i >= 0; i--)
        {
            CancelInstanceTimers(_instances[i]);
        }

        for (var i = 0; i < _definitionSchedules.Count; i++)
        {
            _definitionSchedules[i].Cancel();
        }

        _definitionSchedules.Clear();
        _instances.Clear();
        _ownedSpawnToInstance.Clear();
        _worldFlags.Clear();
        _nextInstanceId = 1;
    }
}
