using static SocialAi.Utils;

namespace SocialAi
{
    public class ParsedMessage
    {
        public FileManager FileManager { get; set; }
        public DiscordUser DiscordUser { get; set; }
        public Prompt Prompt { get; set; }
        public string ImageUrl { get; set; }
        public string Filename { get; set; }

        public ParsedMessage(FileManager fm, DiscordUser discordUser, Prompt prompt, string imageUrl, string filename)
        {
            FileManager = fm;
            DiscordUser = discordUser;
            Prompt = prompt;
            ImageUrl = imageUrl;
            Filename = filename;
        }

        public async Task<bool> Save()
        {
            var path = FileManager.GetPathToSave(Filename);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            await DownloadImageAsync(path, ImageUrl);
            FileManager.Annotate(path, Prompt.Message);
            return true;
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