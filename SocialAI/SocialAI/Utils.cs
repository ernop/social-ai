
namespace SocialAi
{

    public static class Utils
    {
        public static int count { get; set; } = 0;
        public static Prompt GetPrompt(string rawPrompt)
        {
            //"** girl flips her hair --v 4** - <@331647167112413184> (metered, fast)"
            var p = new Prompt();
            rawPrompt = rawPrompt.Substring(2);

            p.Version = 3;
            if (rawPrompt.Contains("--v 4"))
            {
                p.Version = 4;
            }
            string psp;
            if (rawPrompt.IndexOf("** - ") <= 0)
            {
                psp = rawPrompt;
            }
            else
            {
                psp = rawPrompt.Substring(0, rawPrompt.IndexOf("** - ")).Trim().Split("--v")[0].Trim();
            }
            count += 1;

            p.Message = psp;
            return p;
        }
    }

}