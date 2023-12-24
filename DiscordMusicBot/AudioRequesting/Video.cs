using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.AudioRequesting
{
    public record Video(string YoutubeId, VideoHeader Header, DiscordMessageInfo MessageInfo);
}
