using Discord;
using Discord.WebSocket;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using Newtonsoft.Json;

using static SocialAi.Utils;
using System.ComponentModel;
using System.Threading.Channels;
using System.Runtime;


namespace SocialAi
{
    public class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();
        private DiscordSocketClient client = null;
        private static FileManager FileManager { get; set; } = new FileManager();
        private static JsonSettings JsonSettings { get; set; }

        private static Handler Handler { get; set; }

        public static void Test()
        {
            FileManager.Init(JsonSettings);
            var text = "When the expected memetic traction of an idea is conditional on how the idea is formatted, we do not merely bend the outward expression of a new idea into advantageous formatting—as if we think purely first, and only later publish instrumentally and politically. Rather, we begin to pre-format thinking itself, and avoid thoughts that are difficult to format advantageously. We feel that we publish purely and freely, but only because we've installed the instrumental filter at a deeper, almost unconscious level.";
            FileManager.GetTextInLines(text, 1024);
        }

        //two main actual functions we do.
        //1. backfill last N pages of messages in all monitored channels
        //2. more interestingly, monitor new messages showing up in channels and get images as they come in.
        public async Task MainAsync()
        {
            var settingsPath = "c:\\git\\social-ai\\settings.json";
            JsonSettings = JsonConvert.DeserializeObject<JsonSettings>(File.ReadAllText(settingsPath));
            if (JsonSettings == null)
            {
                throw new Exception($"No settings at {settingsPath}");
            }
            FileManager.Init(JsonSettings);

            client = new DiscordSocketClient();
            client.Log += Log;

            var token = File.ReadAllText(JsonSettings.TokenPath);

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            client.MessageReceived += MessageReceived;

            client.Ready += () =>
            {
                Console.WriteLine("Bot is connected!");
                return Task.CompletedTask;
            };
            
            Handler = new Handler(JsonSettings, FileManager);
            var monitor = new ChannelMonitor(JsonSettings, Handler);
            monitor.MonitorChannel(client);

            await Task.Delay(-1);
        }

        private Task Log(Discord.LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage message)
        {
            var regot = await message.Channel.GetMessageAsync(message.Id);
            Console.WriteLine($"received {regot.Content}");
            
            //primitive command method.
            if (regot.Content.ToLower() == "clean")
            {
                CleanTen();
                return;
            }

            //other chat comments.
            if (regot.Content[0] != '*')
            {
                return;
            }

            //actually download the image.
            Handler.ProcessMessageAsync(regot);
        }

        //for party use - clean out older items in the share folder (so if you're running a slideshow, it'll hit new ones preferentially)
        private static void CleanTen()
        {
            var path = JsonSettings.ImageOutputFullPath;
            var cleanPath = JsonSettings.CleanedImageOutputFullPath;
            var oldFileInfos = System.IO.Directory.GetFiles(path).Select(el => new FileInfo(System.IO.Path.Combine(path, el))).OrderBy(el => el.CreationTime).Take(10);
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
    }
}