using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Services.Discord
{
    public class FloatingMessageController : IFloatingMessageController
    {
        private const string CellStates = "░▒▓█";
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
                var timeStart = DateTime.Now;

                AudioInfo? audioInfo = _audioStreamer.GetPlaybackInfo();
                if (audioInfo is not null)
                {
                    await UpdateTimeAsync(audioInfo);
                }

                int msPassed = (int)(DateTime.Now - timeStart).TotalMilliseconds;
                int delayMs = Math.Max(0, UpdateDelayMs - msPassed);
                await Task.Delay(delayMs);
            }
        }

        private async Task UpdateTimeAsync(AudioInfo audioInfo)
        {
            double progress = audioInfo.CurrentTime.TotalSeconds / audioInfo.Video.Header.Duration.TotalSeconds;
            string timeBar = GetProgressBar(progress, ProgressBarLength, CellStates);

            string timeStr = FormatUtils.FormatTimestamps(audioInfo.CurrentTime, audioInfo.Video.Header.Duration);
            string message = string.Format("Playing {0}\n{1} {2}", audioInfo.Video.Header.Title, timeBar, timeStr);
            await _floatingMessage.UpdateAsync(message);
        }

        private static string GetProgressBar(double progress, int cellsCount, string cellStates)
        {
            if (cellsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellsCount));

            int n = cellStates.Length - 1;
            if (n <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellStates) + ".Length");

            int stateProgress = (int)Math.Floor(progress * cellsCount * n);
            int completedCellsCount = stateProgress / n;
            char currentCell = cellStates[stateProgress % n];

            string completedCells = new string(cellStates[^1], completedCellsCount);
            string emptyCells = new string(cellStates[0], cellsCount - completedCellsCount - 1);
            return completedCells + currentCell + emptyCells;
        }
    }
}
