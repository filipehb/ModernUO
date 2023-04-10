using Server.Logging;

namespace Server.Custom;

public class RolePlayConfiguration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(RolePlayConfiguration));

    public static void Configure()
    {
        logger.Information("Carregando configurações da classe RolePlayConfiguration");

        AgeSystemOn = ServerConfiguration.GetOrUpdateSetting("roleplay.ageSystemOn", true);
        logger.Information($"ageSystemOn: {AgeSystemOn}");

        FaintSystemOn = ServerConfiguration.GetOrUpdateSetting("roleplay.faintSystemOn", true);
        logger.Information($"faintSystemOn: {FaintSystemOn}");

        DynamicEconomySystemOn = ServerConfiguration.GetOrUpdateSetting("roleplay.dynamicEconomySystemOn", true);
        logger.Information($"dynamicEconomySystemOn: {DynamicEconomySystemOn}");
    }

    public static bool DynamicEconomySystemOn { get; private set; }

    public static bool FaintSystemOn { get; private set; }

    public static bool AgeSystemOn { get; private set; }
}
