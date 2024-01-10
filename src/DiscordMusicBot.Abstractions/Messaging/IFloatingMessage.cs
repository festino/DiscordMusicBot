namespace DiscordMusicBot.Abstractions.Messaging
{
    public interface IFloatingMessage
    {
        void Update(string? message);
        void Update(Func<string?> messageFactory);

        void OnMessage(DiscordMessageInfo messageInfo, string content);

        Task RunAsync();
    }
}
