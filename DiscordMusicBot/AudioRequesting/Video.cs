using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;

namespace DiscordMusicBot.AudioRequesting
{
    public record Video(string YoutubeId, VideoHeader Header, DiscordMessageInfo MessageInfo);
}
