using AsyncEvent;
using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using Serilog;
using static DiscordMusicBot.Abstractions.IMessageSender;

namespace DiscordMusicBot.Services.Discord
{
    public class DiscordMessageSender : IMessageSender
    {
        private const int MaxMessageLength = 2000;
        private const int MaxLabelLength = 80;

        private readonly ILogger _logger;

        private readonly DiscordSocketClient _client;

        private readonly IGuildWatcher _guildWatcher;

        public event AsyncEventHandler<SuggestSentArgs>? SuggestSent;

        public DiscordMessageSender(ILogger logger, IGuildWatcher guildWatcher, DiscordSocketClient client)
        {
            _logger = logger;
            _guildWatcher = guildWatcher;
            _client = client;
        }

        public async Task<DiscordMessageInfo?> SendAsync(CommandStatus status, string message, DiscordMessageInfo? messageInfo = null)
        {
            return await SendMessageAsync(messageInfo, message);
        }

        public async Task<DiscordMessageInfo?> SuggestAsync(string message, SuggestOption[] options, DiscordMessageInfo? messageInfo = null)
        {
            var builder = new ComponentBuilder();
            foreach (SuggestOption option in options)
            {
                ActionRowBuilder rowBuilder = new ActionRowBuilder();
                string label = RestrictButtonLabel(option.Caption);
                rowBuilder = rowBuilder.WithButton(label, option.MessageOnClick, ButtonStyle.Secondary);
                builder.AddRow(rowBuilder);
            }

            DiscordMessageInfo? suggestInfo = await SendMessageAsync(messageInfo, message, builder.Build());
            if (suggestInfo is not null)
            {
                Task? task = SuggestSent?.InvokeAsync(this, new SuggestSentArgs(suggestInfo, messageInfo));
                if (task is not null)
                    await task;
            }
            return suggestInfo;
        }

        public async Task DeleteAsync(DiscordMessageInfo messageInfo)
        {
            IChannel channel = await _client.GetChannelAsync(messageInfo.ChannelId);
            if (channel is not ISocketMessageChannel messageChannel)
                return;

            await messageChannel.DeleteMessageAsync(messageInfo.MessageId);
        }

        public async Task EditAsync(CommandStatus status, string message, DiscordMessageInfo messageInfo)
        {
            IChannel channel = await _client.GetChannelAsync(messageInfo.ChannelId);
            if (channel is not ISocketMessageChannel messageChannel)
                return;

            message = RestrictMessage(message);
            await messageChannel.ModifyMessageAsync(messageInfo.MessageId, (properties) => properties.Content = message);
        }

        private async Task<DiscordMessageInfo?> SendMessageAsync(DiscordMessageInfo? messageInfo, string message, MessageComponent? components = null)
        {
            var channel = await GetMessageChannelAsync(messageInfo);
            if (channel is null) return null;

            var guildChannel = channel as SocketGuildChannel;
            var guildId = guildChannel?.Guild.Id;
            if (guildId is null) return null;

            message = RestrictMessage(message);
            IUserMessage userMessage = await channel.SendMessageAsync(message, components: components);
            IUser user = userMessage.Author;
            return new DiscordMessageInfo(user.GlobalName, user.Id, (ulong)guildId, userMessage.Channel.Id, userMessage.Id);
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

        private static string RestrictMessage(string message)
        {
            return RestrictLength(message, MaxMessageLength);
        }

        private static string RestrictButtonLabel(string message)
        {
            return RestrictLength(message, MaxLabelLength);
        }

        private static string RestrictLength(string message, int maxLength)
        {
            if (message.Length > maxLength)
            {
                message = message[..(maxLength - 3)] + "...";
            }
            return message;
        }
    }
}
