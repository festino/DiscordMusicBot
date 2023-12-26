using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
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
                string message1 = "Could not skip video";
                await _messageSender.SendAsync(CommandStatus.Info, message1, messageInfo);
                return;
            }

            if (videos.Length == 1)
            {
                string message1 = string.Format("Skipped {0}", FormatUtils.FormatVideo(videos[0].Header));
                await _messageSender.SendAsync(CommandStatus.Info, message1);
                return;
            }

            string message = string.Format("Skipped {0} videos:\n{1}",
                                           videos.Length, FormatUtils.FormatVideos(videos.Select((v) => v.Header)));
            await _messageSender.SendAsync(CommandStatus.Info, message);
        }
    }
}
