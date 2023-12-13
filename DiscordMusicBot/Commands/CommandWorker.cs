using DiscordMusicBot.Commands;
using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot
{
    public class CommandWorker : ICommandWorker
    {
        private readonly bool REPLY_UNKNOWN_COMMAND = false;
        private readonly Dictionary<string, ICommandExecutor> executors;

        public CommandWorker(Dictionary<string, ICommandExecutor> executors)
        {
            this.executors = new();
            foreach (var executor in executors)
            {
                this.executors.Add(executor.Key.ToLower(), executor.Value);
            }
        }

        public async Task<CommandResponse> OnCommand(string command, string args, DiscordMessageInfo discordMessageInfo)
        {
            command = command.ToLower();
            if (executors.ContainsKey(command))
                return await executors[command].Execute(args, discordMessageInfo);

            if (REPLY_UNKNOWN_COMMAND)
                return new CommandResponse(CommandResponseStatus.ERROR, "unknown command");

            return new CommandResponse(CommandResponseStatus.OK, "");
        }
    }
}
