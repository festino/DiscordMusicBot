using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class NowCommandExecutor : ICommandExecutor
    {
        private readonly RequestQueue _queue;
        private readonly IAudioStreamer _streamer;

        public NowCommandExecutor(RequestQueue queue, IAudioStreamer streamer)
        {
            _queue = queue;
            _streamer = streamer;
        }

        public async Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo)
        {
            var history = _queue.GetHistory();
            var videos = _queue.GetVideos();

            string message = "~~" + (history.Length == 0 ? "no history" : history[^1].Header.Title) + "~~";

            if (videos.Count == 0)
            {
                message += "\nqueue is empty";
            }
            else
            {
                TimeSpan fullTime = videos[0].Header.Duration;
                AudioInfo? info = _streamer.GetCurrentTime();
                TimeSpan currentTime = TimeSpan.Zero;
                if (info is not null && info.Video == videos[0])
                {
                    currentTime = info.CurrentTime;
                }

                string formatStr = fullTime.TotalHours >= 1.0 ? @"hh\:mm\:ss" : @"mm\:ss";
                string currentTimeStr = currentTime.ToString(formatStr);
                string fullTimeStr = fullTime.ToString(formatStr);
                message += $"\n{videos[0].Header.Title} ({currentTimeStr} / {fullTimeStr})";

                for (int i = 1; i < Math.Min(videos.Count, 2); i++)
                {
                    message += "\n" + videos[i].Header.Title;
                }
            }

            return new CommandResponse(CommandResponseStatus.Ok, message);
        }
    }
}