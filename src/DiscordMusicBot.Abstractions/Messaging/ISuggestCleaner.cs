using static DiscordMusicBot.Abstractions.Messaging.IMessageSender;

namespace DiscordMusicBot.Abstractions.Messaging
{
    public interface ISuggestCleaner
    {
        Task OnCommandAsync(DiscordMessageInfo requestInfo);
        Task OnSuggestAsync(SuggestSentArgs args);
    }
}
