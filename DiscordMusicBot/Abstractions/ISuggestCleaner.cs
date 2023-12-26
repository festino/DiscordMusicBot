using static DiscordMusicBot.Abstractions.IMessageSender;

namespace DiscordMusicBot.Abstractions
{
    public interface ISuggestCleaner
    {
        Task OnCommandAsync(DiscordMessageInfo requestInfo);
        Task OnSuggestAsync(SuggestSentArgs args);
    }
}
