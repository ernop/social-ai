using SixLabors.ImageSharp.Metadata.Profiles.Exif;

using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Net;

namespace SocialAi
{
    /// <summary>
    /// kind of a useless class, just stores actions that can be done on a Prompt
    /// </summary>
    public class ParsedMessage
    {
        public FileManager FileManager { get; set; }
        public Prompt Prompt { get; set; }
        public string ImageUrl { get; set; }
        public string Filename { get; set; }

        public ParsedMessage(FileManager fm, Prompt prompt, string imageUrl, string filename)
        {
            FileManager = fm;
            Prompt = prompt;
            ImageUrl = imageUrl;
            Filename = filename;
        }

        public async Task<bool> SaveAndAnnotateImage()
        {
            if (string.IsNullOrEmpty(Filename))
            {
                throw new ArgumentNullException("missing filename");
            }

            var newFilename = string.Join("_", Filename.Split('_').Skip(1));
            var srcPath = "";


            //2023.12 weird that I never fixed the naming system so that ones which are single images stand out in some way.
            //I suppose I should also just make up a prefix system fora ll types, like upscale etc for sorting? Hmm well I can do that whenever.
            if (Prompt.GenerationType == GenerationTypeEnum.UpscaleSingle)
            {
                newFilename = $"S_{newFilename}";
            }
            var subFolder = "";
            if (Prompt.GenerationType == GenerationTypeEnum.UpscaleSingle)
            { subFolder = "s"; }
            else
            {
                subFolder = "m";
            }
            var joined = $"{FileManager.Settings.RawMJOutputImagePath}/{subFolder}/{newFilename}";
            var joinedCleaned = $"{FileManager.Settings.CleanedImageOutputFullPath}/{subFolder}/{newFilename}";
            if (File.Exists(joined))
            {
                srcPath = joined;
            }
            else
            {
                if (File.Exists(joinedCleaned))
                    srcPath = joinedCleaned;
                else
                {
                    var res = await DownloadImageAsync(joined, ImageUrl, Prompt.CreatedAtUtc);
                    srcPath = joined;
                    if (!res)
                    {
                        return false;
                    }
                }
            }

            var annotatedPath = $"{FileManager.Settings.AnnotatedImageOutputFullPath}/{newFilename}";

            if (!File.Exists(annotatedPath))
            {
                var fp = await FileManager.Annotate(srcPath, annotatedPath, Prompt);
                AddExif(fp, Prompt);
            }

            return true;
        }

        /// <summary>
        /// unknown if this actually works or not
        /// </summary>
        private void AddExif(string fp, Prompt prompt)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(fp))
            {
                image.Metadata.ExifProfile = new ExifProfile();
                image.Metadata.ExifProfile.SetValue(ExifTag.Artist, "Midjourney+SocialAI:https://github.com/ernop/social-ai/");
                image.Metadata.ExifProfile.SetValue(ExifTag.UserComment, prompt.Content);
            }
        }

        private async Task<bool> DownloadImageAsync(string path, string url, DateTime createdAtUtc)
        {
            Thread.Sleep(500);
            var uri = new Uri(url);
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(200) };
            try
            {
                var imageBytes = await httpClient.GetByteArrayAsync(uri);
                await File.WriteAllBytesAsync(path, imageBytes);
                File.SetLastWriteTimeUtc(path, createdAtUtc);
                File.SetCreationTimeUtc(path, createdAtUtc);
                Console.WriteLine($"Wrote: {path}");
                return true;
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(path))
                {
                    Console.WriteLine($"File existed but couldn't be finalized: {path} so deleting.");
                    System.IO.File.Delete(path);
                }
                Console.WriteLine($"Exception while downloading, will be picked up again later. {path} {ex}");
                return false;
            }
        }
    }
}
