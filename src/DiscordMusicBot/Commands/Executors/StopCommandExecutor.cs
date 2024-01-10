using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Abstractions.Messaging;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class StopCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;

        public StopCommandExecutor(IMessageSender messageSender, RequestQueue queue)
        {
            _messageSender = messageSender;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            List<Video> list = await _queue.ClearAsync();
            if (list.Count == 0)
            {
                string message1 = LangConfig.CommandStopNoVideos;
                await _messageSender.SendAsync(CommandStatus.Info, message1, messageInfo);
                return;
            }

            string videosListStr = FormatUtils.FormatVideos(list.Select(v => v.Header).ToList());
            string message = string.Format(LangConfig.CommandStopMany, videosListStr);
            await _messageSender.SendAsync(CommandStatus.Info, message);
        }
    }
}
