namespace SocialAi
{
    public class JsonSettings
    {
        public string TokenPath { get; set; } = "";
        public string ImageOutputFullPath { get; set; } = "";
        public string CleanedImageOutputFullPath { get; set; } = "";

        //how far back to go downloading old pages
        public int PageLimit { get; set; } = 20;

        public List<ulong> ChannelIds { get; set; } = new List<ulong>();
        public List<ulong> DMIds { get; set; } = new List<ulong>();
    }
}