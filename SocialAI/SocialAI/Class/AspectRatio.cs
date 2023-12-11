namespace SocialAi
{
    //An aspect ratio holder    
    public class AspectRatio
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public AspectRatio(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}