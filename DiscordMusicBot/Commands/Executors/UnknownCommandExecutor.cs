using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Commands.Executors
{
    public class UnknownCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;

        public UnknownCommandExecutor(IMessageSender notificationService)
        {
            _messageSender = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            await _messageSender.SendAsync(CommandStatus.Info, "unknown command", messageInfo);
        }
    }
}
