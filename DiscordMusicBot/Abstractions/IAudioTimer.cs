using AsyncEvent;

namespace DiscordMusicBot.Abstractions
{
    public interface IAudioTimer
    {
        record TimeUpdatedArgs(AudioInfo AudioInfo);
        event AsyncEventHandler<TimeUpdatedArgs>? TimeUpdated;

        Task RunAsync();
    }
}
