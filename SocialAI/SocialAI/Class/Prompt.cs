namespace SocialAi
{
    public class Prompt
    {
        //the FULL message text
        public string? FullMessage { get; set; }

        //the human part of the text? hmm not sure
        public string? Message { get; set; }
        public int Version { get; set; }
        
        //null=default
        public AR AR { get; set; }
    }

    public class AR
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}