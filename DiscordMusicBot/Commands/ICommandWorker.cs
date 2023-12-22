using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot
{
    public interface ICommandWorker
    {
        Task<CommandResponse> OnCommandAsync(string command, string args, DiscordMessageInfo discordMessageInfo);
    }
}
