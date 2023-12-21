using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class ListCommandExecutor : ICommandExecutor
    {
        private readonly RequestQueue _queue;

        public ListCommandExecutor(RequestQueue queue)
        {
            _queue = queue;
        }

        public async Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo)
        {
            var list = _queue.GetVideos();
            if (list.Count == 0)
                return new CommandResponse(CommandResponseStatus.Ok, "queue is empty");

            var fullTime = TimeSpan.FromSeconds(list.Sum(v => v.Header.Duration.TotalSeconds));
            string message = $"{list.Count} songs, {fullTime}\n";
            message += string.Join("\n", list.Select(v => v.Header.Title));
            return new CommandResponse(CommandResponseStatus.Ok, message);
        }
    }
}
