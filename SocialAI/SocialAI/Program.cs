using Discord;
using Discord.WebSocket;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using Newtonsoft.Json;

using static Utils;
using System.Runtime;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();
    private DiscordSocketClient client;
    private FileManager FileManager = new FileManager("d:\\proj\\social-ai\\output\\images");

    public static void Test()
    {
        var im = Bitmap.FromFile("d:\\dl\\signal-2022-05-08-102334.jpeg");
        var fakeGraphics = Graphics.FromImage(im);
        var fm = new FileManager("d:\\proj\\social-ai\\output\\images");
        var text = "When the expected memetic traction of an idea is conditional on how the idea is formatted, we do not merely bend the outward expression of a new idea into advantageous formatting—as if we think purely first, and only later publish instrumentally and politically. Rather, we begin to pre-format thinking itself, and avoid thoughts that are difficult to format advantageously. We feel that we publish purely and freely, but only because we've installed the instrumental filter at a deeper, almost unconscious level.";
        fm.GetTextInLines(text, 1024, fakeGraphics);
    }

    public async Task MainAsync()

    {
        Test();
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

    private async void MonitorChannel()
    {
        var json = File.ReadAllText("d:\\proj\\social-ai\\social-ai\\SocialAI\\SocialAI\\settings.json");
        var settings = JsonConvert.DeserializeObject<Settings>(json);

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
                IAsyncEnumerable<IReadOnlyCollection<IMessage>>? pages = null;
                if (fromMessage == null)
                {
                    pages = channel.GetMessagesAsync();
                }
                else
                {
                    pages = channel.GetMessagesAsync(fromMessage, Direction.Before);
                }

                Console.WriteLine($"channelId:{channelid} - pgae:{page} - {pages.CountAsync()}");

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
                var filename = att.Filename;
                if (filename.Contains("_"))
                {
                    var fp = filename.Split("_", 2);
                    filename = fp[1];
                }
                var createdTimestamp =$"{mm.CreatedAt.Year}y-{mm.CreatedAt.Month:D2}m-{mm.CreatedAt.Day:D2}d-{mm.CreatedAt.Hour:D2}h-{mm.CreatedAt.Minute:D2}m-{mm.CreatedAt.Second:D2}s";
                filename = createdTimestamp + "-" + filename;
                var ui = new ParsedMessage(FileManager, du, p, att.ProxyUrl, filename);
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