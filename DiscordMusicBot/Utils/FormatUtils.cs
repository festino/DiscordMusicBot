using Discord;
using DiscordMusicBot.Abstractions;

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

        public static string FormatTimestamp(TimeSpan timestamp)
        {
            string formatStr = timestamp.TotalHours >= 1.0 ? @"hh\:mm\:ss" : @"mm\:ss";
            return timestamp.ToString(formatStr); ;
        }

        public static string FormatProgressBar(double progress, int cellsCount, string cellStates)
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

        public static string FormatVideo(VideoHeader header)
        {
            return header.Title;
        }

        public static string FormatVideos(IEnumerable<VideoHeader> headers)
        {
            // TODO add enumeration, skip middle videos
            return string.Join('\n', headers.Select(h => FormatVideo(h)));
        }
    }
}
