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
    public  class Handler
    {
        private JsonSettings Settings { get; set; }
        private FileManager FileManager { get; set; }
        public Handler(JsonSettings settings, FileManager fm)
        {
            Settings = settings;
            FileManager = fm;
        }

        public async void HandleDMChannelAsync(Discord.Rest.RestDMChannel channel, Channel channelConfig)
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

        public async void HandleTextChannelAsync(Discord.Rest.RestTextChannel channel, Channel channelConfig)
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

                Console.WriteLine($"TextChannelId: {channelConfig.Name}:{channelConfig.ChannelId} - page:{page} - {pages.CountAsync()}");

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

        public async void ProcessMessageAsync(IMessage mm)
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
    }
}