using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Configuration;

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
            string message = LangConfig.CommandHelp;
            await _messageSender.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}
