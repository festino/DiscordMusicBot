using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.AudioRequesting
{
    public record Video(string YoutubeId, VideoHeader Header, DiscordMessageInfo MessageInfo);
}
