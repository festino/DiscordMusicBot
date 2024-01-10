using AsyncEvent;

namespace DiscordMusicBot.Abstractions
{
    public interface IAudioPlayer
    {
        enum PlaybackEndedStatus { Ok, Stopped, Disconnected }
        record PlaybackEndedArgs(PlaybackEndedStatus Status, Video Video);
        event AsyncEventHandler<PlaybackEndedArgs>? Finished;

        AudioInfo? GetPlaybackInfo();

        Task JoinAndPlayAsync(Video video, Stream pcmStream, Func<ulong[]> getRequesterIds);
        void RequestLeave();

        Task PauseAsync();
        Task ResumeAsync();
        Task StopAsync();
    }
}
