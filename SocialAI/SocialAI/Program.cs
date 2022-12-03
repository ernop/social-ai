using Discord;
using Discord.WebSocket;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using Newtonsoft.Json;

using static Utils;
using System.ComponentModel;
using System.Threading.Channels;
using System.Runtime;

public class Program
{
    private static int PageLimit { get; set; } = 4;
    public static Task Main(string[] args) => new Program().MainAsync();
    private DiscordSocketClient client;
    private static FileManager FileManager { get; set; }
    private static JsonSettings JsonSettings { get; }= JsonConvert.DeserializeObject<JsonSettings>(File.ReadAllText("d:\\proj\\social-ai\\settings.json"));

    public static void Test()
    {
        var im = Bitmap.FromFile("d:\\dl\\signal-2022-05-08-102334.jpeg");
        var fakeGraphics = Graphics.FromImage(im);
        var fm = new FileManager(JsonSettings);
        var text = "When the expected memetic traction of an idea is conditional on how the idea is formatted, we do not merely bend the outward expression of a new idea into advantageous formatting—as if we think purely first, and only later publish instrumentally and politically. Rather, we begin to pre-format thinking itself, and avoid thoughts that are difficult to format advantageously. We feel that we publish purely and freely, but only because we've installed the instrumental filter at a deeper, almost unconscious level.";
        fm.GetTextInLines(text, 1024, fakeGraphics);
    }

    public async Task MainAsync()

    {
        FileManager = new FileManager(JsonSettings);

        //Test();
        client = new DiscordSocketClient();
        client.Log += Log;

        var token = File.ReadAllText(JsonSettings.TokenPath);
        
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

    private async void MonitorChannel()
    {
        IChannel channel;
        foreach (var channelId in JsonSettings.ChannelIds)
        {
            try
            {
                channel = client.GetChannelAsync(channelId).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                continue;
            }

            var t = channel.GetChannelType();
            switch (t)
            {
                case ChannelType.Text:
                    handleTextChannel((Discord.Rest.RestTextChannel)channel, channelId);
                    break;
                case ChannelType.DM:
                    handleDMChannel((Discord.Rest.RestDMChannel)channel, channelId);
                    break;
                case ChannelType.Voice:
                    break;
                case ChannelType.Group:
                    break;
                case ChannelType.Category:
                    break;
                case ChannelType.News:
                    break;
                case ChannelType.Store:
                    break;
                case ChannelType.NewsThread:
                    break;
                case ChannelType.PublicThread:
                    break;
                case ChannelType.PrivateThread:
                    break;
                case ChannelType.Stage:
                    break;
                case ChannelType.GuildDirectory:
                    break;
                case ChannelType.Forum:
                    break;
                case null:
                    break;
            }

        }
    }

    private async void handleDMChannel(Discord.Rest.RestDMChannel channel, ulong channelId)
    {
        IMessage fromMessage = null;
        var page = 0;
        while (page < PageLimit)
        {
            IAsyncEnumerable<IReadOnlyCollection<IMessage>>? pages;
            if (fromMessage == null)
            {
                pages = channel.GetMessagesAsync();
            }
            else
            {
                pages = channel.GetMessagesAsync(fromMessage, Direction.Before);
            }

            Console.WriteLine($"DmChannelId:{channelId} - page:{page} - {pages.CountAsync()}");

            await foreach (var awaitedPage in pages)
            {
                foreach (var mm in awaitedPage)
                {
                    fromMessage = mm;
                    ProcessMessageAsync(mm);
                    await Task.Delay(1);
                }
            }
            page++;
        }
    }

    private async void handleTextChannel(Discord.Rest.RestTextChannel channel, ulong channelId)
    {
        IMessage fromMessage = null;
        var page = 0;
        while (page < PageLimit)
        {
            IAsyncEnumerable<IReadOnlyCollection<IMessage>>? pages;
            if (fromMessage == null)
            {
                pages = channel.GetMessagesAsync();
            }
            else
            {
                pages = channel.GetMessagesAsync(fromMessage, Direction.Before);
            }

            Console.WriteLine($"TextChannelId:{channelId} - page:{page} - {pages.CountAsync()}");

            await foreach (var awaitedPage in pages)
            {
                foreach (var mm in awaitedPage)
                {
                    fromMessage = mm;
                    ProcessMessageAsync(mm);
                    await Task.Delay(1);
                }
            }
            page++;
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
                ui.Save();
            }
        }
    }

    private Task Log(Discord.LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private static void CleanTen()
    {
        var path = "d:\\proj\\social-ai\\output\\images";
        var cleanPath = "d:\\proj\\social-ai\\output\\images\\clean";
        var oldFileInfos = System.IO.Directory.GetFiles(path).Select(el=>new FileInfo(System.IO.Path.Combine(path,el))).OrderBy(el=>el.CreationTime).Take(10);
        foreach (var el in oldFileInfos)
        {
            var oldPath = System.IO.Path.Combine(path, el.Name);
            var newPath = System.IO.Path.Combine(cleanPath, el.Name);
            if (System.IO.File.Exists(newPath))
            {
                System.IO.File.Delete(oldPath);
            }
            else
            {
                System.IO.File.Move(oldPath, newPath);
            }
        }
    }

    private async Task MessageReceived(SocketMessage message)
    {
        var regot = await message.Channel.GetMessageAsync(message.Id);
        Console.WriteLine($"received {regot.Content}");
        if (regot.Content.ToLower() == "clean")
        {
            CleanTen();
            return;
        }
        if (regot.Content[0] != '*')
        {
            return;
        }
        ProcessMessageAsync(regot);
    }
}