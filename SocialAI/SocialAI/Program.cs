using Discord;
using Discord.WebSocket;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();
    private DiscordSocketClient client;

    public async Task MainAsync()

    {
        client = new DiscordSocketClient();
        client.Log += Log;

        var token = File.ReadAllText("d:\\proj\\social-ai\\social-ai\\SocialAI\\token.txt");

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        client.MessageUpdated += MessageUpdated;
        client.Ready += () =>
        {
            Console.WriteLine("Bot is connected!");
            return Task.CompletedTask;
        };
        MonitorChannel();

        await Task.Delay(-1);
    }

    private async void MonitorChannel()
    {
        var ai = client.GetApplicationInfoAsync();
        var channelId = (ulong)1004186219690868756;
        var channel = client.GetChannelAsync(channelId);
        var air = ai.Result;
        var cr = (Discord.Rest.RestTextChannel)channel.Result;
        var msgs = cr.GetMessagesAsync();
        await foreach (var m in msgs)
        {
            Console.WriteLine(m);
            var nn = m.Skip(1).First();
            var s= nn.Content;
        }

    }

    private Task Log(Discord.LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        // If the message was not in the cache, downloading it will result in getting a copy of `after`.
        var message = await before.GetOrDownloadAsync();
        Console.WriteLine($"{message} -> {after}");
    }
}