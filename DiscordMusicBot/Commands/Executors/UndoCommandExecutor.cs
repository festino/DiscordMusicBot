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

        public async Task ExecuteAsync(string args, DiscordMessageInfo discordMessageInfo)
        {
            Video[]? videos = await _queue.RemoveLastAsync(discordMessageInfo);

            if (videos is null)
            {
                await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok, "could not skip video"));
                return;
            }

            if (videos.Length == 1)
            {
                await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok,
                                                         "skip " + videos[0].Header.Title));
                return;
            }

            await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Ok,
                                                 "skip\n" + string.Join("\n", videos.Select((v) => v.Header.Title))));
        }
    }
}
