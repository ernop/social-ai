namespace SocialAi
{
    /// <summary>
    /// The type of interaction (cmdline generation from discord) the user did, like upscale, beta upscale, initial generation etc.
    /// This is incomplete and I'm only generating it observationally.
    /// </summary>
    public enum GenerationTypeEnum
    {
        NormalGeneration = 1,
        Upscale = 2,
        UpscaleLight = 3,
        UpscaleBeta = 4,
        Remix = 5,
        UpscaleAnime = 6,
        UpscaleMax = 7,
        Variations = 8,
    }
}
