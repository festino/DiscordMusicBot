using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Abstractions.Messaging;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class ListCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;
        private readonly IAudioPlayer _player;

        public ListCommandExecutor(IMessageSender messageSender, RequestQueue queue, IAudioPlayer player)
        {
            _queue = queue;
            _player = player;
            _messageSender = messageSender;
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

            AudioInfo? info = _player.GetPlaybackInfo();
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
