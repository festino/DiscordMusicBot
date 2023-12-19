using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;

namespace DiscordMusicBot.Configuration
{
    public record Config(
        string CommandPrefix,
        string DiscordToken,
        string YoutubeToken
    ) : IDiscordConfig, IYoutubeConfig;
}