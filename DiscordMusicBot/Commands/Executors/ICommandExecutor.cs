using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands
{
    public interface ICommandExecutor
    {
        Task ExecuteAsync(string args, DiscordMessageInfo messageInfo);
    }
}
