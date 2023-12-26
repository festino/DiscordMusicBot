﻿using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Youtube.Data;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.Executors
{
    public class PlayCommandExecutor : ICommandExecutor
    {
        private readonly int SearchResultCount = 3;
        private readonly RequestQueue _queue;
        private readonly IYoutubeDataProvider _youtubeDataProvider;

        private readonly IMessageSender _messageSender;

        public PlayCommandExecutor(IMessageSender notificationService,
                                   RequestQueue queue, IYoutubeDataProvider youtubeDataProvider)
        {
            _messageSender = notificationService;
            _queue = queue;
            _youtubeDataProvider = youtubeDataProvider;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            string[] argsStrs = args.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (argsStrs.Length == 0)
            {
                await _messageSender.SendAsync(CommandStatus.Error, "no argument", messageInfo);
                return;
            }

            if (HasLink(args))
            {
                await AddVideos(argsStrs, messageInfo);
                return;
            }

            await SuggestSearch(args, messageInfo);
        }

        private async Task SuggestSearch(string query, DiscordMessageInfo messageInfo)
        {
            Tuple<string, VideoHeader>[] options = await _youtubeDataProvider.Search(query);
            var topOptions = options
                .Take(SearchResultCount);
            SuggestOption[] suggestOptions = topOptions
                .Select(t => new SuggestOption(MessageFormatUtils.FormatLabel(t.Item2), $"play https://youtu.be/{t.Item1}"))
                .ToArray();

            if (suggestOptions.Length == 0)
            {
                await _messageSender.SendAsync(CommandStatus.Error, "no search results", messageInfo);
                return;
            }

            string text = "choose:\n" + string.Join(" | ", topOptions.Select(t => $"[{t.Item2.ChannelName}](https://youtu.be/{t.Item1})"));
            await _messageSender.SuggestAsync(text, suggestOptions, messageInfo);
        }

        private async Task AddVideos(string[] args, DiscordMessageInfo messageInfo)
        {
            List<string> badArgs = new();
            List<string> youtubeIds = new();
            List<YoutubeIdSource> idSources = new();
            foreach (string arg in args)
            {
                var idsChunk = await _youtubeDataProvider.GetYoutubeIds(arg);
                if (idsChunk is null)
                {
                    badArgs.Add(arg);
                }
                else
                {
                    youtubeIds.AddRange(idsChunk.YoutubeIds);
                    idSources.AddRange(Enumerable.Repeat(idsChunk.Source, youtubeIds.Count));
                }
            }

            if (badArgs.Count > 0)
            {
                await _messageSender.SendAsync(CommandStatus.Error, $"could not get ids: {string.Join(", ", badArgs)}", messageInfo);
                return;
            }

            var headersResult = await _youtubeDataProvider.GetHeaders(youtubeIds.ToArray());
            List<string> badIds = new();
            List<VideoHeader> headers = new();
            for (int i = 0; i < youtubeIds.Count; i++)
            {
                var header = headersResult[i];
                if (header is not null)
                    headers.Add(header);
                else if (idSources[i] != YoutubeIdSource.Playlist)
                    badIds.Add(youtubeIds[i]);
            }

            if (badIds.Count > 0)
            {
                await _messageSender.SendAsync(CommandStatus.Error, $"there are invalid ids: {string.Join(", ", badIds)}", messageInfo);
                return;
            }

            for (int i = 0; i < headers.Count; i++)
            {
                _queue.Add(new Video(youtubeIds[i], headers[i], messageInfo));
            }

            await _messageSender.SendAsync(CommandStatus.Info,
                headers.Count == 1 ? $"added song {headers[0].Title}" : $"added {headers.Count} songs"
            );
        }

        private static bool HasLink(string s)
        {
            return s.Contains('.') && s.Contains('/');
        }
    }
}
