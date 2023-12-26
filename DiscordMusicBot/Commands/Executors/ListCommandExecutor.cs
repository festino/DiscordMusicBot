﻿using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class ListCommandExecutor : ICommandExecutor
    {
        private readonly IMessageSender _messageSender;
        private readonly RequestQueue _queue;

        public ListCommandExecutor(IMessageSender notificationService, RequestQueue queue)
        {
            _queue = queue;
            _messageSender = notificationService;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            List<Video> list = _queue.GetVideos();
            if (list.Count == 0)
            {
                string message1 = "queue is empty";
                await _messageSender.SendAsync(CommandStatus.Info, message1, messageInfo);
                return;
            }

            var fullTime = TimeSpan.FromSeconds(list.Sum(v => v.Header.Duration.TotalSeconds));
            string videoListStr = FormatUtils.FormatVideos(list.Select(v => v.Header).ToList());
            string message = string.Format("{0} songs, {1}\n{2}", list.Count, fullTime, videoListStr);
            await _messageSender.SendAsync(CommandStatus.Info, message, messageInfo);
        }
    }
}
