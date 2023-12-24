﻿using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Commands.Executors
{
    public class HelpCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;

        public HelpCommandExecutor(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            await _notificationService.SendAsync(CommandStatus.Info, "available commands:\n" +
                                                 "help, play, skip, undo, stop, list, now", messageInfo);
        }
    }
}
