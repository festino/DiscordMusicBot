namespace DiscordMusicBot.Abstractions
{
    public interface INotificationService
    {
        Task SendAsync(CommandStatus status, string message, DiscordMessageInfo? messageInfo = null);
        Task SuggestAsync(string message, SuggestOption[] options, DiscordMessageInfo? messageInfo = null);
    }
}
