using System.Diagnostics.Tracing;
using System.Reactive.Joins;
using System.Text.RegularExpressions;

namespace SocialAi
{
    public class Prompt
    {
        //the FULL message text
        public string? Content { get; set; }
        
        //harder to parse, but has unredacted username
        public string? CleanContent { get; set; }
        public ulong DiscordMessageID { get; set; }
        public DiscordUser DiscordUser { get; set; }

        //the human part of the text
        public string Message { get; set; }

        //annotater should call this to get text based on parsed version.
        public string GetAnnotation()
        {
            return Message;
        }
        
        public int? Version { get; set; }
        public int? Chaos { get; set; }

        public long? Seed { get; set; } 
        public long? Stylize { get; set; }
        //null=default
        public AR AR { get; set; }

        public string Condense(string m)
        {
            return m.Replace("  ", " ").Trim();
        }

        public Prompt(string content, string cleanContent)
        {
            Content = content;
            //content examples
            //"** girl flips her hair --v 4** - <@331647167112413184> (metered, fast)"
            //"**marine biologist facing the sun, sunset, on the beach --v 4 --ar 2:3 --c 44** - <@331647167112413184> (fast)"

            CleanContent = cleanContent;
            //cleanContent = "marine biologist facing the sun, sunset, on the beach --v 4 --ar 2:3 --c 44 - <Brouhahaha#7120> (fast)"

            //trim leading **
            var remainingFullMessage = Condense(content).Substring(2).Trim();

            var usernameChecker = new Regex(@"<(\S+)#(\d\d\d\d)>").Match(cleanContent);
            if (usernameChecker.Success)
            {
                DiscordUser = new DiscordUser(usernameChecker.Groups[1].Value);
            }

            var parts = remainingFullMessage.Split("**");
            remainingFullMessage = parts[0];
            var meta = parts[1];

            var idChecker = new Regex(@"<@(\d+)>").Match(meta);
            if (idChecker.Success)
            {
                meta = meta.Replace(idChecker.Groups[0].Value, "");
                meta = Condense(meta);
                DiscordMessageID = (ulong)long.Parse(idChecker.Groups[1].Value);
            }

            //cleaning up human part of message
            var versionChecker = new Regex(@"--v (\d)").Match(remainingFullMessage);
            if (versionChecker.Success)
            {
                remainingFullMessage = remainingFullMessage.Replace(versionChecker.Groups[0].Value, "");
                remainingFullMessage = Condense(remainingFullMessage);
                Version = int.Parse(versionChecker.Groups[1].Value);
            }

            var chaosChecker = new Regex(@"--c (\d+)").Match(remainingFullMessage);
            if (chaosChecker.Success)
            {
                remainingFullMessage = remainingFullMessage.Replace(chaosChecker.Groups[0].Value, "");
                remainingFullMessage = Condense(remainingFullMessage);
                Chaos = int.Parse(chaosChecker.Groups[1].Value);
            }

            var arChecker = new Regex(@"--ar (\d):(\d)").Match(remainingFullMessage);
            if (arChecker.Success)
            {
                remainingFullMessage = remainingFullMessage.Replace(arChecker.Groups[0].Value, "");
                remainingFullMessage = Condense(remainingFullMessage);
                var w = int.Parse(arChecker.Groups[1].Value);
                var h = int.Parse(arChecker.Groups[2].Value);
                AR = new AR(w, h);
            }


            var seedChecker = new Regex(@"--seed (\d+)").Match(remainingFullMessage);
            if (seedChecker.Success)
            {
                remainingFullMessage = remainingFullMessage.Replace(seedChecker.Groups[0].Value, "");
                remainingFullMessage = Condense(remainingFullMessage);
                Seed = int.Parse(seedChecker.Groups[1].Value);
            }

            var stylizeChecker = new Regex(@"--s (\d+)").Match(remainingFullMessage);
            if (stylizeChecker.Success)
            {
                remainingFullMessage = remainingFullMessage.Replace(stylizeChecker.Groups[0].Value, "");
                remainingFullMessage = Condense(remainingFullMessage);
                Stylize = int.Parse(stylizeChecker.Groups[1].Value);
            }

            remainingFullMessage = Condense(remainingFullMessage);

            Message = remainingFullMessage;
        }
    }

    public class AR
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public AR(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}