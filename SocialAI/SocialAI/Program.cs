using Discord;
using Discord.WebSocket;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using Newtonsoft.Json;

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

        //two main actual functions we do.
        //1. backfill last N pages of messages in all monitored channels
        //2. more interestingly, monitor new messages showing up in channels and get images as they come in.
        public async Task MainAsync()
        {
            var settingsPath = "d:\\proj\\social-ai\\social-ai\\settings.json";
            var txt = File.ReadAllText(settingsPath);
            JsonSettings = JsonConvert.DeserializeObject<JsonSettings>(txt);
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
            }

            //other chat comments.
            else if (regot.Content[0] == '*')
            {
                Handler.ProcessMessageAsync(regot);
            }
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