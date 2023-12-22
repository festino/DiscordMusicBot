using static DiscordMusicBot.Abstractions.ICommandSender;

namespace DiscordMusicBot
{
    public interface ICommandWorker
    {
        Task OnCommandAsync(object sender, CommandRecievedArgs args);
    }
}
