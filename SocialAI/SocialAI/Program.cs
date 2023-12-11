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

            var settingsPath = "d:\\proj\\social-ai\\social-ai\\settings.json";
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

        /// <summary>
        /// there are 3 main folder outputs.  1) raw from mj.  2) cleaned (this is basically a folder for images which were downloaded already, which I moved out of the main folder cause it gets super slow.  Why is this necessary? because I want the "full downloader" to not redownload them in that case. So it also checks this holding, backup folder, to save you bandwidth and stuff. 3) annotated (with subtext of the prompt added by this program, which is its main function)
        ///     within each one there are two subfolders: m for everything but single images, and s for single images.
        /// </summary>
        /// <param name="settingPath"></param>
        private static void CheckFolderExistence(string settingPath)
        {
            var annotated = new Tuple<string, string>(FileManager.Settings.AnnotatedImageOutputFullPath, nameof(FileManager.Settings.AnnotatedImageOutputFullPath));
            var cleaned = new Tuple<string, string>(FileManager.Settings.CleanedImageOutputFullPath, nameof(FileManager.Settings.CleanedImageOutputFullPath));
            var raw = new Tuple<string, string>(FileManager.Settings.RawMJOutputImagePath, nameof(FileManager.Settings.RawMJOutputImagePath));

            var cover = new List<Tuple<string, string>>() { annotated, cleaned, raw, };

            foreach (var th in cover)
            {
                if (!Directory.Exists(th.Item1)){
                    Console.WriteLine($"Your settings file {settingPath} references a folder for {th.Item2} which doesn't actually exist: \"{th.Item1}\". Create it. Exiting program.");

                    Environment.Exit(1);
                }
                var mdir = System.IO.Path.Combine(th.Item1, "m");
                if (!Directory.Exists(mdir))
                {
                    Console.WriteLine($"Your settings file {settingPath} references a folder for {th.Item2}/mdir for non-single images, which doesn't actually exist: \"{mdir}\". Create it. Exiting program.");

                    Environment.Exit(1);
                }
                var sdir = System.IO.Path.Combine(th.Item1, "s");
                if (!Directory.Exists(sdir))
                {
                    Console.WriteLine($"Your settings file {settingPath} references a folder for {th.Item2}/sdir for non-single images, which doesn't actually exist: \"{sdir}\". Create it. Exiting program.");

                    Environment.Exit(1);
                }
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
            else if (regot.Content.Length == 0)
            {
                var a = 4;
            }

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