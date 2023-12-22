using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Abstractions
{
    public interface INotificationService
    {
        Task SendAsync(CommandStatus status, string message, DiscordMessageInfo? messageInfo = null);
    }
}
