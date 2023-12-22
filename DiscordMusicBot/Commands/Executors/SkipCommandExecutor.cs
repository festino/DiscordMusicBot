using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class SkipCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;
        private readonly RequestQueue _queue;

        public SkipCommandExecutor(INotificationService notificationService, RequestQueue queue)
        {
            _notificationService = notificationService;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            Video? video = await _queue.RemoveCurrentAsync();

            if (video is null)
            {
                await _notificationService.SendAsync(CommandStatus.Info, "could not skip video", messageInfo);
                return;
            }

            await _notificationService.SendAsync(CommandStatus.Info, "skip " + video.Header.Title);
        }
    }
}
