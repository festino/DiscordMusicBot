using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Video[]? videos;
            if (args != "l")
            {
                Video? video = await _queue.RemoveCurrentAsync();
                if (video is null)
                    videos = null;
                else
                    videos = new Video[] { video };
            }
            else
            {
                videos = await _queue.RemoveLastAsync(discordMessageInfo);
            }


            if (videos is null)
                return new CommandResponse(CommandResponseStatus.OK, "could not skip video");

            if (videos.Length == 1)
                return new CommandResponse(CommandResponseStatus.OK, "skip " + videos[0].Header.Title);

            return new CommandResponse(CommandResponseStatus.OK, "skip\n" + string.Join("\n", videos.Select((v) => v.Header.Title)));
        }
    }
}
