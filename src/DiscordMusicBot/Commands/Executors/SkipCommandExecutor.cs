using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Abstractions.Messaging;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class SkipCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;

        public SkipCommandExecutor(IMessageSender messageSender, RequestQueue queue)
        {
            _messageSender = messageSender;
            _queue = queue;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            Video? video = await _queue.RemoveCurrentAsync();

            if (video is null)
            {
                string message1 = LangConfig.CommandSkipNoVideos;
                await _messageSender.SendAsync(CommandStatus.Info, message1, messageInfo);
                return;
            }

            string message = string.Format(LangConfig.CommandSkipOne, FormatUtils.FormatVideo(video.Header));
            await _messageSender.SendAsync(CommandStatus.Info, message);
        }
    }
}
