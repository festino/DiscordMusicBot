using static DiscordMusicBot.Abstractions.Messaging.ICommandSender;

namespace DiscordMusicBot
{
    public interface ICommandWorker
    {
        Task OnCommandAsync(object sender, CommandRecievedArgs args);
    }
}
