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

        public async Task ExecuteAsync(string args, DiscordMessageInfo discordMessageInfo)
        {
            Video? video = await _queue.RemoveCurrentAsync();

            if (video is null)
            {
                await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok, "could not skip video"));
                return;
            }

            await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok, "skip " + video.Header.Title));
        }
    }
}
