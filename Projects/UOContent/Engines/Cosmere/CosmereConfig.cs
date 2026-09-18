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
    public static bool ProfessionEnabled { get; private set; } = true;
    public static double ProfessionXpMultiplier { get; private set; } = 1.0;
    public static bool LosVisibilityEnabled { get; private set; } = true;
    public static bool HungerEffectsEnabled { get; private set; } = true;
    public static double HungerDecayMinutes { get; private set; } = 5.0;
    public static int HungerDecayAmount { get; private set; } = 1;
    public static bool DayPeriodsEnabled { get; private set; } = true;
    public static bool AnnouncePeriodChange { get; private set; }
    public static int NightLightBonus { get; private set; }
    public static bool LivingWorldEnabled { get; private set; } = true;
    public static bool StaffWakesLivingWorld { get; private set; }
    public static double LivingWorldTickSeconds { get; private set; } = 3.0;
    public static int LivingWorldPlayerRange { get; private set; } = 24;
    public static int LivingWorldHuntRange { get; private set; } = 12;
    public static bool PossessEnabled { get; private set; } = true;
    public static bool RpJournalEnabled { get; private set; } = true;

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
        ProfessionEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.ProfessionEnabled", true);
        ProfessionXpMultiplier = Math.Max(
            0.0,
            ServerConfiguration.GetOrUpdateSetting("Cosmere.ProfessionXpMultiplier", 1.0)
        );
        LosVisibilityEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.LosVisibilityEnabled", true);
        HungerEffectsEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.HungerEffectsEnabled", true);
        HungerDecayMinutes = ServerConfiguration.GetOrUpdateSetting("Cosmere.HungerDecayMinutes", 5.0);
        HungerDecayAmount = ServerConfiguration.GetOrUpdateSetting("Cosmere.HungerDecayAmount", 1);
        DayPeriodsEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.DayPeriodsEnabled", true);
        AnnouncePeriodChange = ServerConfiguration.GetOrUpdateSetting("Cosmere.AnnouncePeriodChange", false);
        NightLightBonus = Math.Max(0, ServerConfiguration.GetOrUpdateSetting("Cosmere.NightLightBonus", 0));
        LivingWorldEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.LivingWorldEnabled", true);
        StaffWakesLivingWorld = ServerConfiguration.GetOrUpdateSetting("Cosmere.StaffWakesLivingWorld", false);
        LivingWorldTickSeconds = ServerConfiguration.GetOrUpdateSetting("Cosmere.LivingWorldTickSeconds", 3.0);
        LivingWorldPlayerRange = Math.Max(0, ServerConfiguration.GetOrUpdateSetting("Cosmere.LivingWorldPlayerRange", 24));
        LivingWorldHuntRange = Math.Max(0, ServerConfiguration.GetOrUpdateSetting("Cosmere.LivingWorldHuntRange", 12));
        PossessEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.PossessEnabled", true);
        RpJournalEnabled = ServerConfiguration.GetOrUpdateSetting("Cosmere.RpJournalEnabled", true);
    }

    /// <summary>Test-only hook to flip the master debug switch without reloading config.</summary>
    internal static void SetDebugEnabledForTests(bool enabled) => DebugEnabled = enabled;

    /// <summary>Test-only hook for profession master switch.</summary>
    internal static void SetProfessionEnabledForTests(bool enabled) => ProfessionEnabled = enabled;

    /// <summary>Test-only hook for profession XP multiplier.</summary>
    internal static void SetProfessionXpMultiplierForTests(double multiplier) =>
        ProfessionXpMultiplier = Math.Max(0.0, multiplier);

    internal static void SetLosVisibilityEnabledForTests(bool enabled) => LosVisibilityEnabled = enabled;

    internal static void SetHungerEffectsEnabledForTests(bool enabled) => HungerEffectsEnabled = enabled;

    internal static void SetHungerDecayForTests(double minutes, int amount)
    {
        HungerDecayMinutes = minutes;
        HungerDecayAmount = amount;
    }

    internal static void SetDayPeriodsEnabledForTests(bool enabled) => DayPeriodsEnabled = enabled;

    internal static void SetLivingWorldEnabledForTests(bool enabled) => LivingWorldEnabled = enabled;

    internal static void SetStaffWakesLivingWorldForTests(bool enabled) => StaffWakesLivingWorld = enabled;

    internal static void SetRpJournalEnabledForTests(bool enabled) => RpJournalEnabled = enabled;
}
