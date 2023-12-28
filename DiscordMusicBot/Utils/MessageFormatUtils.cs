using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Configuration;

namespace DiscordMusicBot.Utils
{
    public class MessageFormatUtils
    {
        private readonly static string CellStates = "░▒▓█";
        private readonly static int ProgressBarLength = 30;

        private readonly static int MinLoadingDots = 0;
        private readonly static int MaxLoadingDots = 3;
        private static int LoadingDotsCount = MaxLoadingDots;

        public static string FormatJoiningMessage()
        {
            return string.Format(LangConfig.JoiningVoiceChannel, GetLoadingDots());
        }

        public static string FormatPlayingMessage(AudioInfo? audioInfo)
        {
            if (audioInfo is null)
            {
                return string.Format(LangConfig.Loading, GetLoadingDots());
            }

            VideoHeader header = audioInfo.Video.Header;
            double progress = audioInfo.CurrentTime.TotalSeconds / header.Duration.TotalSeconds;
            string timeBar = FormatUtils.FormatProgressBar(progress, ProgressBarLength, CellStates);
            string timeStr = FormatUtils.FormatTimestamps(audioInfo.CurrentTime, header.Duration);
            if (progress == 0.0)
                return string.Format(LangConfig.LoadingAudio, FormatUtils.FormatVideo(header), timeBar, timeStr);

            return string.Format(LangConfig.PlayingAudio, FormatUtils.FormatVideo(header), timeBar, timeStr);
        }

        public static string FormatLabel(VideoHeader header)
        {
            return string.Format("({1}) {0}", FormatUtils.FormatVideo(header), FormatUtils.FormatTimestamp(header.Duration));
        }

        private static string GetLoadingDots()
        {
            if (++LoadingDotsCount > MaxLoadingDots)
                LoadingDotsCount = MinLoadingDots;

            return new string('.', LoadingDotsCount);
        }
    }
}
