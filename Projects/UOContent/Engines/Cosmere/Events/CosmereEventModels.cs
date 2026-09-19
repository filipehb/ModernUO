using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Server.Engines.Events;

namespace Server.Engines.Cosmere.Events;

public enum CosmereEventScope
{
    Global,
    Regional
}

public enum CosmereEventStartMode
{
    Manual,
    Schedule,
    Trigger,
    Composite
}

public enum CosmereEventEndMode
{
    Manual,
    Duration,
    Deadline,
    Trigger
}

public enum CosmereEventInstanceState
{
    Scheduled,
    Running,
    Ended,
    Cancelled
}

public class CosmereEventDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string StaffNotes { get; set; } = "";
    public CosmereEventScope Scope { get; set; } = CosmereEventScope.Global;
    public string Map { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Enabled { get; set; } = true;
    public CosmereEventStartMode StartMode { get; set; } = CosmereEventStartMode.Manual;
    public CosmereEventEndMode EndMode { get; set; } = CosmereEventEndMode.Manual;
    public double? DurationMinutes { get; set; }
    public DateTime? DeadlineUtc { get; set; }
    public int MaxConcurrentInstances { get; set; } = 1;
    public List<CosmereEventTriggerDef> StartTriggers { get; set; } = [];
    public List<CosmereEventPhaseDef> Phases { get; set; } = [];
}

public class CosmereEventPhaseDef
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public List<CosmereEventActionDef> OnEnter { get; set; } = [];
    public List<CosmereEventTriggerDef> Triggers { get; set; } = [];
    public double? AutoAdvanceAfterMinutes { get; set; }
}

public class CosmereEventTriggerDef
{
    public string Type { get; set; } = "";
    public string CreatureType { get; set; }
    public string CreatureName { get; set; }
    public bool RegionalFilter { get; set; }
    public string ScheduleTime { get; set; }
    public string ScheduleRecurrence { get; set; }
    public double? AfterMinutes { get; set; }
    public string GotoPhase { get; set; }
    public bool EndEvent { get; set; }
    public List<CosmereEventActionDef> Actions { get; set; } = [];
}

public class CosmereEventActionDef
{
    public string Type { get; set; } = "";
    public string Message { get; set; }
    public string Audience { get; set; }
    public string PhaseId { get; set; }
    public string Reason { get; set; }
    public string GoalId { get; set; }
    public string GoalTitle { get; set; }
    public string CreatureType { get; set; }
    public int Count { get; set; } = 1;
    public string LocationMode { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public string Map { get; set; }
    public double? LifespanMinutes { get; set; }
    public string FlagKey { get; set; }
    public string FlagValue { get; set; }
    public string FlagScope { get; set; }
}

public class CosmereEventInstance
{
    public int InstanceId { get; set; }
    public string DefinitionId { get; set; } = "";
    public CosmereEventInstanceState State { get; set; } = CosmereEventInstanceState.Running;
    public string PhaseId { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public DateTime EndsUtc { get; set; } = DateTime.MinValue;
    public DateTime PhaseEnteredUtc { get; set; }
    public List<Serial> OwnedSerials { get; } = [];
    public Dictionary<string, string> Flags { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Serial TriggerContextSerial { get; set; }
    public Point3D TriggerContextLocation { get; set; }
    public string TriggerContextMap { get; set; }

    [JsonIgnore]
    public TimerExecutionToken DurationToken;

    [JsonIgnore]
    public TimerExecutionToken PhaseAdvanceToken;

    [JsonIgnore]
    public List<BaseScheduledEvent> ScheduledHandles { get; } = [];
}
