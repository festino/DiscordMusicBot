using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class HelpCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;

        public HelpCommandExecutor(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo discordMessageInfo)
        {
            await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok, "available commands:\n" +
                                                 "help, play, skip, undo, stop, list, now"));
        }
    }
}
