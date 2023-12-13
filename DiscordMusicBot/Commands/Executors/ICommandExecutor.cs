using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Commands
{
    public interface ICommandExecutor
    {
        Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo);
    }
}
