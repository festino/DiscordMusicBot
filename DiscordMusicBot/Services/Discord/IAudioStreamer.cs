using AsyncEvent;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.AudioRequesting
{
    public interface IAudioStreamer
    {
        enum PlaybackEndedStatus { Ok, Stopped, Disconnected }
        record PlaybackEndedArgs(PlaybackEndedStatus Status, Video Video);
        event AsyncEventHandler<PlaybackEndedArgs>? Finished;

        ulong? GuildId { get; set; }

        Task JoinAndPlayAsync(Video video, Stream pcmStream, Func<ulong[]> getRequesterIds);

        Task PauseAsync();
        Task ResumeAsync();
        Task StopAsync();

        AudioInfo? GetCurrentTime();
    }
}
