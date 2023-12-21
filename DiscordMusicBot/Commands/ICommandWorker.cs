using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot
{
    public interface ICommandWorker
    {
        Task<CommandResponse> OnCommand(string command, string args, DiscordMessageInfo discordMessageInfo);
    }
}
