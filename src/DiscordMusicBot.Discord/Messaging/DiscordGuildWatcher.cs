using DiscordMusicBot.Services.Discord;
using static DiscordMusicBot.Abstractions.Messaging.ICommandSender;

namespace DiscordMusicBot.Discord.Messaging
{
    public class DiscordGuildWatcher : IGuildWatcher
    {
        private const int QueueSize = 10;
        private readonly Queue<ulong> _lastChannels = new();

        public Task OnCommandAsync(object sender, CommandRecievedArgs args)
        {
            _lastChannels.Enqueue(args.MessageInfo.ChannelId);
            if (_lastChannels.Count > QueueSize)
            {
                _lastChannels.Dequeue();
            }
            return Task.CompletedTask;
        }

        public ulong? GetCommandChannel()
        {
            if (_lastChannels.Count == 0)
                return null;

            return _lastChannels
                    .GroupBy(id => id)
                    .Select(g => (g.Key, Count: g.Count()))
                    .MaxBy(t => t.Count)
                    .Key;
        }
    }
}
