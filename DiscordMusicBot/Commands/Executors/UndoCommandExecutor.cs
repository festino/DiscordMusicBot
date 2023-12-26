using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;

namespace DiscordMusicBot.Commands.Executors
{
    public class UndoCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;

        public UndoCommandExecutor(IMessageSender notificationService, RequestQueue queue)
        {
            _messageSender = notificationService;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            Video[]? videos = await _queue.RemoveLastAsync(messageInfo);

            if (videos is null)
            {
                await _messageSender.SendAsync(CommandStatus.Info, "could not skip video", messageInfo);
                return;
            }

            if (videos.Length == 1)
            {
                await _messageSender.SendAsync(CommandStatus.Info, "skip " + videos[0].Header.Title);
                return;
            }

            await _messageSender.SendAsync(CommandStatus.Info,
                                                 "skip\n" + string.Join("\n", videos.Select((v) => v.Header.Title)));
        }
    }
}
