using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using Serilog;

namespace DiscordMusicBot.Services.Discord
{
    public class FloatingMessage : IFloatingMessage
    {
        private readonly ILogger _logger;

        private readonly INotificationService _notificationService;

        private DiscordMessageInfo? _messageInfo = null;
        private string? _message;

        public FloatingMessage(ILogger logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task UpdateAsync(string? message)
        {
            if (message == _message) return;

            _message = message;
            if (message is null)
            {
                await DeleteMessageAsync();
                return;
            }

            if (_messageInfo is null)
            {
                _messageInfo = await _notificationService.SendAsync(CommandStatus.Info, message);
            }
            else
            {
                await _notificationService.EditAsync(CommandStatus.Info, message, _messageInfo);
            }
        }

        public async Task OnMessageAsync(DiscordMessageInfo messageInfo, string content)
        {
            if (_messageInfo is null || _message is null) return;
            if (messageInfo.ChannelId != _messageInfo.ChannelId) return;
            if (messageInfo.GuildId != _messageInfo.GuildId) return;
            if (messageInfo.MessageId == _messageInfo.MessageId) return;
            if (content == _message) return;

            await _notificationService.DeleteAsync(_messageInfo);
            _messageInfo = await _notificationService.SendAsync(CommandStatus.Info, _message);
        }

        private async Task DeleteMessageAsync()
        {
            if (_messageInfo is null)
            {
                _logger.Here().Error("Could not delete null floating message");
                return;
            }

            await _notificationService.DeleteAsync(_messageInfo);
        }
    }
}
