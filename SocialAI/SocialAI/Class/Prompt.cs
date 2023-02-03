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

        //linked images from the prompt, works with "blend"
        public IList<string> ReferencedImages { get; set; } = new List<string>();

        //the human part of the text
        public string Message { get; set; }

        //annotater should call this to get text based on parsed version.
        public string GetAnnotation()
        {
            var res = $"{Message}";
            if (false) //we exclude these now for cleanliness.
            {
                if (Chaos.HasValue)
                {
                    res += $" C:{Chaos}";
                }
                if (Niji.HasValue)
                {
                    res += $" <Niji>";
                }
                if (Seed.HasValue)
                {
                    res += $" Seed:<{Seed}>";
                }
            }
            return res;
        }

        public int? Version { get; set; }
        public int? Chaos { get; set; }
        public bool? Niji { get; set; }
        public long? Seed { get; set; }
        public long? Stylize { get; set; }
        //null=default
        public AR AR { get; set; }

        public GenerationTypeEnum GenerationType { get; set; }
        
        //when the message was created
        public DateTime CreatedAtUtc { get; set; }

        //which channel it was sent on.
        public string CreatedChannelName { get; set; }

        public string Condense(string m)
        {
            return m.Replace("  ", " ").Trim();
        }

        /// <summary>
        /// Rip up the prompt pulling out the various bits of information.
        /// 
        /// Note: Prompts are super messy and can have multiple copies of each qualifier in them, first one takes precedence.
        /// </summary>
        public Prompt(string content, string cleanContent)
        {
            Content = content;
            //content examples
            //"** girl flips her hair --v 4** - <@331647167112413184> (metered, fast)"
            //"**marine biologist facing the sun, sunset, on the beach --v 4 --ar 2:3 --c 44** - <@331647167112413184> (fast)"
            //https://s.mj.run/Wr7eOBOD494 https://s.mj.run/JrtVcYHZsUU --v 4 - @XXX

            //upscales look like this: "anime painting of gandalf leading the ancient israelites --v 4 --ar 2:1 --c 100 - Upscaled by @Username#9999 (fast)"
            //upscale light looks like this: "<https://s.mj.run/xI98Y80tUrY <https://s.mj.run/ylVDjpAtLpQ --v 4 - Upscaled (Light) by @Username#9999 (fast)"
            //normal generations look like this: "twin peaks --v 4 --ar 2:1 --c 100 - @Username#9999 (fast)"
            //there are many other annoying types.

            CleanContent = cleanContent;
            //cleanContent = "marine biologist facing the sun, sunset, on the beach --v 4 --ar 2:3 --c 44 - <X#7120> (fast)"

            //trim leading **
            if (content.Length < 2 || content.IndexOf("**") == -1)
            {
                return;
            }
            var remainingFullMessage = Condense(content).Substring(2).Trim();

            
            //we derive the type of action from the random text mj appends to the end.
            var upscaleChecker = new Regex(@"Upscaled by @(\S+#\d{4,4})").Match(cleanContent);
            if (upscaleChecker.Success)
            {
                GenerationType = GenerationTypeEnum.Upscale;
                cleanContent = cleanContent.Replace(upscaleChecker.Groups[0].Value, "");
                cleanContent = Condense(cleanContent);
                DiscordUser = new DiscordUser(upscaleChecker.Groups[1].Value);
            }

            var upscaleLightChecker = new Regex(@"Upscaled \(Light\) by @(\S+#\d{4,4})").Match(cleanContent);
            if (upscaleLightChecker.Success)
            {
                GenerationType = GenerationTypeEnum.UpscaleLight;
                cleanContent = cleanContent.Replace(upscaleLightChecker.Groups[0].Value, "");
                cleanContent = Condense(cleanContent);
                DiscordUser = new DiscordUser(upscaleLightChecker.Groups[1].Value);
            }

            var upscaleBetaChecker = new Regex(@"Upscaled \(Beta\) by @(\S+#\d{4,4})").Match(cleanContent);
            if (upscaleBetaChecker.Success)
            {
                GenerationType = GenerationTypeEnum.UpscaleBeta;
                cleanContent = cleanContent.Replace(upscaleBetaChecker.Groups[0].Value, "");
                cleanContent = Condense(cleanContent);
                DiscordUser = new DiscordUser(upscaleBetaChecker.Groups[1].Value);
            }

            var remixChecker = new Regex(@"Remix by @(\S+#\d{4,4})").Match(cleanContent);
            if (remixChecker.Success)
            {
                GenerationType = GenerationTypeEnum.Remix;
                cleanContent = cleanContent.Replace(remixChecker.Groups[0].Value, "");
                cleanContent = Condense(cleanContent);
                DiscordUser = new DiscordUser(remixChecker.Groups[1].Value);
            }

            var upscaleAnimeChecker = new Regex(@"Upscaled \(Anime\) by @(\S+#\d{4,4})").Match(cleanContent);
            if (upscaleAnimeChecker.Success)
            {
                GenerationType = GenerationTypeEnum.UpscaleAnime;
                cleanContent = cleanContent.Replace(upscaleAnimeChecker.Groups[0].Value, "");
                cleanContent = Condense(cleanContent);
                DiscordUser = new DiscordUser(upscaleAnimeChecker.Groups[1].Value);
            }

            var upscaleMaxChecker= new Regex(@"Upscaled \(Max\) by @(\S+#\d{4,4})").Match(cleanContent);
            if (upscaleMaxChecker.Success)
            {
                GenerationType = GenerationTypeEnum.UpscaleMax;
                cleanContent = cleanContent.Replace(upscaleMaxChecker.Groups[0].Value, "");
                cleanContent = Condense(cleanContent);
                DiscordUser = new DiscordUser(upscaleMaxChecker.Groups[1].Value);
            }

            var normalGenerationChecker = new Regex(@" - @(\S+#\d{4,4})").Match(cleanContent);
            if (normalGenerationChecker.Success)
            {
                GenerationType = GenerationTypeEnum.NormalGeneration;
                cleanContent = cleanContent.Replace(normalGenerationChecker.Groups[0].Value, "");
                cleanContent = Condense(cleanContent);
                DiscordUser = new DiscordUser(normalGenerationChecker.Groups[1].Value);
            }

            var variationsChecker = new Regex(@" Variations by @(\S+#\d{4,4})").Match(cleanContent);
            if (variationsChecker.Success)
            {
                GenerationType = GenerationTypeEnum.Variations;
                cleanContent = cleanContent.Replace(variationsChecker.Groups[0].Value, "");
                cleanContent = Condense(cleanContent);
                DiscordUser = new DiscordUser(variationsChecker.Groups[1].Value);
            }

            if (DiscordUser == null)
            {
               var a = 4;
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

            //cleaning up human part of message
            var nijiChecker = new Regex(@"--niji").Match(remainingFullMessage);
            if (nijiChecker.Success)
            {
                remainingFullMessage = remainingFullMessage.Replace(nijiChecker.Groups[0].Value, "");
                remainingFullMessage = Condense(remainingFullMessage);
                Niji = true;
            }

            var chaosChecker = new Regex(@"--c (\d+)").Match(remainingFullMessage);
            if (chaosChecker.Success)
            {
                remainingFullMessage = remainingFullMessage.Replace(chaosChecker.Groups[0].Value, "");
                remainingFullMessage = Condense(remainingFullMessage);
                Chaos = int.Parse(chaosChecker.Groups[1].Value);

                var chaosChecker2 = new Regex(@"--c (\d+)").Match(remainingFullMessage);
                if (chaosChecker2.Success)
                {
                    remainingFullMessage = remainingFullMessage.Replace(chaosChecker2.Groups[0].Value, "");
                    remainingFullMessage = Condense(remainingFullMessage);
                }
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

            //remove and save image links for inclusion in the output image.
            //NOTE that 'cleanContent''s version of these links is apparently broken, missing a trailing '>'
            while (true)
            {
                var imageChecker= new Regex(@"<(https://s.mj.run/[^\s]+)>").Match(remainingFullMessage);
                if (imageChecker.Success)
                {
                    remainingFullMessage = remainingFullMessage.Replace(imageChecker.Groups[0].Value, "");
                    remainingFullMessage = Condense(remainingFullMessage);
                    var image = imageChecker.Groups[1].Value;
                    ReferencedImages.Add(image);
                    continue;
                }
                break;
            }

            remainingFullMessage = Condense(remainingFullMessage);

            Message = remainingFullMessage;
        }
    }

    //An aspect ratio holder    
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