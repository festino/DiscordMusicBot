using AsyncEvent;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Abstractions
{
    public interface IAudioStreamer
    {
        enum PlaybackEndedStatus { Ok, Stopped, Disconnected }
        record PlaybackEndedArgs(PlaybackEndedStatus Status, Video Video);
        event AsyncEventHandler<PlaybackEndedArgs>? Finished;

        ulong? GuildId { get; set; }

        Task JoinAndPlayAsync(Video video, Stream pcmStream, Func<ulong[]> getRequesterIds);
        void RequestLeave();

        Task PauseAsync();
        Task ResumeAsync();
        Task StopAsync();

        AudioInfo? GetPlaybackInfo();
    }
}
