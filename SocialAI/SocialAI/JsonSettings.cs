namespace SocialAi
{
    public class JsonSettings
    {
        //the base of the project, for git
        public string ProjBase { get; set; } = "";
        public string TokenPath { get; set; } = "";
        
        /// <summary>
        /// This is clearly my main output, the image plus my socialAI Annotation
        /// </summary>
        public string AnnotatedImageOutputFullPath { get; set; } = "";
        
        /// <summary>
        /// Some kind of legacy holding folder for files you don't watn to redownload. not sure the right way to handle this from now.
        /// </summary>
        public string CleanedImageOutputFullPath { get; set; } = "";

        /// <summary>
        /// Raw Image directly sent from mj.
        /// </summary>
        public string RawMJOutputImagePath { get; set; } = "";

        //how far back to go downloading old pages
        public int PageLimit { get; set; } = 20;

        //when you set it to actionmethod2, this should be filled in to filter whose prompts to get.  including full discordname like Username#1234
        public string UsernameForPromptDownloading { get; set; } = "";

        public List<ChannelDescriptor> Channels { get; set; } = new List<ChannelDescriptor>();
        public List<ChannelDescriptor> DMChannels { get; set; } = new List<ChannelDescriptor>();
    }

    public class ChannelDescriptor 
    {    
        public ulong ChannelId { get; set; }
        public string Name { get; set; } = "None";

        public override string ToString()
        {
            return "Channel:"+Name;
        }
    }
}