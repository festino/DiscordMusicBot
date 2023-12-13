using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Commands.Executors
{
    public class StopCommandExecutor : ICommandExecutor
    {
        private readonly RequestQueue _queue;

        public StopCommandExecutor(RequestQueue queue)
        {
            _queue = queue;
        }

        public async Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo)
        {
            var list = await _queue.ClearAsync();
            if (list.Count == 0)
                return new CommandResponse(CommandResponseStatus.OK, "queue is empty");

            return new CommandResponse(CommandResponseStatus.OK, "drop queue\n" + string.Join("\n", list.Select((v) => v.Header.Title)));
        }
    }
}
