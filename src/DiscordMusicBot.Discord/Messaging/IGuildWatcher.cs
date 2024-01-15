using static DiscordMusicBot.Abstractions.Messaging.ICommandSender;

namespace DiscordMusicBot.Services.Discord
{
    public interface IGuildWatcher
    {
        public ulong? GuildId { get; set; }

        Task OnCommandAsync(object sender, CommandRecievedArgs args);
        ulong? GetCommandChannel();
    }
}
