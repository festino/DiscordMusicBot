using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Utils
{
    public class MessageFormatUtils
    {
        private const string CellStates = "░▒▓█";
        private const int ProgressBarLength = 30;

        public static string? FormatPlayingMessage(AudioInfo? audioInfo)
        {
            if (audioInfo is null)
                return "Loading" + new string('.', Random.Shared.Next(3, 5));

            double progress = audioInfo.CurrentTime.TotalSeconds / audioInfo.Video.Header.Duration.TotalSeconds;
            string timeBar = FormatUtils.FormatProgressBar(progress, ProgressBarLength, CellStates);
            string timeStr = FormatUtils.FormatTimestamps(audioInfo.CurrentTime, audioInfo.Video.Header.Duration);
            if (progress == 0.0)
                return string.Format("Loading {0}\n{1} {2}", audioInfo.Video.Header.Title, timeBar, timeStr);

            return string.Format("Playing {0}\n{1} {2}", audioInfo.Video.Header.Title, timeBar, timeStr);
        }

        public static string FormatLabel(VideoHeader header)
        {
            return "(" + FormatUtils.FormatTimestamp(header.Duration) + ") " + header.Title;
        }
    }
}
