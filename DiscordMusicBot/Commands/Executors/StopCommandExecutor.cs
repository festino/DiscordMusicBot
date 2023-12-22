using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class StopCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;
        private readonly RequestQueue _queue;

        public StopCommandExecutor(INotificationService notificationService, RequestQueue queue)
        {
            _notificationService = notificationService;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo discordMessageInfo)
        {
            var list = await _queue.ClearAsync();
            if (list.Count == 0)
            {
                await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok, "queue is empty"));
                return;
            }

            await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok,
                                                 "drop queue\n" + string.Join("\n", list.Select((v) => v.Header.Title))));
        }
    }
}
