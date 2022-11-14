using Discord;
using Discord.WebSocket;

using System.Threading;
using Newtonsoft.Json;

using static Utils;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();
    private DiscordSocketClient client;
    private FileManager FileManager = new FileManager("d:\\proj\\social-ai\\output\\images");

    public async Task MainAsync()

    {
        client = new DiscordSocketClient();
        client.Log += Log;

        var token = File.ReadAllText("d:\\proj\\social-ai\\social-ai\\SocialAI\\token.txt");

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        //client.MessageUpdated += MessageUpdated;
        client.MessageReceived += MessageReceived;

        client.Ready += () =>
        {
            Console.WriteLine("Bot is connected!");
            return Task.CompletedTask;
        };
        MonitorChannel();

        await Task.Delay(-1);
    }

    public class Settings
    {
        public List<ulong> ChannelIds { get; set; }
    }

    private async void MonitorChannel()
    {
        var json = File.ReadAllText("d:\\proj\\social-ai\\social-ai\\SocialAI\\SocialAI\\settings.json");
        var settings= JsonConvert.DeserializeObject<Settings>(json);
        
        foreach (var channelid in settings.ChannelIds)
        {
            IMessage fromMessage = null;
            Discord.Rest.RestTextChannel channel = null;
            try
            {
                channel = (Discord.Rest.RestTextChannel)client.GetChannelAsync(channelid).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(channelid);
                continue;
            }

            var page = 0;
            while (page < 20)
            {
                page++;
                IAsyncEnumerable<IReadOnlyCollection<IMessage>>? msgs = null;
                if (fromMessage == null)
                {
                    msgs = channel.GetMessagesAsync();
                }
                else
                {
                    msgs = channel.GetMessagesAsync(fromMessage, Direction.Before);
                }

                await foreach (var m in msgs)
                {
                    foreach (var mm in m)
                    {
                        fromMessage = mm;
                        ProcessMessageAsync(mm);
                        await Task.Delay(1);
                    }
                }
                break;
            }
        }
    }

    private async void ProcessMessageAsync(IMessage mm)
    {
        var s = mm.Content;
        if (mm.Attachments.Count > 0)
        {
            foreach (var att in mm.Attachments)
            {
                if (att.Filename.EndsWith("webp"))
                {
                    continue;
                }
                var du = new DiscordUser();
                var p = GetPrompt(s);
                var ui = new ParsedMessage(FileManager, du, p, att.ProxyUrl, att.Filename);
                var res = ui.Save();
            }
        }
    }

    private Task Log(Discord.LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task MessageReceived(SocketMessage message)
    {
        var regot = await message.Channel.GetMessageAsync(message.Id);
        Console.WriteLine($"received {regot}");
        ProcessMessageAsync(regot);
    }
}