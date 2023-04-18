using System;
using Discord.WebSocket;
using Server.Logging;
using Server.Network;

namespace Server.Custom.DiscordBot;

public class UODiscordBotEvents
{
    private static readonly ulong _shard_status_channel = Convert.ToUInt64(RolePlayConfiguration.DiscordBotTokenStatusChanelID);

    public static void Initialize()
    {
        if (RolePlayConfiguration.DiscordBotSystemOn.Equals(false))
        {
            Utility.PushColor(ConsoleColor.Red);
            Console.WriteLine("DiscordBot Disabled");
            Utility.PopColor();

            return;
        }

        if (string.IsNullOrEmpty(RolePlayConfiguration.DiscordBotToken))
        {
            Utility.PushColor(ConsoleColor.Red);
            Console.WriteLine("Discord: Invalid token!");
            Utility.PopColor();

            return;
        }

        // Eventos
        EventSink.Logout += OnLogout;
        EventSink.Login += OnLogin;
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted()
    {
        UODiscordBotUtils.SendMessageAsync(_shard_status_channel, ":fire: Servidor ligado :fire:");
    }

    private static void OnLogin(Mobile obj)
    {
        if (obj.AccessLevel <= AccessLevel.Player)
        {
            var states = TcpServer.Instances;
            UODiscordBotUtils.SendMessageAsync(_shard_status_channel, $"Temos um total de {states.Count} player(s) online.");
        }
    }

    private static void OnLogout(Mobile obj)
    {
        if (obj.AccessLevel <= AccessLevel.Player)
        {
            var states = TcpServer.Instances;
            UODiscordBotUtils.SendMessageAsync(_shard_status_channel, $"Temos um total de {states.Count} player(s) online.");
        }
    }
}
