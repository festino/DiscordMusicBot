using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;

namespace DiscordMusicBot.Commands.Executors
{
    public class ListCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;

        public ListCommandExecutor(IMessageSender notificationService, RequestQueue queue)
        {
            _queue = queue;
            _messageSender = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            var list = _queue.GetVideos();
            if (list.Count == 0)
            {
                await _messageSender.SendAsync(CommandStatus.Info, "queue is empty", messageInfo);
                return;
            }

            var fullTime = TimeSpan.FromSeconds(list.Sum(v => v.Header.Duration.TotalSeconds));
            string message = $"{list.Count} songs, {fullTime}\n";
            message += string.Join("\n", list.Select(v => v.Header.Title));
            await _messageSender.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}
