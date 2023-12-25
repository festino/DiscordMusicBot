﻿using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using Serilog;

namespace DiscordMusicBot.Services.Discord
{
    public class FloatingMessage : IFloatingMessage
    {
        private const int UpdateDelayMs = 1000;

        private readonly ILogger _logger;

        private readonly INotificationService _notificationService;

        private DiscordMessageInfo? _messageInfo = null;
        private string? _message = null;

        private bool _isEdited = false;
        private bool _isLast = true;

        public FloatingMessage(ILogger logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                var timeStart = DateTime.Now;

                await TickAsync();

                int msPassed = (int)(DateTime.Now - timeStart).TotalMilliseconds;
                int delayMs = Math.Max(0, UpdateDelayMs - msPassed);
                await Task.Delay(delayMs);
            }
        }

        public async Task UpdateAsync(string? message)
        {
            if (message == _message) return;

            _message = message;
            _isEdited = true;
        }

        public async Task OnMessageAsync(DiscordMessageInfo messageInfo, string content)
        {
            if (_messageInfo is null || _message is null) return;
            if (messageInfo.ChannelId != _messageInfo.ChannelId) return;
            if (messageInfo.GuildId != _messageInfo.GuildId) return;
            if (messageInfo.MessageId == _messageInfo.MessageId) return;
            if (content == _message) return;

            _isLast = false;
        }

        private async Task TickAsync()
        {
            if (_message is null && _messageInfo is null) return;

            if (_message is null)
            {
                await DeleteMessageAsync();
                return;
            }

            if (_messageInfo is null)
            {
                await CreateMessageAsync();
                return;
            }

            if (_isLast && !_isEdited) return;

            if (!_isLast)
            {
                await DeleteMessageAsync();
                await CreateMessageAsync();
                return;
            }

            if (_isEdited)
            {
                await _notificationService.EditAsync(CommandStatus.Info, _message, _messageInfo);
            }
        }

        private async Task CreateMessageAsync()
        {
            if (_message is null)
            {
                _logger.Here().Error("Could not create null floating message");
                return;
            }

            _isEdited = false;
            _isLast = true;
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
