using DiscordMusicBot.Abstractions.Messaging;
using DiscordMusicBot.Configuration;

namespace DiscordMusicBot.Commands.Executors
{
    public class HelpCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;

        public HelpCommandExecutor(IMessageSender messageSender)
        {
            _messageSender = messageSender;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            string message = LangConfig.CommandHelp;
            await _messageSender.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}
