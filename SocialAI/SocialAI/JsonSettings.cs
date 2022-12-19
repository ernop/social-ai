namespace SocialAi
{
    public class JsonSettings
    {
        //the base of the project, for git
        public string ProjBase { get; set; } = "";
        public string TokenPath { get; set; } = "";
        public string ImageOutputFullPath { get; set; } = "";
        public string CleanedImageOutputFullPath { get; set; } = "";

        //how far back to go downloading old pages
        public int PageLimit { get; set; } = 20;

        public List<Channel> Channels{ get; set; } = new List<Channel>();
        public List<Channel> DMChannels { get; set; } = new List<Channel>();
    }

    public class Channel 
    {    
        public ulong ChannelId { get; set; }
        public string Name { get; set; }
    }
}