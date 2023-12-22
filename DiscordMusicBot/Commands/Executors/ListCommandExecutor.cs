using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class ListCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;
        private readonly RequestQueue _queue;

        public ListCommandExecutor(INotificationService notificationService, RequestQueue queue)
        {
            _queue = queue;
            _notificationService = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            var list = _queue.GetVideos();
            if (list.Count == 0)
            {
                await _notificationService.SendAsync(CommandStatus.Info, "queue is empty", messageInfo);
                return;
            }

            var fullTime = TimeSpan.FromSeconds(list.Sum(v => v.Header.Duration.TotalSeconds));
            string message = $"{list.Count} songs, {fullTime}\n";
            message += string.Join("\n", list.Select(v => v.Header.Title));
            await _notificationService.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}
