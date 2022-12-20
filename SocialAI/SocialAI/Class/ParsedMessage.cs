using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace SocialAi
{
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

        public async Task<bool> SaveAndAnnotate()
        {
            var path = FileManager.GetPathToSave(Filename);
            if (path==null)
            {
                return false;
            }
            await DownloadImageAsync(path, ImageUrl);
            var fp = FileManager.Annotate(path, Prompt.Message);
            AddExif(fp, Prompt);
            return true;
        }

        //unknown if this actually works or not
        private void AddExif(string fp, Prompt prompt)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(fp))
            {
                image.Metadata.ExifProfile = new ExifProfile();
                image.Metadata.ExifProfile.SetValue(ExifTag.Artist, "Midjourney+SocialAI:https://github.com/ernop/social-ai/");
                image.Metadata.ExifProfile.SetValue(ExifTag.UserComment, prompt.Content);
            }
        }

        private async Task DownloadImageAsync(string path, string url)
        {
            var uri = new Uri(url);
            using var httpClient = new HttpClient();

            var imageBytes = await httpClient.GetByteArrayAsync(uri);
            await File.WriteAllBytesAsync(path, imageBytes);
        }
    }
}
