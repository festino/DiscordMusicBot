using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class SkipCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;

        public SkipCommandExecutor(IMessageSender notificationService, RequestQueue queue)
        {
            _messageSender = notificationService;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            Video? video = await _queue.RemoveCurrentAsync();

            if (video is null)
            {
                string message1 = "Could not skip video";
                await _messageSender.SendAsync(CommandStatus.Info, message1, messageInfo);
                return;
            }

            string message = string.Format("Skipped {0}", FormatUtils.FormatVideo(video.Header));
            await _messageSender.SendAsync(CommandStatus.Info, message);
        }
    }
}
