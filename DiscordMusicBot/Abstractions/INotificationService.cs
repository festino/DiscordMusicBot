using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Abstractions
{
    public interface INotificationService
    {
        Task SendAsync(CommandResponse message, DiscordMessageInfo? messageInfo = null);
        Task<CommandResponse> OnCommandAsync(string command, string args, DiscordMessageInfo discordMessageInfo);
    }
}
