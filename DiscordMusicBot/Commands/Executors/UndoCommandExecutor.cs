using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class UndoCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;

        public UndoCommandExecutor(IMessageSender notificationService, RequestQueue queue)
        {
            _messageSender = notificationService;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            Video[]? videos = await _queue.RemoveLastAsync(messageInfo);

            if (videos is null)
            {
                string message1 = LangConfig.CommandSkipNoVideos;
                await _messageSender.SendAsync(CommandStatus.Info, message1, messageInfo);
                return;
            }

            if (videos.Length == 1)
            {
                string message1 = string.Format(LangConfig.CommandSkipOne, FormatUtils.FormatVideo(videos[0].Header));
                await _messageSender.SendAsync(CommandStatus.Info, message1);
                return;
            }

            string videosListStr = FormatUtils.FormatVideos(videos.Select((v) => v.Header).ToList());
            string message = string.Format(LangConfig.CommandSkipMany, videos.Length, videosListStr);
            await _messageSender.SendAsync(CommandStatus.Info, message);
        }
    }
}
