using DiscordMusicBot.Abstractions.Messaging;
using DiscordMusicBot.Configuration;

namespace DiscordMusicBot.Commands.Executors
{
    public class UnknownCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;

        public UnknownCommandExecutor(IMessageSender messageSender)
        {
            _messageSender = messageSender;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            string message = LangConfig.CommandUnknown;
            await _messageSender.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}
