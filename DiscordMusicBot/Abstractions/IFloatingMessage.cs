namespace DiscordMusicBot.Abstractions
{
    public interface IFloatingMessage
    {
        Task UpdateAsync(string? message);
        Task OnMessageAsync(DiscordMessageInfo messageInfo, string content);
        Task RunAsync();
    }
}
