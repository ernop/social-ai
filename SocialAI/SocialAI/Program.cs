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

        private static ChannelHandler ChannelHandler { get; set; }

        //two main actual functions we do.
        //1. backfill last N pages of messages in all monitored channels
        //2. more interestingly, monitor new messages showing up in channels and get images as they come in.
        public async Task MainAsync()
        {
            //modify this to control.
            var am = ActionMethod.SavePrompts;
            am = ActionMethod.BackfillAndMonitor;
            Console.WriteLine($"Operating in mode: {am}");

            var settingsPath = "c:\\proj\\social-ai\\settings.json";
            if (!File.Exists(settingsPath))
            {
                Console.WriteLine($"Base settings file doesn't exist ({settingsPath}). You probably want to copy SampleSettings.json to this path (or fix the C# code above to point at where your file is) and also fill in the values in the file with the channel ids, folders etc for things to work.");
                Environment.Exit(3);
            }

            var txt = File.ReadAllText(settingsPath);
            JsonSettings = JsonConvert.DeserializeObject<JsonSettings>(txt);
            if (JsonSettings == null)
            {
                throw new Exception($"No settings at {settingsPath}");
            }
            FileManager.Init(JsonSettings);

            CheckFolderExistence(settingsPath);

            client = new DiscordSocketClient();
            client.Log += Log;

            var token = File.ReadAllText(JsonSettings.TokenPath);

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            IChannel channel;
            ChannelHandler = new ChannelHandler(JsonSettings, FileManager);

            foreach (var channelConfig in JsonSettings.Channels)
            {
                try
                {
                    channel = client.GetChannelAsync(channelConfig.ChannelId).Result;
                }
                catch (Exception ex)
                {
                    //this hits sometimes, unclear reasons likely due to bad coercion
                    Console.WriteLine("Failure to get channel.");
                    Console.WriteLine(ex);
                    continue;
                }

                var channelType = channel.GetChannelType();
                if (channelType != ChannelType.Text)
                {
                    Console.WriteLine($"Can only handle Text ChannelType right now. {channelType}, {channel}");
                }
                switch (am)
                {
                    case ActionMethod.BackfillAndMonitor:

                        //setup monitoring
                        client.MessageReceived += MessageReceived;

                        client.Ready += () =>
                        {
                            Console.WriteLine("Bot is connected!");
                            return Task.CompletedTask;
                        };

                        //backfill

                        ChannelHandler.BackfillImagesFromTextChannelAsync((Discord.Rest.RestTextChannel)channel, channelConfig);
                        Console.WriteLine($"BackfillImagesFromTextChannelAsync: {channelConfig.Name}");

                        //now, also wait forever monitoring the channel.
                        await Task.Delay(-1);

                        break;
                    case ActionMethod.SavePrompts:
                        if (string.IsNullOrEmpty(JsonSettings.UsernameForPromptDownloading))
                        {
                            throw new Exception("Configuration problem - no UsernameForPromptDownloading found in settings");
                        }
                        try
                        {
                            //this is very annoying. you can't get into some types of channels. TODO figure this out.                            
                            var convertedChannel = (Discord.Rest.RestTextChannel)channel;
                            var prompts = ChannelHandler.DownloadAllPromptsFromTextChannelAsync(convertedChannel, channelConfig, JsonSettings.UsernameForPromptDownloading);
                            SavePrompts(prompts, JsonSettings.UsernameForPromptDownloading, channelConfig.Name);
                        }
                        catch (Exception ex)
                        {
                            //Cannot get this, probably cause it's not set up with the right permissions?
                            //var convertedChannlB = (Discord.WebSocket.SocketTextChannel)channel;
                            //var prompts = ChannelHandler.DownloadAllPromptsFromTextChannelAsync(null, channelConfig, JsonSettings.UsernameForPromptDownloading);
                            //SavePrompts(prompts, JsonSettings.UsernameForPromptDownloading, channelConfig.Name);
                            Console.WriteLine($"Error on channel: {channelConfig.Name}");
                            return;
                            //throw;
                        }

                        Console.WriteLine($"DownloadAllPromptsFromTextChannelAsync: {channelConfig.Name}");
                        break;
                }
            }
        }


        private static void CheckFolderExistence(string settingPath)
        {
            if (!Directory.Exists(FileManager.Settings.AnnotatedImageOutputFullPath))
            {
                Console.WriteLine($"Your settings file {settingPath} references a folder for AnnotatedImageOutputFullPath which doesn't actually exist: {FileManager.Settings.AnnotatedImageOutputFullPath}. Create it. Exiting program.");
                Environment.Exit(1);
            }
            if (!Directory.Exists(FileManager.Settings.CleanedImageOutputFullPath))
            {
                Console.WriteLine($"Your settings file {settingPath} references a folder for CleanedImageOutputFullPath which doesn't actually exist: {FileManager.Settings.CleanedImageOutputFullPath}. Create it. Exiting program.");
                Environment.Exit(1);
            }
            if (!Directory.Exists(FileManager.Settings.OrigImageOutputFullPath))
            {
                Console.WriteLine($"Your settings file {settingPath} references a folder for AnnotatedImageOutputFullPath which doesn't actually exist: {FileManager.Settings.OrigImageOutputFullPath}. Create it and retry. Exiting program.");
                Environment.Exit(1);
            }
        }

        private void SavePrompts(Task<List<Prompt>> prompts, string username, string rawChannelName)
        {
            var safeChannelName = Path.GetFileName(rawChannelName);
            var targetPromptFilename = $"{JsonSettings.AnnotatedImageOutputFullPath}/{username.Split('#')[0]}_{safeChannelName}_prompts.txt";
            using (StreamWriter writer = new StreamWriter(targetPromptFilename))
            {
                writer.WriteLine("Message\tGenerationType\tVersion\tChaos\tAR\tseed\tstylize\tniji\tCreatedAtUtc\tCreatedChannelName\trefsCSV");
                foreach (var prompt in prompts.Result)
                {
                    var parts = new List<string>();
                    parts.Add(prompt.Message);
                    parts.Add(prompt.GenerationType.ToString());
                    parts.Add(prompt.Version.ToString());
                    parts.Add(prompt.Chaos.ToString());

                    var theAr = "";
                    if (prompt.AR != null)
                    {
                        theAr = $"{prompt.AR.Width}:{prompt.AR.Height}";
                    }
                    parts.Add(theAr);

                    parts.Add(prompt.Seed?.ToString() ?? "");
                    parts.Add(prompt.Stylize?.ToString() ?? "");
                    parts.Add(prompt.Niji?.ToString() ?? "");

                    parts.Add(prompt.CreatedAtUtc.ToString());
                    parts.Add(prompt.CreatedChannelName);

                    var refs = "";
                    if (prompt.ReferencedImages?.Count > 0)
                    {
                        refs = string.Join(',', prompt.ReferencedImages);
                    }
                    parts.Add(refs);


                    var joined = string.Join("\t", parts);
                    writer.WriteLine(joined);
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
            //primitive command method.
            if (regot.Content.ToLower() == "clean")
            {
                CleanTen();
            }

            //other chat comments.
            else if (regot.Content[0] == '*')
            {
                ChannelHandler.DownloadImagesFromMessageAsync(regot);
            }
        }

        //for party use - clean out older items in the share folder (so if you're running a slideshow, it'll hit new ones preferentially)
        private static void CleanTen()
        {
            var path = JsonSettings.AnnotatedImageOutputFullPath;
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