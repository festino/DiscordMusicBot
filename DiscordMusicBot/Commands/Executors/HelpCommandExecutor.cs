using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class HelpCommandExecutor : ICommandExecutor
    {

        public async Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo)
        {
            return new CommandResponse(CommandResponseStatus.OK, "available commands:\n" +
                "help, play, skip, undo, stop, list, now");
        }
    }
}
