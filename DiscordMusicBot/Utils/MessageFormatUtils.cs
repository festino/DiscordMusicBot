using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Utils
{
    public class MessageFormatUtils
    {
        private readonly static string CellStates = "░▒▓█";
        private readonly static int ProgressBarLength = 30;

        private readonly static int MinLoadingDots = 0;
        private readonly static int MaxLoadingDots = 3;
        private static int LoadingDotsCount = MaxLoadingDots;

        public static string? FormatPlayingMessage(AudioInfo? audioInfo)
        {
            if (audioInfo is null)
            {
                if (++LoadingDotsCount > MaxLoadingDots)
                    LoadingDotsCount = MinLoadingDots;

                return "Loading" + new string('.', LoadingDotsCount);
            }

            double progress = audioInfo.CurrentTime.TotalSeconds / audioInfo.Video.Header.Duration.TotalSeconds;
            string timeBar = FormatUtils.FormatProgressBar(progress, ProgressBarLength, CellStates);
            string timeStr = FormatUtils.FormatTimestamps(audioInfo.CurrentTime, audioInfo.Video.Header.Duration);
            if (progress == 0.0)
                return string.Format("Loading {0}\n{1} {2}", audioInfo.Video.Header.Title, timeBar, timeStr);

            return string.Format("Playing {0}\n{1} {2}", audioInfo.Video.Header.Title, timeBar, timeStr);
        }

        public static string FormatLabel(VideoHeader header)
        {
            return string.Format("({1}) {0}", header.Title, FormatUtils.FormatTimestamp(header.Duration));
        }
    }
}
