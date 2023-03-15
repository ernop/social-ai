using SixLabors.ImageSharp.Metadata.Profiles.Exif;

using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

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
            var path = FileManager.GetPathToSave(Filename);
            if (path==null)
            {
                return false;
            }
            await DownloadImageAsync(path, ImageUrl, Prompt.CreatedAtUtc);

            //here I should artificially set the created & updated date of the file to the timestamp, so sorting works properly
            //generally this will be better for users.

            //read the filesize to hopefully move it later.
            var im = Image.FromFile(path);
            var width = im.Size.Width;
            im.Dispose();


            var fp = await FileManager.Annotate(path, Prompt);
            AddExif(fp, Prompt);

            //If the image is a mosaic of 4, move it to the "collage" folder.
            //no way to tell this other than looking at the file size currently.

            //note: this probably doesn't work right.
            try
            {
                if (im.Size.Width == 2048)
                {
                    im.Dispose();
                    var newFp = FileManager.GetPathToSave(Filename, mosaic: true);
                    System.IO.File.Move(fp, newFp);
                }
            }catch (Exception ex)
            {
                var a = 45;
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

        private async Task DownloadImageAsync(string path, string url, DateTime createdAtUtc)
        {
            var uri = new Uri(url);
            using var httpClient = new HttpClient();

            var imageBytes = await httpClient.GetByteArrayAsync(uri);
            await File.WriteAllBytesAsync(path, imageBytes);
            File.SetLastWriteTime(path, createdAtUtc);
        }
    }
}
