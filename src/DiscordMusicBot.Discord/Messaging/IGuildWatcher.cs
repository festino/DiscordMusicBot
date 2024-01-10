using static DiscordMusicBot.Abstractions.Messaging.ICommandSender;

namespace DiscordMusicBot.Services.Discord
{
    public interface IGuildWatcher
    {
        Task OnCommandAsync(object sender, CommandRecievedArgs args);
        ulong? GetCommandChannel();
    }
}
