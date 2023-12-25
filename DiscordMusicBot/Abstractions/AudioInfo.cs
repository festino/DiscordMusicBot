using DiscordMusicBot.AudioRequesting;

namespace DiscordMusicBot.Abstractions
{
    public record AudioInfo(Video Video, TimeSpan CurrentTime);
}
