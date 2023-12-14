using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Commands.Executors
{
    public class HelpCommandExecutor: ICommandExecutor
    {

        public async Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo)
        {
            return new CommandResponse(CommandResponseStatus.OK, "available commands:\n" +
                "help, play, skip, undo, stop, list, now");
        }
    }
}
