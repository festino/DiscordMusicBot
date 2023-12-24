using Discord;

namespace DiscordMusicBot.Utils
{
    public static class FormatUtils
    {
        public static string FormatTimestamps(TimeSpan currentTime, TimeSpan fullTime)
        {
            string formatStr = fullTime.TotalHours >= 1.0 ? @"hh\:mm\:ss" : @"mm\:ss";
            string currentTimeStr = currentTime.ToString(formatStr);
            string fullTimeStr = fullTime.ToString(formatStr);
            return Format.Code($"{currentTimeStr} / {fullTimeStr}");
        }
    }
}
