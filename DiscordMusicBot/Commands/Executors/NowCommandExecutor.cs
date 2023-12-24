using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class NowCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;
        private readonly RequestQueue _queue;
        private readonly IAudioStreamer _streamer;

        public NowCommandExecutor(INotificationService notificationService, RequestQueue queue, IAudioStreamer streamer)
        {
            _notificationService = notificationService;
            _queue = queue;
            _streamer = streamer;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
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
                AudioInfo? info = _streamer.GetPlaybackInfo();
                TimeSpan currentTime = TimeSpan.Zero;
                if (info is not null && info.Video == videos[0])
                {
                    currentTime = info.CurrentTime;
                }

                message += $"\n{videos[0].Header.Title} ({FormatUtils.FormatTimestamps(currentTime, fullTime)})";

                for (int i = 1; i < Math.Min(videos.Count, 2); i++)
                {
                    message += "\n" + videos[i].Header.Title;
                }
            }

            await _notificationService.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}