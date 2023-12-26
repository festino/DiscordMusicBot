namespace DiscordMusicBot.Abstractions
{
    public interface IMessageSender
    {
        Task<DiscordMessageInfo?> SendAsync(CommandStatus status, string message, DiscordMessageInfo? messageInfo = null);
        Task<DiscordMessageInfo?> SuggestAsync(string message, SuggestOption[] options, DiscordMessageInfo? messageInfo = null);
        Task DeleteAsync(DiscordMessageInfo messageInfo);
        Task EditAsync(CommandStatus status, string message, DiscordMessageInfo messageInfo);
    }
}
