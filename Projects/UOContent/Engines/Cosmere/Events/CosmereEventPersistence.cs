using System;
using System.Collections.Generic;

namespace Server.Engines.Cosmere.Events;

public class CosmereEventPersistence : GenericPersistence
{
    private static CosmereEventPersistence _instance;

    public static void Configure() => _instance ??= new CosmereEventPersistence();

    private CosmereEventPersistence() : base("CosmereEvents", 10)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        CosmereEventRuntime.CaptureState(out var nextId, out var instances, out var worldFlags);

        writer.Write(1); // version
        writer.Write(nextId);
        writer.Write(instances.Count);
        for (var i = 0; i < instances.Count; i++)
        {
            WriteInstance(writer, instances[i]);
        }

        writer.Write(worldFlags.Count);
        foreach (var pair in worldFlags)
        {
            writer.Write(pair.Key);
            writer.Write(pair.Value);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadInt();
        var nextId = reader.ReadInt();
        var count = reader.ReadInt();
        var instances = new List<CosmereEventInstance>(count);
        for (var i = 0; i < count; i++)
        {
            instances.Add(ReadInstance(reader, version));
        }

        var flagCount = reader.ReadInt();
        var worldFlags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < flagCount; i++)
        {
            worldFlags[reader.ReadString()] = reader.ReadString();
        }

        CosmereEventRuntime.LoadState(nextId, instances, worldFlags);
    }

    private static void WriteInstance(IGenericWriter writer, CosmereEventInstance inst)
    {
        writer.Write(inst.InstanceId);
        writer.Write(inst.DefinitionId);
        writer.Write((int)inst.State);
        writer.Write(inst.PhaseId);
        writer.Write(inst.StartedUtc);
        writer.Write(inst.EndsUtc);
        writer.Write(inst.PhaseEnteredUtc);
        writer.Write(inst.TriggerContextSerial);
        writer.Write(inst.TriggerContextLocation);
        writer.Write(inst.TriggerContextMap ?? "");

        writer.Write(inst.OwnedSerials.Count);
        for (var i = 0; i < inst.OwnedSerials.Count; i++)
        {
            writer.Write(inst.OwnedSerials[i]);
        }

        writer.Write(inst.Flags.Count);
        foreach (var pair in inst.Flags)
        {
            writer.Write(pair.Key);
            writer.Write(pair.Value);
        }
    }

    private static CosmereEventInstance ReadInstance(IGenericReader reader, int version)
    {
        _ = version;
        var inst = new CosmereEventInstance
        {
            InstanceId = reader.ReadInt(),
            DefinitionId = reader.ReadString() ?? "",
            State = (CosmereEventInstanceState)reader.ReadInt(),
            PhaseId = reader.ReadString() ?? "",
            StartedUtc = reader.ReadDateTime(),
            EndsUtc = reader.ReadDateTime(),
            PhaseEnteredUtc = reader.ReadDateTime(),
            TriggerContextSerial = reader.ReadSerial(),
            TriggerContextLocation = reader.ReadPoint3D(),
            TriggerContextMap = reader.ReadString()
        };

        var ownedCount = reader.ReadInt();
        for (var i = 0; i < ownedCount; i++)
        {
            inst.OwnedSerials.Add(reader.ReadSerial());
        }

        var flagCount = reader.ReadInt();
        for (var i = 0; i < flagCount; i++)
        {
            inst.Flags[reader.ReadString()] = reader.ReadString();
        }

        return inst;
    }
}
