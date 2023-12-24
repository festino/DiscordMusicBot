using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Commands.Executors
{
    public class UnknownCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;

        public UnknownCommandExecutor(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            await _notificationService.SendAsync(CommandStatus.Info, "unknown command", messageInfo);
        }
    }
}
