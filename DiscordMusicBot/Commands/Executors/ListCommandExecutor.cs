using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class ListCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;
        private readonly IAudioStreamer _streamer;

        public ListCommandExecutor(IMessageSender notificationService, RequestQueue queue, IAudioStreamer streamer)
        {
            _queue = queue;
            _streamer = streamer;
            _messageSender = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            List<Video> videos = _queue.GetVideos();
            if (videos.Count == 0)
            {
                string message1 = LangConfig.CommandListNoVideos;
                await _messageSender.SendAsync(CommandStatus.Info, message1, messageInfo);
                return;
            }

            var fullTime = TimeSpan.FromSeconds(videos.Sum(v => v.Header.Duration.TotalSeconds));

            AudioInfo? info = _streamer.GetPlaybackInfo();
            TimeSpan currentTime = TimeSpan.Zero;
            if (info is not null && info.Video == videos[0])
            {
                currentTime = info.CurrentTime;
            }
            fullTime -= currentTime;

            string fullTimeStr = FormatUtils.FormatTimestamp(fullTime);
            string videoListStr = FormatUtils.FormatVideos(videos.Select(v => v.Header).ToList());
            string message = string.Format(LangConfig.CommandListTemplate, videos.Count, fullTimeStr, videoListStr);
            await _messageSender.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}
