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
            await SendMessageAsync(messageInfo, message);
        }

        public async Task SuggestAsync(string message, SuggestOption[] options, DiscordMessageInfo? messageInfo = null)
        {
            var builder = new ComponentBuilder();
            foreach (SuggestOption option in options)
            {
                builder = builder.WithButton(option.Caption, option.MessageOnClick, ButtonStyle.Secondary);
            }

            message = RestrictMessage(message);
            await SendMessageAsync(messageInfo, message, builder.Build());
        }

        private async Task SendMessageAsync(DiscordMessageInfo? messageInfo, string message, MessageComponent? components = null)
        {
            var channel = await GetMessageChannelAsync(messageInfo);
            if (channel is null) return;

            message = RestrictMessage(message);
            await channel.SendMessageAsync(message, components: components);
        }

        private async Task<ISocketMessageChannel?> GetMessageChannelAsync(DiscordMessageInfo? messageInfo)
        {
            ulong? channelId = messageInfo?.ChannelId ?? _guildWatcher.GetCommandChannel();
            if (channelId is null)
            {
                _logger.Here().Error("Could not send message, no channel id");
                return null;
            }

            IChannel? channel = await _client.GetChannelAsync((ulong)channelId);
            if (channel is not ISocketMessageChannel messageChannel)
            {
                _logger.Here().Error("Could not send message, channel #{ChannelId} ({Channel}) " +
                                     "is not a message channel", channelId, channel);
                return null;
            }

            return messageChannel;
        }

        private string RestrictMessage(string message)
        {
            message = Format.Sanitize(message);
            if (message.Length > MaxMessageLength)
            {
                message = message[..(MaxMessageLength - 3)] + "...";
            }
            return message;
        }
    }
}
