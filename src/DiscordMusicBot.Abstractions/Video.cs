using DiscordMusicBot.Abstractions.Messaging;

namespace DiscordMusicBot.Abstractions
{
    public record Video(string YoutubeId, VideoHeader Header, DiscordMessageInfo MessageInfo);
}
