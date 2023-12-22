using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using Serilog;

namespace DiscordMusicBot.Services.Discord
{
    public class DiscordNotificationService : INotificationService
    {
        private const int MaxMessageLength = 2000;

        private readonly ILogger _logger;

        private readonly DiscordSocketClient _client;

        private readonly IGuildWatcher _guildWatcher;

        public DiscordNotificationService(ILogger logger, IGuildWatcher guildWatcher, DiscordBot bot)
        {
            _logger = logger;
            _guildWatcher = guildWatcher;
            _client = bot.Client;
        }

        public async Task SendAsync(CommandStatus status, string message, DiscordMessageInfo? messageInfo = null)
        {
            ulong? channelId = messageInfo?.ChannelId ?? _guildWatcher.GetCommandChannel();
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

            string responseMessage = Format.Sanitize(message);
            if (responseMessage.Length > MaxMessageLength)
            {
                responseMessage = responseMessage[..(MaxMessageLength - 3)] + "...";
            }

            await messageChannel.SendMessageAsync(responseMessage);
        }
    }
}
