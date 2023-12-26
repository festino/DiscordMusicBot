using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Commands.Executors
{
    public class HelpCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;

        public HelpCommandExecutor(IMessageSender notificationService)
        {
            _messageSender = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            string message = string.Format("Available commands:\nhelp, play, skip, undo, stop, list, now");
            await _messageSender.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}
