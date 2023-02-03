
namespace SocialAi
{
    public class DiscordUser
    {
        public string? DiscordUsername { get; set; }

        public DiscordUser(string? discordUsername)
        {
            DiscordUsername = discordUsername;
        }

        public override string ToString()
        {
            return $"DiscordUser: {DiscordUsername}";
        }
    }
}