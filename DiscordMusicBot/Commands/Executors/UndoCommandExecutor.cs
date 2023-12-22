using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class UndoCommandExecutor : ICommandExecutor
    {
        private readonly INotificationService _notificationService;
        private readonly RequestQueue _queue;

        public UndoCommandExecutor(INotificationService notificationService, RequestQueue queue)
        {
            _notificationService = notificationService;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            Video[]? videos = await _queue.RemoveLastAsync(messageInfo);

            if (videos is null)
            {
                await _notificationService.SendAsync(CommandStatus.Info, "could not skip video", messageInfo);
                return;
            }

            if (videos.Length == 1)
            {
                await _notificationService.SendAsync(CommandStatus.Info, "skip " + videos[0].Header.Title);
                return;
            }

            await _notificationService.SendAsync(CommandStatus.Info,
                                                 "skip\n" + string.Join("\n", videos.Select((v) => v.Header.Title)));
        }
    }
}
