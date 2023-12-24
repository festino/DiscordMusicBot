using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Commands
{
    public interface ICommandExecutor
    {
        Task ExecuteAsync(string args, DiscordMessageInfo messageInfo);
    }
}
