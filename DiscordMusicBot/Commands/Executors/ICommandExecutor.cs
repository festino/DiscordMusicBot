using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands
{
    public interface ICommandExecutor
    {
        Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo);
    }
}
