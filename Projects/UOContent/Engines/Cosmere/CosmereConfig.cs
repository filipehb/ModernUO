using System;

namespace Server.Engines.Cosmere;

public static class CosmereConfig
{
    public static bool PermanentDeath { get; private set; }
    public static bool CombatEnabled { get; private set; } = true;
    public static bool AutoGraze { get; private set; } = true;
    public static bool AutoDodge { get; private set; }
    public static int CombatIdleSeconds { get; private set; } = 30;
    public static int NpcPhysicalDefense { get; private set; } = 12;
    public static bool AllomancyEnabled { get; private set; } = true;
    public static bool RequireVials { get; private set; }
    public static int FeetPerTile { get; private set; } = 5;
    public static bool SocialEnabled { get; private set; } = true;
    public static bool NpcAutoResist { get; private set; } = true;
    public static int SocialIdleSeconds { get; private set; } = 120;
    public static bool SocialPvpOptIn { get; private set; } = true;
    public static int DefaultNpcMaxFocus { get; private set; } = 4;
    public static int NpcSpiritualDefense { get; private set; } = 12;
    public static int NpcCognitiveDefense { get; private set; } = 12;
    public static bool AdvancementEnabled { get; private set; } = true;
    public static int MaxLevel { get; private set; } = 20;
    public static int Tier5HealthSoftCap { get; private set; }
    public static bool DebugEnabled { get; private set; } = true;

    public static void Configure()
    {
        PermanentDeath = ServerConfiguration.GetOrUpdateSetting("Cosmere.PermanentDeath", false);
        CombatEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.CombatEnabled", true);
        AutoGraze = ServerConfiguration.GetOrUpdateSetting("Cosmere.AutoGraze", true);
        AutoDodge = ServerConfiguration.GetOrUpdateSetting("Cosmere.AutoDodge", false);
        CombatIdleSeconds = ServerConfiguration.GetOrUpdateSetting("Cosmere.CombatIdleSeconds", 30);
        NpcPhysicalDefense = ServerConfiguration.GetOrUpdateSetting("Cosmere.NpcPhysicalDefense", 12);
        AllomancyEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.AllomancyEnabled", true);
        RequireVials = ServerConfiguration.GetOrUpdateSetting("Cosmere.RequireVials", false);
        FeetPerTile = Math.Max(1, ServerConfiguration.GetOrUpdateSetting("Cosmere.FeetPerTile", 5));
        SocialEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.SocialEnabled", true);
        NpcAutoResist = ServerConfiguration.GetOrUpdateSetting("Cosmere.NpcAutoResist", true);
        SocialIdleSeconds = ServerConfiguration.GetOrUpdateSetting("Cosmere.SocialIdleSeconds", 120);
        SocialPvpOptIn = ServerConfiguration.GetOrUpdateSetting("Cosmere.SocialPvpOptIn", true);
        DefaultNpcMaxFocus = Math.Max(0, ServerConfiguration.GetOrUpdateSetting("Cosmere.DefaultNpcMaxFocus", 4));
        NpcSpiritualDefense = ServerConfiguration.GetOrUpdateSetting("Cosmere.NpcSpiritualDefense", 12);
        NpcCognitiveDefense = ServerConfiguration.GetOrUpdateSetting("Cosmere.NpcCognitiveDefense", 12);
        AdvancementEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.AdvancementEnabled", true);
        MaxLevel = Math.Max(1, ServerConfiguration.GetOrUpdateSetting("Cosmere.MaxLevel", 20));
        Tier5HealthSoftCap = Math.Max(0, ServerConfiguration.GetOrUpdateSetting("Cosmere.Tier5HealthSoftCap", 0));
        DebugEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.DebugEnabled", true);
    }

    /// <summary>Test-only hook to flip the master debug switch without reloading config.</summary>
    internal static void SetDebugEnabledForTests(bool enabled) => DebugEnabled = enabled;
}
