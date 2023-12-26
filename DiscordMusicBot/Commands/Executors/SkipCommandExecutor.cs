using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;

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
                await _messageSender.SendAsync(CommandStatus.Info, "could not skip video", messageInfo);
                return;
            }

            await _messageSender.SendAsync(CommandStatus.Info, "skip " + video.Header.Title);
        }
    }
}
