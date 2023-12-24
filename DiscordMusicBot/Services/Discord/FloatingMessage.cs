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

        public FloatingMessage(ILogger logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task UpdateAsync(string? message)
        {
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
