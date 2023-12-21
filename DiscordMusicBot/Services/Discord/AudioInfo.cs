using DiscordMusicBot.AudioRequesting;

namespace DiscordMusicBot.Services.Discord
{
    public record AudioInfo(Video Video, TimeSpan CurrentTime);
}
