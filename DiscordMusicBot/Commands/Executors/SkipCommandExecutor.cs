using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Commands.Executors
{
    public class SkipCommandExecutor : ICommandExecutor
    {
        private readonly RequestQueue _queue;

        public SkipCommandExecutor(RequestQueue queue)
        {
            _queue = queue;
        }

        public async Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo)
        {
            Video? video = await _queue.RemoveCurrentAsync();

            if (video is null)
                return new CommandResponse(CommandResponseStatus.OK, "could not skip video");

            return new CommandResponse(CommandResponseStatus.OK, "skip " + video.Header.Title);
        }
    }
}
