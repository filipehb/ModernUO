using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Cosmere;
using Server.Engines.Cosmere.Events;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereEventTests
{
    public CosmereEventTests()
    {
        CosmereConfig.SetEventsEnabledForTests(true);
        CosmereEventRuntime.ResetForTests();
        CosmereEventCatalog.ClearForTests();
    }

    [Fact]
    public void Validate_RequiresPhaseAndRegionalBounds()
    {
        var def = new CosmereEventDefinition { Id = "x", Phases = [] };
        Assert.False(CosmereEventCatalog.Validate(def, out _));

        def.Phases.Add(new CosmereEventPhaseDef { Id = "main" });
        Assert.True(CosmereEventCatalog.Validate(def, out _));

        def.Scope = CosmereEventScope.Regional;
        def.Map = "Felucca";
        def.Width = 0;
        Assert.False(CosmereEventCatalog.Validate(def, out _));

        def.Width = 10;
        def.Height = 10;
        Assert.True(CosmereEventCatalog.Validate(def, out _));
    }

    [Fact]
    public void TryStart_EnterPhase_ThenEnd()
    {
        var def = CosmereEventCatalog.CreateBlank("test-skirmish");
        def.EndMode = CosmereEventEndMode.Manual;
        CosmereEventCatalog.UpsertForTests(def);

        Assert.True(CosmereEventRuntime.TryStart(def.Id, out var inst, out var error), error);
        Assert.Equal(CosmereEventInstanceState.Running, inst.State);
        Assert.Equal("main", inst.PhaseId);
        Assert.Equal(1, CosmereEventRuntime.CountRunning(def.Id));

        CosmereEventRuntime.End(inst, cancelled: false, "test");
        Assert.Equal(CosmereEventInstanceState.Ended, inst.State);
        Assert.Equal(0, CosmereEventRuntime.CountRunning(def.Id));
    }

    [Fact]
    public void MaxConcurrent_BlocksSecondStart()
    {
        var def = CosmereEventCatalog.CreateBlank("once-only");
        def.MaxConcurrentInstances = 1;
        CosmereEventCatalog.UpsertForTests(def);

        Assert.True(CosmereEventRuntime.TryStart(def.Id, out _, out _));
        Assert.False(CosmereEventRuntime.TryStart(def.Id, out _, out var error));
        Assert.Contains("Max concurrent", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GotoPhase_Advances()
    {
        var def = CosmereEventCatalog.CreateBlank("phased");
        def.Phases.Add(new CosmereEventPhaseDef { Id = "two", Title = "Two" });
        CosmereEventCatalog.UpsertForTests(def);

        Assert.True(CosmereEventRuntime.TryStart(def.Id, out var inst, out _));
        CosmereEventRuntime.GotoPhase(inst, "two");
        Assert.Equal("two", inst.PhaseId);
    }

    [Fact]
    public void SpawnTemp_TracksAndCleansOnEnd()
    {
        var def = CosmereEventCatalog.CreateBlank("spawn-clean");
        def.Scope = CosmereEventScope.Regional;
        def.Map = "Felucca";
        def.X = 100;
        def.Y = 100;
        def.Width = 20;
        def.Height = 20;
        def.Phases[0].OnEnter =
        [
            new CosmereEventActionDef
            {
                Type = "SpawnTemp",
                CreatureType = "DireWolf",
                Count = 2,
                LocationMode = "At",
                Map = "Felucca",
                X = 110,
                Y = 110,
                Z = 0
            }
        ];
        CosmereEventCatalog.UpsertForTests(def);

        Assert.True(CosmereEventRuntime.TryStart(def.Id, out var inst, out var error), error);
        Assert.Equal(2, inst.OwnedSerials.Count);

        var serials = new List<Serial>(inst.OwnedSerials);
        CosmereEventRuntime.End(inst, cancelled: true, "cleanup");

        Assert.Empty(inst.OwnedSerials);
        for (var i = 0; i < serials.Count; i++)
        {
            var mob = World.FindMobile(serials[i]);
            Assert.True(mob == null || mob.Deleted);
        }
    }

    [Fact]
    public void CreatureDeath_StartTrigger_StartsInstance()
    {
        var def = CosmereEventCatalog.CreateBlank("death-start");
        def.StartMode = CosmereEventStartMode.Trigger;
        def.StartTriggers =
        [
            new CosmereEventTriggerDef
            {
                Type = "CreatureDeath",
                CreatureType = "DireWolf"
            }
        ];
        CosmereEventCatalog.UpsertForTests(def);

        var wolf = new DireWolf();
        try
        {
            wolf.MoveToWorld(new Point3D(150, 150, 0), Map.Felucca);
            CosmereEventRuntime.OnCreatureDeath(wolf);
            Assert.Equal(1, CosmereEventRuntime.CountRunning(def.Id));
        }
        finally
        {
            CosmereEventRuntime.TryStopDefinition(def.Id, cancel: true, out _);
            wolf.Delete();
        }
    }

    [Fact]
    public void Persistence_RoundTrip_InstanceFields()
    {
        var writer = new BufferWriter(true);
        writer.Write(7);
        writer.Write("war-of-ash");
        writer.Write((int)CosmereEventInstanceState.Running);
        writer.Write("raid");
        var started = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        writer.Write(started);
        writer.Write(started.AddHours(2));
        writer.Write(started);
        writer.Write((Serial)0x40000010);
        writer.Write(new Point3D(1, 2, 3));
        writer.Write("Felucca");
        writer.Write(1);
        writer.Write((Serial)0x40000011);
        writer.Write(1);
        writer.Write("flag");
        writer.Write("1");

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);
        writer.Close();

        var reader = (IGenericReader)new BufferReader(buffer);
        var inst = new CosmereEventInstance
        {
            InstanceId = reader.ReadInt(),
            DefinitionId = reader.ReadString(),
            State = (CosmereEventInstanceState)reader.ReadInt(),
            PhaseId = reader.ReadString(),
            StartedUtc = reader.ReadDateTime(),
            EndsUtc = reader.ReadDateTime(),
            PhaseEnteredUtc = reader.ReadDateTime(),
            TriggerContextSerial = reader.ReadSerial(),
            TriggerContextLocation = reader.ReadPoint3D(),
            TriggerContextMap = reader.ReadString()
        };
        var owned = reader.ReadInt();
        for (var i = 0; i < owned; i++)
        {
            inst.OwnedSerials.Add(reader.ReadSerial());
        }

        var flags = reader.ReadInt();
        for (var i = 0; i < flags; i++)
        {
            inst.Flags[reader.ReadString()] = reader.ReadString();
        }

        Assert.Equal(7, inst.InstanceId);
        Assert.Equal("war-of-ash", inst.DefinitionId);
        Assert.Equal("raid", inst.PhaseId);
        Assert.Single(inst.OwnedSerials);
        Assert.Equal("1", inst.Flags["flag"]);
    }
}
