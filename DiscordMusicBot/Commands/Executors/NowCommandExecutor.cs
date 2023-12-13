using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Commands.Executors
{
    public class NowCommandExecutor : ICommandExecutor
    {
        private readonly RequestQueue _queue;

        public NowCommandExecutor(RequestQueue queue)
        {
            _queue = queue;
        }

        public async Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo)
        {
            var history = _queue.GetHistory();
            var videos = _queue.GetVideos();

            string message = "~~" + (history.Length == 0 ? "no history" : history[^1].Header.Title) + "~~";

            if (videos.Count == 0)
            {
                message += "\nqueue is empty";
            }
            else
            {
                for (int i = 0; i < Math.Min(videos.Count, 2); i++)
                    message += "\n" + videos[i].Header.Title;
            }

            return new CommandResponse(CommandResponseStatus.OK, message);
        }
    }
}