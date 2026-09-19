using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.Cosmere;

public sealed class CosmereKnownNameEntry
{
    public Serial SubjectSerial { get; set; }
    public string Label { get; set; } = "";
    public string Notes { get; set; } = "";

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(SubjectSerial);
        writer.Write(Label);
        writer.Write(Notes);
    }

    public static CosmereKnownNameEntry Deserialize(IGenericReader reader) =>
        new()
        {
            SubjectSerial = reader.ReadSerial(),
            Label = reader.ReadString() ?? "",
            Notes = reader.ReadString() ?? ""
        };
}

public static class CosmereKnownNames
{
    public const int MaxLabelLength = 25;
    public const AccessLevel StaffSeeRealName = AccessLevel.Counselor;

    public static bool IsEnabled => CosmereConfig.KnownNamesEnabled;

    public static string Resolve(Mobile viewer, Mobile subject)
    {
        if (subject == null)
        {
            return "";
        }

        if (!IsEnabled || subject is not PlayerMobile)
        {
            return subject.Name ?? "";
        }

        if (viewer == null)
        {
            return CosmereConfig.UnknownNameDisplay;
        }

        if (viewer.AccessLevel >= StaffSeeRealName)
        {
            return subject.RawName ?? subject.Name ?? "";
        }

        if (viewer == subject)
        {
            return subject.Name ?? "";
        }

        // Disguise / incognito: known-name bond does not apply while NameMod is active.
        if (subject.NameMod != null)
        {
            return subject.Name ?? "";
        }

        if (viewer is PlayerMobile viewerPm &&
            TryGetEntry(viewerPm, subject.Serial, out var entry) &&
            !string.IsNullOrWhiteSpace(entry.Label))
        {
            return entry.Label;
        }

        return CosmereConfig.UnknownNameDisplay;
    }

    public static bool TryGetEntry(PlayerMobile viewer, Serial subjectSerial, out CosmereKnownNameEntry entry)
    {
        entry = null;
        if (viewer?._knownNames == null)
        {
            return false;
        }

        return viewer._knownNames.TryGetValue(subjectSerial, out entry);
    }

    public static void Set(
        PlayerMobile viewer,
        Serial subjectSerial,
        string label,
        string notes
    )
    {
        ArgumentNullException.ThrowIfNull(viewer);

        viewer._knownNames ??= new Dictionary<Serial, CosmereKnownNameEntry>();
        var trimmedLabel = TruncateLabel(label);
        var trimmedNotes = notes?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(trimmedLabel))
        {
            viewer._knownNames.Remove(subjectSerial);
            return;
        }

        if (viewer._knownNames.TryGetValue(subjectSerial, out var existing))
        {
            existing.Label = trimmedLabel;
            existing.Notes = trimmedNotes;
            return;
        }

        viewer._knownNames[subjectSerial] = new CosmereKnownNameEntry
        {
            SubjectSerial = subjectSerial,
            Label = trimmedLabel,
            Notes = trimmedNotes
        };
    }

    public static bool Remove(PlayerMobile viewer, Serial subjectSerial)
    {
        if (viewer?._knownNames == null)
        {
            return false;
        }

        return viewer._knownNames.Remove(subjectSerial);
    }

    public static string TruncateLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return "";
        }

        label = label.Trim().FixHtml();
        if (label.Length <= MaxLabelLength)
        {
            return label;
        }

        return label[..MaxLabelLength];
    }

    public static void Serialize(PlayerMobile pm, IGenericWriter writer)
    {
        var map = pm._knownNames;
        if (map == null || map.Count == 0)
        {
            writer.Write(0);
            return;
        }

        writer.Write(map.Count);
        foreach (var pair in map)
        {
            pair.Value.Serialize(writer);
        }
    }

    public static void Deserialize(PlayerMobile pm, IGenericReader reader)
    {
        var count = reader.ReadInt();
        if (count <= 0)
        {
            pm._knownNames = null;
            return;
        }

        pm._knownNames = new Dictionary<Serial, CosmereKnownNameEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var entry = CosmereKnownNameEntry.Deserialize(reader);
            if (entry.SubjectSerial != Serial.Zero)
            {
                pm._knownNames[entry.SubjectSerial] = entry;
            }
        }
    }
}
