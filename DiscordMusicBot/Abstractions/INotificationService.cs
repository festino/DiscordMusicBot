using DiscordMusicBot.Services.Discord;
using static DiscordMusicBot.Abstractions.ICommandSender;

namespace DiscordMusicBot.Abstractions
{
    public interface INotificationService
    {
        Task SendAsync(CommandResponse message, DiscordMessageInfo? messageInfo = null);
        Task OnCommandAsync(object sender, CommandRecievedArgs args);
    }
}
