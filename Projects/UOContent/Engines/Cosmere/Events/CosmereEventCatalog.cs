using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.Engines.Cosmere.Events;

public static class CosmereEventCatalog
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(CosmereEventCatalog));
    private static readonly Dictionary<string, CosmereEventDefinition> _definitions =
        new(StringComparer.OrdinalIgnoreCase);

    public static string DirectoryPath =>
        Path.Combine(Core.BaseDirectory, "Configuration", "Cosmere", "Events");

    public static IReadOnlyDictionary<string, CosmereEventDefinition> Definitions => _definitions;

    public static void Configure()
    {
        Directory.CreateDirectory(DirectoryPath);
        Reload();
    }

    public static void Reload()
    {
        _definitions.Clear();
        Directory.CreateDirectory(DirectoryPath);

        foreach (var file in Directory.EnumerateFiles(DirectoryPath, "*.json"))
        {
            try
            {
                var def = JsonConfig.Deserialize<CosmereEventDefinition>(file);
                if (def == null || string.IsNullOrWhiteSpace(def.Id))
                {
                    _logger.Warning("Skipping invalid Cosmere event file {File}", file);
                    continue;
                }

                if (!Validate(def, out var error))
                {
                    _logger.Warning("Skipping Cosmere event {Id}: {Error}", def.Id, error);
                    continue;
                }

                _definitions[def.Id] = def;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load Cosmere event file {File}", file);
            }
        }

        _logger.Information("Loaded {Count} Cosmere event definition(s)", _definitions.Count);
    }

    internal static void ClearForTests() => _definitions.Clear();

    internal static void UpsertForTests(CosmereEventDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
        {
            return;
        }

        _definitions[definition.Id] = definition;
    }

    public static bool TryGet(string id, out CosmereEventDefinition definition) =>
        _definitions.TryGetValue(id ?? "", out definition);

    public static bool Save(CosmereEventDefinition definition, out string error)
    {
        if (!Validate(definition, out error))
        {
            return false;
        }

        Directory.CreateDirectory(DirectoryPath);
        var path = Path.Combine(DirectoryPath, $"{SanitizeFileName(definition.Id)}.json");
        JsonConfig.Serialize(path, definition);
        _definitions[definition.Id] = definition;
        error = null;
        return true;
    }

    public static bool Validate(CosmereEventDefinition def, out string error)
    {
        if (def == null || string.IsNullOrWhiteSpace(def.Id))
        {
            error = "Id is required.";
            return false;
        }

        if (def.Phases == null || def.Phases.Count == 0)
        {
            error = "At least one phase is required.";
            return false;
        }

        for (var i = 0; i < def.Phases.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(def.Phases[i].Id))
            {
                error = $"Phase {i} is missing Id.";
                return false;
            }
        }

        if (def.Scope == CosmereEventScope.Regional &&
            (def.Width <= 0 || def.Height <= 0 || string.IsNullOrWhiteSpace(def.Map)))
        {
            error = "Regional events require Map and positive Width/Height.";
            return false;
        }

        if (def.MaxConcurrentInstances < 1)
        {
            def.MaxConcurrentInstances = 1;
        }

        error = null;
        return true;
    }

    public static CosmereEventDefinition CreateBlank(string id) =>
        new()
        {
            Id = id?.Trim() ?? "",
            Title = id?.Trim() ?? "New Event",
            Summary = "",
            Enabled = true,
            StartMode = CosmereEventStartMode.Manual,
            EndMode = CosmereEventEndMode.Manual,
            MaxConcurrentInstances = 1,
            Phases =
            [
                new CosmereEventPhaseDef
                {
                    Id = "main",
                    Title = "Main",
                    OnEnter =
                    [
                        new CosmereEventActionDef
                        {
                            Type = "Announce",
                            Message = "Something stirs in the world…",
                            Audience = "All"
                        }
                    ]
                }
            ]
        };

    public static Map ResolveMap(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return Map.Parse(name);
    }

    public static bool Contains(CosmereEventDefinition def, Point3D loc, Map map)
    {
        if (def.Scope != CosmereEventScope.Regional)
        {
            return true;
        }

        var defMap = ResolveMap(def.Map);
        if (defMap == null || map != defMap)
        {
            return false;
        }

        return loc.X >= def.X && loc.X < def.X + def.Width &&
               loc.Y >= def.Y && loc.Y < def.Y + def.Height;
    }

    private static string SanitizeFileName(string id)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            id = id.Replace(c, '_');
        }

        return id;
    }
}
