using DiscordMusicBot.Discord.Configuration;
using DiscordMusicBot.Youtube.Configuration;

namespace DiscordMusicBot.Configuration
{
    public record Config(
        string CommandPrefix,
        string DiscordToken,
        string YoutubeToken
    ) : IDiscordConfig, IYoutubeConfig;
}