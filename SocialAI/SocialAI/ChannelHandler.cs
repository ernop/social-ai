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
    public class ChannelHandler
    {
        private JsonSettings Settings { get; set; }
        private FileManager FileManager { get; set; }
        public ChannelHandler(JsonSettings settings, FileManager fm)
        {
            Settings = settings;
            FileManager = fm;
        }

        public async Task<List<Prompt>> DownloadAllPromptsFromTextChannelAsync(Discord.Rest.RestTextChannel channel, ChannelDescriptor channelConfig, string username)
        {
            IMessage fromMessage = null;
            var page = 0;
            var prompts = new List<Prompt>();
            var gotOne = true;
            while (true)
            {
                if (!gotOne) {
                    break;
                }

                gotOne = false;
                IAsyncEnumerable<IReadOnlyCollection<IMessage>>? pages;
                if (fromMessage == null)
                {
                    pages = channel.GetMessagesAsync();
                }
                else
                {
                    pages = channel.GetMessagesAsync(fromMessage, Direction.Before);
                }

                if (pages == null || pages.CountAsync().Result == 0)
                {
                    break;
                }

                Console.WriteLine($"Channel:{channelConfig.Name} - page:{page} - {pages.CountAsync()}");

                await foreach (var awaitedPage in pages)
                {
                    foreach (var message in awaitedPage)
                    {
                        fromMessage = message;
                        gotOne = true;

                        var prompt = DownloadPrompt(message);
                        if (prompt == null)
                        {
                            continue;
                        }
                        if (prompt.DiscordUser.DiscordUsername != username)
                        {
                            continue;
                        }
                        prompts.Add(prompt);
                    }
                }
                page++;
                Console.WriteLine($"Got {prompts.Count} prompts from this user so far.");
                await Task.Delay(1);
            }

            prompts = prompts.OrderBy(el => el.CreatedAtUtc).ToList();
            return prompts;
        }

        public async void BackfillImagesFromTextChannelAsync(Discord.Rest.RestTextChannel channel, ChannelDescriptor channelConfig)
        {
            IMessage fromMessage = null;
            var page = 0;
            while (page < Settings.PageLimit)
            {
                IAsyncEnumerable<IReadOnlyCollection<IMessage>>? messages;
                if (fromMessage == null)
                {
                    messages = channel.GetMessagesAsync();
                }
                else
                {
                    messages = channel.GetMessagesAsync(fromMessage, Direction.Before);
                }

                Console.WriteLine($"Channel:{channelConfig.Name} - page:{page} - {messages.CountAsync()}");

                await foreach (var awaitedMessage in messages)
                {
                    foreach (var message in awaitedMessage)
                    {
                        //Console.WriteLine($"\tmm{channelConfig}-{message.Content}");
                        fromMessage = message;
                        DownloadImagesFromMessageAsync(message);
                        await Task.Delay(1);
                    }
                }
                page++;
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// Checking a message. Do what we care about and only that- downloading the attachment etc.
        /// </summary>
        public Prompt DownloadPrompt(IMessage discordMessage)
        {

            if (discordMessage.Attachments.Count == 0) { return null; }


            //channel, createdAt
            if (string.IsNullOrEmpty(discordMessage.Content))
            {
                return null;
            }

            if (discordMessage.CleanContent == "Progress images have been disabled. Don't worry, the results will still be sent when it completes.")
            {
                return null;
            }

            if (!discordMessage.Content.StartsWith("**"))
            {
                Console.WriteLine($"Failed parsing: \"{discordMessage.Content}\"");
                return null;
            }


            var prompt = new Prompt(discordMessage.Content, discordMessage);

            //this contans "default" for generations, "reply" for upscales
            //but we currently get that information via the text "Upscaled by" or other text
            //Console.WriteLine(discordMessage.Type);

            //not sure why/when this happens?
            if (prompt.Content == "")
            {
                return null;
            }
            

            return prompt;
        }

        /// <summary>
        /// Checking a message. Do what we care about and only that- downloading the attachment etc.
        /// </summary>
        public async void DownloadImagesFromMessageAsync(IMessage discordMessage)
        {
            if (discordMessage.Attachments.Count > 0)
            {
                var prompt = new Prompt(discordMessage.Content, discordMessage);
                if (prompt.Content == "") { return; }

                //as if there are ever more than 1? observationally there is only 1, ever anyway.
                foreach (var att in discordMessage.Attachments)
                {
                    if (att.Filename.EndsWith("webp"))
                    {
                        continue;
                    }
                    var parsedMessage = new ParsedMessage(FileManager, prompt, att.ProxyUrl, att.Filename);
                    try
                    {
                        await parsedMessage.SaveAndAnnotateImage();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex.Message}");
                        await parsedMessage.SaveAndAnnotateImage();
                    }
                }
            }
        }
    }
}