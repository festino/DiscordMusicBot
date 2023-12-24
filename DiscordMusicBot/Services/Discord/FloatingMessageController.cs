using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Services.Discord
{
    public class FloatingMessageController : IFloatingMessageController
    {
        private const int ProgressBarLength = 30;
        private const int UpdateDelayMs = 1000;

        private readonly IFloatingMessage _floatingMessage;

        private readonly IAudioStreamer _audioStreamer;

        public FloatingMessageController(IFloatingMessage floatingMessage, IAudioStreamer audioStreamer)
        {
            _floatingMessage = floatingMessage;
            _audioStreamer = audioStreamer;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                int delayMs = UpdateDelayMs;
                AudioInfo? audioInfo = _audioStreamer.GetPlaybackInfo();
                if (audioInfo is not null)
                {
                    var timeStart = DateTime.Now;
                    await UpdateTimeAsync(audioInfo);
                    int msPassed = (int)(DateTime.Now - timeStart).TotalMilliseconds;
                    delayMs = Math.Max(0, delayMs - msPassed);
                }
                await Task.Delay(delayMs);
            }
        }

        private async Task UpdateTimeAsync(AudioInfo audioInfo)
        {
            double progress = audioInfo.CurrentTime.TotalSeconds / audioInfo.Video.Header.Duration.TotalSeconds;
            int completedCells = (int)Math.Floor(progress * ProgressBarLength);
            string timeBar = new string('█', completedCells) + new string('░', ProgressBarLength - completedCells);
            string timeStr = FormatUtils.FormatTimestamps(audioInfo.CurrentTime, audioInfo.Video.Header.Duration);
            string message = string.Format("Playing {0}\n{1} {2}", audioInfo.Video.Header.Title, timeBar, timeStr);
            await _floatingMessage.UpdateAsync(message);
        }
    }
}
