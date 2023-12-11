namespace SocialAi
{
    /// <summary>
    /// The type of interaction (cmdline generation from discord) the user did, like upscale, beta upscale, initial generation etc.
    /// This is incomplete and I'm only generating it observationally.
    /// </summary>
    public enum GenerationTypeEnum
    {
        NormalImageCommandOutputMosaic = 1, //this generates the mosaic
        Upscale = 2,
        UpscaleLight = 3,
        UpscaleBeta = 4,
        Remix = 5,
        UpscaleAnime = 6,
        UpscaleMax = 7,
        Variations = 8,
        UpscaleSingle = 9, //this is when you click one of the 4 to "upscale"? it, i.e., "make" a single image. The very common action.
    }
}
