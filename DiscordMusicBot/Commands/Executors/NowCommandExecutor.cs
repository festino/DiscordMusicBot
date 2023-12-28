using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class NowCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;
        private readonly IAudioStreamer _streamer;

        public NowCommandExecutor(IMessageSender notificationService, RequestQueue queue, IAudioStreamer streamer)
        {
            _messageSender = notificationService;
            _queue = queue;
            _streamer = streamer;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            var history = _queue.GetHistory();
            var videos = _queue.GetVideos();

            string message = "";
            if (history.Length > 0)
            {
                message += "~~" + FormatUtils.FormatVideo(history[^1].Header) + "~~";
            }

            if (videos.Count == 0)
            {
                message += "\n" + LangConfig.CommandListNoVideos;
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

                message += $"\n{FormatUtils.FormatVideo(videos[0].Header)} ({FormatUtils.FormatTimestamps(currentTime, fullTime)})";

                for (int i = 1; i < Math.Min(videos.Count, 2); i++)
                {
                    message += "\n" + FormatUtils.FormatVideo(videos[i].Header);
                }
            }

            await _messageSender.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}