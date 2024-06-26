﻿using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Abstractions.Messaging;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Utils;
using DiscordMusicBot.Youtube.Data;

namespace DiscordMusicBot.Commands.Executors
{
    public class PlayCommandExecutor : ICommandExecutor
    {
        private readonly int SearchResultCount = 3;
        private readonly RequestQueue _queue;
        private readonly IYoutubeDataProvider _youtubeDataProvider;

        private readonly IMessageSender _messageSender;

        public PlayCommandExecutor(IMessageSender messageSender,
                                   RequestQueue queue, IYoutubeDataProvider youtubeDataProvider)
        {
            _messageSender = messageSender;
            _queue = queue;
            _youtubeDataProvider = youtubeDataProvider;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo messageInfo)
        {
            string[] argsStrs = args.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (argsStrs.Length == 0)
            {
                string message = LangConfig.CommandPlayNoArgument;
                await _messageSender.SendAsync(CommandStatus.Error, message, messageInfo);
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
            Tuple<string, VideoHeader>[] topOptions = await _youtubeDataProvider.Search(query, SearchResultCount);
            SuggestOption[] suggestOptions = topOptions
                .Select(t => new SuggestOption(MessageFormatUtils.FormatLabel(t.Item2), $"play https://youtu.be/{t.Item1}"))
                .ToArray();

            if (suggestOptions.Length == 0)
            {
                string message1 = LangConfig.CommandPlaySearchNoOptions;
                await _messageSender.SendAsync(CommandStatus.Error, message1, messageInfo);
                return;
            }

            string links = string.Join(" | ", topOptions.Select(t => FormatUtils.FormatLink($"https://youtu.be/{t.Item1}",
                                                                                            t.Item2.ChannelName)));
            string message = string.Format(LangConfig.CommandPlaySearchOptions, links);
            await _messageSender.SuggestAsync(message, suggestOptions, messageInfo);
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
                string message1 = string.Format(LangConfig.CommandPlayBadArgs, string.Join(", ", badArgs));
                await _messageSender.SendAsync(CommandStatus.Error, message1, messageInfo);
                return;
            }

            var headersResult = await _youtubeDataProvider.GetHeaders(youtubeIds.ToArray());
            List<string> badIds = new();
            List<VideoHeader> headers = new();
            for (int i = 0; i < youtubeIds.Count; i++)
            {
                var header = headersResult[i];
                if (header is not null)
                {
                    if (header.ReasonUnplayable is not null)
                    {
                        badIds.Add($"{youtubeIds[i]} ({header.ReasonUnplayable})");
                    }
                    else if (header.Live)
                    {
                        badIds.Add($"{youtubeIds[i]} (Live broadcasts are currently not available)");
                    }
                    else
                    {
                        headers.Add(header);
                    }
                }
                else if (idSources[i] != YoutubeIdSource.Playlist)
                {
                    badIds.Add(youtubeIds[i]);
                }
            }

            if (badIds.Count > 0)
            {
                string message1 = string.Format(LangConfig.CommandPlayBadIds, string.Join(", ", badIds));
                await _messageSender.SendAsync(CommandStatus.Error, message1, messageInfo);
                return;
            }

            for (int i = 0; i < headers.Count; i++)
            {
                _queue.Add(new Video(youtubeIds[i], headers[i], messageInfo));
            }

            string message;
            if (headers.Count == 1)
                message = string.Format(LangConfig.CommandPlayAddOne, FormatUtils.FormatVideo(headers[0]));
            else
                message = string.Format(LangConfig.CommandPlayAddMany, headers.Count, FormatUtils.FormatVideos(headers));

            await _messageSender.SendAsync(CommandStatus.Info, message);
        }

        private static bool HasLink(string s)
        {
            return s.Contains('.') && s.Contains('/');
        }
    }
}
