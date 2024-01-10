using DiscordMusicBot.Abstractions.Messaging;
using static DiscordMusicBot.Abstractions.Messaging.IMessageSender;

namespace DiscordMusicBot.Discord.Messaging
{
    public class SuggestCleaner : ISuggestCleaner
    {
        private readonly IMessageSender _messageSender;

        private readonly Dictionary<ulong, DiscordMessageInfo> _requestersSuggests = new();

        public SuggestCleaner(IMessageSender messageSender)
        {
            _messageSender = messageSender;
        }

        public async Task OnCommandAsync(DiscordMessageInfo messageInfo)
        {
            await TryDeleteAsync(messageInfo.RequesterId);
        }

        public async Task OnSuggestAsync(SuggestSentArgs args)
        {
            if (args.Request is null) return;

            ulong requesterId = args.Request.RequesterId;
            await TryDeleteAsync(requesterId);
            _requestersSuggests[requesterId] = args.Suggest;
        }

        private async Task TryDeleteAsync(ulong requesterId)
        {
            if (!_requestersSuggests.ContainsKey(requesterId)) return;

            await _messageSender.DeleteAsync(_requestersSuggests[requesterId]);
            _requestersSuggests.Remove(requesterId);
        }
    }
}
