using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Utils
{
    public class MessageFormatUtils
    {
        private const string CellStates = "░▒▓█";
        private const int ProgressBarLength = 30;

        public static string? FormatPlayingMessage(AudioInfo audioInfo)
        {
            double progress = audioInfo.CurrentTime.TotalSeconds / audioInfo.Video.Header.Duration.TotalSeconds;
            string timeBar = FormatUtils.FormatProgressBar(progress, ProgressBarLength, CellStates);

            string timeStr = FormatUtils.FormatTimestamps(audioInfo.CurrentTime, audioInfo.Video.Header.Duration);
            return string.Format("Playing {0}\n{1} {2}", audioInfo.Video.Header.Title, timeBar, timeStr);
        }
    }
}
