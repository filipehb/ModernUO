using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Server.Commands;
using Server.Logging;

namespace Server.Custom.DiscordBot;

public class UODiscordBotCommandControl
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(UODiscordBotCommandControl));
    public static DiscordSocketClient Client { get; set; }

    private static readonly ulong _shard_control_channel = Convert.ToUInt64(RolePlayConfiguration.DiscordBotChannelControlID);

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

        StartAsync();
    }

    public static async Task StartAsync()
    {
        DiscordSocketConfig discordSocketConfig = new DiscordSocketConfig();
        discordSocketConfig.GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent;
        Client = new DiscordSocketClient(discordSocketConfig);

        Client.Log += Log;

        var token = RolePlayConfiguration.DiscordBotToken;

        await Client.LoginAsync(TokenType.Bot, token);
        await Client.StartAsync();

        Client.MessageReceived += ClientOnMessageReceived;

        await Task.Delay(-1);
    }

    private static async Task ClientOnMessageReceived(SocketMessage socketMessage)
    {
        await Task.Run(() =>
        {
            //Activity is not from a Bot.
            if (!socketMessage.Author.IsBot)
            {
                var username = socketMessage.Author.Username;
                var usernameId = socketMessage.Author.Id;
                var channelId  = socketMessage.Channel.Id;
                if (channelId == _shard_control_channel)
                {
                    var message = socketMessage.Content;

                    // verifique se a mensagem começa com o prefixo "!"
                    if (message.StartsWith("!"))
                    {
                        var command = message.Substring(1).Split(' ')[0];

                        CommandLogging.WriteLine(
                            username,
                            $"{username} com id do Discord {usernameId} executou o comando {command} no canal {channelId} via Discord"
                        );

                        switch (command.ToLower())
                        {
                            case "saveworld":
                                {
                                    Log(new LogMessage(LogSeverity.Info, $"Channel", $"Comando de {command} enviado via Discord pelo usuário {username}"));
                                    World.Save();
                                }
                                break;
                            case "restart":
                                {
                                    ulong _shard_status_channel = Convert.ToUInt64(RolePlayConfiguration.DiscordBotTokenStatusChanelID);
                                    UODiscordBotUtils.SendMessageAsync(_shard_status_channel, ":turtle: Servidor sendo restartado :turtle:", true).Wait();

                                    Log(new LogMessage(LogSeverity.Info, $"Channel", $"Comando de {command} enviado via Discord pelo usuário {username}"));
                                    Core.Kill(true);
                                }
                                break;
                            case "shutdown":
                                {
                                    ulong _shard_status_channel = Convert.ToUInt64(RolePlayConfiguration.DiscordBotTokenStatusChanelID);
                                    UODiscordBotUtils.SendMessageAsync(_shard_status_channel, ":turtle: Servidor OFF :turtle:", true).Wait();

                                    Log(new LogMessage(LogSeverity.Info, $"Channel", $"Comando de {command} enviado via Discord pelo usuário {username}"));
                                    Core.Kill();
                                }
                                break;
                        }
                    }
                }
            }
        });
    }

    private static Task Log(LogMessage msg)
    {
        logger.Information($"Log Discord: {msg.ToString()}");
        return Task.CompletedTask;
    }
}
