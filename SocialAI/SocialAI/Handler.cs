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
    public class Handler
    {
        private JsonSettings Settings { get; set; }
        private FileManager FileManager { get; set; }
        public Handler(JsonSettings settings, FileManager fm)
        {
            Settings = settings;
            FileManager = fm;
        }

        public async void HandleDMChannelAsync(Discord.Rest.RestDMChannel channel, ChannelDescriptor channelConfig)
        {
            IMessage fromMessage = null;
            var page = 0;
            while (page < Settings.PageLimit)
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

                Console.WriteLine($"DMChannel: {channelConfig.Name}:{channelConfig.ChannelId} - page:{page} - {pages.CountAsync()}");

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

        public async void HandleTextChannelAsync(Discord.Rest.RestTextChannel channel, ChannelDescriptor channelConfig)
        {
            IMessage fromMessage = null;
            var page = 0;
            while (page < Settings.PageLimit)
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

                Console.WriteLine($"Channel:{channelConfig.Name} - page:{page} - {pages.CountAsync()}");

                await foreach (var awaitedPage in pages)
                {
                    foreach (var mm in awaitedPage)
                    {
                        Console.WriteLine($"\tmm{channelConfig}-{mm.Content}");
                        fromMessage = mm;
                        ProcessMessageAsync(mm);
                        await Task.Delay(1);
                    }
                }
                page++;
                await Task.Delay(1);
            }
        }

        public async void ProcessMessageAsync(IMessage mm)
        {
            if (mm.Attachments.Count > 0)
            {
                foreach (var att in mm.Attachments)
                {
                    if (att.Filename.EndsWith("webp"))
                    {
                        continue;
                    }
                    var prompt = new Prompt(mm.Content, mm.CleanContent);
                    if (prompt.Content == "") { return; }
                    var parsedMessage = new ParsedMessage(FileManager, prompt, att.ProxyUrl, att.Filename);
                    try
                    {
                        await parsedMessage.SaveAndAnnotate();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex.Message}");
                    }
                }
            }
        }
    }
}