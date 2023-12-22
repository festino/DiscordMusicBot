using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class UnknownCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;

        public UnknownCommandExecutor(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo discordMessageInfo)
        {
            await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok, "unknown command"));
        }
    }
}
