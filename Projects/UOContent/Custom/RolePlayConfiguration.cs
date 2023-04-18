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

        DiscordBotSystemOn = ServerConfiguration.GetOrUpdateSetting("roleplay.discordBotSystemOn", true);
        logger.Information($"discordBotSystemOn: {DiscordBotSystemOn}");
        DiscordBotToken = ServerConfiguration.GetOrUpdateSetting("roleplay.discordBot.token", "");
        logger.Information($"discordBot.token Setado");
        DiscordBotTokenStatusChanelID = ServerConfiguration.GetOrUpdateSetting("roleplay.discordBot.channel.status.id", "");
        logger.Information($"discordBot.channel.status.id Setado");
        DiscordBotChannelControlID = ServerConfiguration.GetOrUpdateSetting("roleplay.discordBot.channel.control.id", "");
        logger.Information($"discordBot.channel.control.id Setado");
    }

    public static string DiscordBotChannelControlID { get; set; }

    public static string DiscordBotTokenStatusChanelID { get; set; }

    public static string DiscordBotToken { get; private set; }

    public static bool DiscordBotSystemOn { get; private set; }

    public static bool DynamicEconomySystemOn { get; private set; }

    public static bool FaintSystemOn { get; private set; }

    public static bool AgeSystemOn { get; private set; }
}
