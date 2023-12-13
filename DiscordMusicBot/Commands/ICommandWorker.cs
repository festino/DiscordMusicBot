using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot
{
    public interface ICommandWorker
    {
        Task<CommandResponse> OnCommand(string command, string args, DiscordMessageInfo discordMessageInfo);
    }
}
