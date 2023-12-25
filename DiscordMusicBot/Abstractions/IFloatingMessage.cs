namespace DiscordMusicBot.Abstractions
{
    public interface IFloatingMessage
    {
        void Update(string? message);
        void Update(Func<string?> messageFactory);

        void OnMessage(DiscordMessageInfo messageInfo, string content);

        Task RunAsync();
    }
}
