using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;

namespace DiscordMusicBot.Commands.Executors
{
    public class StopCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;

        public StopCommandExecutor(IMessageSender notificationService, RequestQueue queue)
        {
            _messageSender = notificationService;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            var list = await _queue.ClearAsync();
            if (list.Count == 0)
            {
                await _messageSender.SendAsync(CommandStatus.Info, "queue is empty", messageInfo);
                return;
            }

            await _messageSender.SendAsync(CommandStatus.Info,
                                                 "drop queue\n" + string.Join("\n", list.Select((v) => v.Header.Title)));
        }
    }
}
