
public static class Utils
{
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
        var psp = "";
        if (rawPrompt.IndexOf("** - ") <= 0)
        {
            psp = rawPrompt;
        }
        else
        {
            psp = rawPrompt.Substring(0, rawPrompt.IndexOf("** - ")).Trim().Split("--")[0].Trim();
        }
        p.Message = psp;
        return p;
    }
}
