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
    public class ChannelMonitor
    {
        public JsonSettings Settings { get; set; }
        public Handler Handler { get; set; }

        public ChannelMonitor(JsonSettings settings, Handler handler)
        {
            Settings = settings;
            Handler = handler;
        }

        public void MonitorChannel(DiscordSocketClient client)
        {
            IChannel channel;
            foreach (var channelConfig in Settings.Channels)
            {
                try
                {
                    channel = client.GetChannelAsync(channelConfig.ChannelId).Result;
                }
                catch (Exception ex)
                {
                    //this hits sometimes, unclear reasons likely due to bad coercion
                    Console.WriteLine(ex);
                    continue;
                }

                var channelType = channel.GetChannelType();
                switch (channelType)
                {
                    case ChannelType.Text:
                        Handler.HandleTextChannelAsync((Discord.Rest.RestTextChannel)channel, channelConfig);
                        Console.WriteLine($"Monitoring TextChannel: {channelConfig.Name}");
                        break;
                    case ChannelType.DM:
                        Handler.HandleDMChannelAsync((Discord.Rest.RestDMChannel)channel, channelConfig);
                        Console.WriteLine($"Monitoring DMChannel: {channelConfig.Name}");
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
    }
}