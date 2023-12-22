using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Abstractions;
using Serilog;

namespace DiscordMusicBot.Services.Discord
{
    public class DiscordNotificationService : INotificationService
    {
        private const int QueueSize = 10;
        private readonly Queue<ulong> _lastChannels = new();

        private readonly ILogger _logger;

        private readonly DiscordSocketClient _client;

        public DiscordNotificationService(ILogger logger, DiscordBot bot)
        {
            _logger = logger;
            _client = bot.Client;
        }

        public async Task SendAsync(CommandResponse message, DiscordMessageInfo? messageInfo = null)
        {
            ulong? channelId = messageInfo?.ChannelId ?? GetCommandChannel();
            if (channelId is null)
            {
                _logger.Error("Could not send message, no channel id: {Message}", message);
                return;
            }

            IChannel? channel = await _client.GetChannelAsync((ulong)channelId);
            if (channel is not ISocketMessageChannel messageChannel)
            {
                _logger.Error("Could not send message, channel #{ChannelId} ({Channel}) " +
                              "is not a message channel: {Message}", channelId, channel, message);
                return;
            }

            await messageChannel.SendMessageAsync(message.Message);
        }

        public async Task<CommandResponse> OnCommandAsync(string command, string args, DiscordMessageInfo discordMessageInfo)
        {
            _lastChannels.Enqueue(discordMessageInfo.ChannelId);
            if (_lastChannels.Count > QueueSize)
            {
                _lastChannels.Dequeue();
            }
            return new CommandResponse(CommandResponseStatus.Empty, "");
        }

        private ulong? GetCommandChannel()
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
