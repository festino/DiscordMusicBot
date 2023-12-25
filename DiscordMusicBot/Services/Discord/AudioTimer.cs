using AsyncEvent;
using DiscordMusicBot.Abstractions;
using static DiscordMusicBot.Abstractions.IAudioTimer;

namespace DiscordMusicBot.Services.Discord
{
    public class AudioTimer : IAudioTimer
    {
        private const int UpdateDelayMs = 1000;

        private readonly IAudioStreamer _audioStreamer;

        public event AsyncEventHandler<TimeUpdatedArgs>? TimeUpdated;

        public AudioTimer(IAudioStreamer audioStreamer)
        {
            _audioStreamer = audioStreamer;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                var timeStart = DateTime.Now;

                AudioInfo? audioInfo = _audioStreamer.GetPlaybackInfo();
                if (audioInfo is not null)
                {
                    Task? task = TimeUpdated?.InvokeAsync(this, new TimeUpdatedArgs(audioInfo));
                    if (task is not null)
                        await task;
                }

                int msPassed = (int)(DateTime.Now - timeStart).TotalMilliseconds;
                int delayMs = Math.Max(0, UpdateDelayMs - msPassed);
                await Task.Delay(delayMs);
            }
        }
    }
}
