using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using Serilog;
using static DiscordMusicBot.Abstractions.ICommandSender;

namespace DiscordMusicBot.Services.Discord
{
    public class DiscordNotificationService : INotificationService
    {
        private const int MaxMessageLength = 2000;

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
                _logger.Here().Error("Could not send message, no channel id: {Message}", message);
                return;
            }

            IChannel? channel = await _client.GetChannelAsync((ulong)channelId);
            if (channel is not ISocketMessageChannel messageChannel)
            {
                _logger.Here().Error("Could not send message, channel #{ChannelId} ({Channel}) " +
                                     "is not a message channel: {Message}", channelId, channel, message);
                return;
            }

            string responseMessage = message.Message;
            if (responseMessage.Length > MaxMessageLength)
            {
                responseMessage = responseMessage[..(MaxMessageLength - 3)] + "...";
            }

            await messageChannel.SendMessageAsync(responseMessage);
        }

        public Task OnCommandAsync(object sender, CommandRecievedArgs args)
        {
            _lastChannels.Enqueue(args.MessageInfo.ChannelId);
            if (_lastChannels.Count > QueueSize)
            {
                _lastChannels.Dequeue();
            }
            return Task.CompletedTask;
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
