using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Server.Logging;

namespace Server.Custom.DiscordBot;

public class UODiscordBotUtils
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(UODiscordBotCommandControl));
    public static DiscordSocketClient Client { get; set; }

    public static async Task SendMessageAsync(ulong _channelId, string message, bool auto_stop=false)
    {
        Client = new DiscordSocketClient();

        Client.Log += Log;

        var token = RolePlayConfiguration.DiscordBotToken;

        await Client.LoginAsync(TokenType.Bot, token);
        await Client.StartAsync();

        var channel = await Client.GetChannelAsync(_channelId, RequestOptions.Default);
        if (channel is IMessageChannel messageChannel)
        {
            await messageChannel.SendMessageAsync(message);
        }
        else
        {
            Log(new LogMessage(LogSeverity.Error, "Channel", $"Canal {_channelId} não encontrado"));
        }

        // Aguarda a finalização do programa
        if (auto_stop)
        {
            await Task.Delay(3000);
        }
        else
        {
            await Task.Delay(-1);
        }
    }

    private static Task Log(LogMessage msg)
    {
        logger.Information($"Log Discord: {msg.ToString()}");
        return Task.CompletedTask;
    }
}
