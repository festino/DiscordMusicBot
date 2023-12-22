using static DiscordMusicBot.Abstractions.ICommandSender;

namespace DiscordMusicBot.Services.Discord
{
    public interface IGuildWatcher
    {
        Task OnCommandAsync(object sender, CommandRecievedArgs args);
        ulong? GetCommandChannel();
    }
}
