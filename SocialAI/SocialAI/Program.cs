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
            var im = Bitmap.FromFile("d:\\dl\\signal-2022-05-08-102334.jpeg");
            var fakeGraphics = Graphics.FromImage(im);
            FileManager.Init(JsonSettings);
            var text = "When the expected memetic traction of an idea is conditional on how the idea is formatted, we do not merely bend the outward expression of a new idea into advantageous formatting—as if we think purely first, and only later publish instrumentally and politically. Rather, we begin to pre-format thinking itself, and avoid thoughts that are difficult to format advantageously. We feel that we publish purely and freely, but only because we've installed the instrumental filter at a deeper, almost unconscious level.";
            FileManager.GetTextInLines(text, 1024, fakeGraphics);
        }

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


            MonitorChannel();

            await Task.Delay(-1);
        }

        private void MonitorChannel()
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
                        Handler.HandleTextChannelAsync((Discord.Rest.RestTextChannel)channel, channelId);
                        break;
                    case ChannelType.DM:
                        Handler.HandleDMChannelAsync((Discord.Rest.RestDMChannel)channel, channelId);
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



        private Task Log(Discord.LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
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
            Handler.ProcessMessageAsync(regot);
        }

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