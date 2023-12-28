using Discord;
using DiscordMusicBot.Abstractions;
using System.Text;
using System.Text.RegularExpressions;

namespace DiscordMusicBot.Utils
{
    public static class FormatUtils
    {
        private static readonly int BeginningHeadersCount = 5;
        private static readonly int EndingHeadersCount = 5;

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
            if (progress < 0.0)
                throw new ArgumentOutOfRangeException(nameof(progress));

            if (cellsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellsCount));

            int n = cellStates.Length - 1;
            if (n <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellStates) + ".Length");

            int stateProgress = (int)Math.Floor(progress * cellsCount * n);
            int completedCellsCount = stateProgress / n;
            char currentCell = cellStates[stateProgress % n];

            string completedCells = new(cellStates[^1], completedCellsCount);
            if (completedCellsCount >= cellsCount)
                return completedCells;

            string emptyCells = new(cellStates[0], cellsCount - completedCellsCount - 1);
            return completedCells + currentCell + emptyCells;
        }

        public static string FormatVideo(VideoHeader header)
        {
            return header.Title;
        }

        public static string? FormatLink(string link, string label)
        {
            label = Regex.Replace(label, @"\uD83D[\uDC00-\uDFFF]|\uD83C[\uDC00-\uDFFF]|\uFFFD", "");
            return $"[{label}]({link})";
        }

        public static string FormatVideos(IReadOnlyList<VideoHeader> headers)
        {
            StringBuilder sb = new();
            if (headers.Count < BeginningHeadersCount + 1 + EndingHeadersCount)
            {
                AppendConsequentVideos(sb, headers, 0, headers.Count);
                return sb.ToString();
            }

            AppendConsequentVideos(sb, headers, 0, BeginningHeadersCount);
            sb.Append("\n...\n");
            AppendConsequentVideos(sb, headers, headers.Count - EndingHeadersCount, headers.Count);
            return sb.ToString();
        }

        private static void AppendConsequentVideos(StringBuilder sb, IReadOnlyList<VideoHeader> headers, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (i != start)
                    sb.Append('\n');

                sb.Append(string.Format("{0}. {1}", i + 1, FormatVideo(headers[i])));
            }
        }
    }
}
