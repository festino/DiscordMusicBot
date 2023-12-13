using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;
using DiscordMusicBot.Services.Youtube.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Commands.Executors
{
    public class PlayCommandExecutor : ICommandExecutor
    {
        private readonly int SEARCH_RESULT_COUNT = 3;
        private readonly RequestQueue _queue;
        private readonly IYoutubeDataProvider _youtubeDataProvider;

        public PlayCommandExecutor(RequestQueue queue, IYoutubeDataProvider youtubeDataProvider)
        {
            _queue = queue;
            _youtubeDataProvider = youtubeDataProvider;
        }

        public async Task<CommandResponse> Execute(string args, DiscordMessageInfo discordMessageInfo)
        {
            string[] argsStrs = args.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (argsStrs.Length == 0)
                return new CommandResponse(CommandResponseStatus.ERROR, "no argument");

            if (HasLink(args))
                return await AddVideos(argsStrs, discordMessageInfo);
                
            return await SuggestSearch(args, discordMessageInfo);
        }

        private async Task<CommandResponse> SuggestSearch(string query, DiscordMessageInfo discordMessageInfo)
        {
            Tuple<string, VideoHeader>[] options = await _youtubeDataProvider.Search(query);
            string[] topOptions = options.Take(SEARCH_RESULT_COUNT).Select(t => t.Item2.Title).ToArray();
            if (topOptions.Length == 0)
                return new CommandResponse(CommandResponseStatus.ERROR, "no search results");

            return new CommandResponse(CommandResponseStatus.SUGGEST, "choose:\n" + string.Join('\n', topOptions));
        }

        private async Task<CommandResponse> AddVideos(string[] args, DiscordMessageInfo discordMessageInfo)
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
                return new CommandResponse(CommandResponseStatus.ERROR, $"could not get ids: {string.Join(", ", badArgs)}");

            var headersResult = await _youtubeDataProvider.GetHeaders(youtubeIds.ToArray());
            List<string> badIds = new();
            List<VideoHeader> headers = new();
            for (int i = 0; i < youtubeIds.Count; i++)
            {
                var header = headersResult[i];
                if (header is not null)
                    headers.Add(header);
                else if (idSources[i] != YoutubeIdSource.PLAYLIST)
                    badIds.Add(youtubeIds[i]);
            }

            if (badIds.Count > 0)
                return new CommandResponse(CommandResponseStatus.ERROR, $"there are invalid ids: {string.Join(", ", badIds)}");

            for (int i = 0; i < headers.Count; i++)
            {
                _queue.Add(new Video(youtubeIds[i], headers[i], discordMessageInfo));
            }

            return new CommandResponse(CommandResponseStatus.OK,
                headers.Count == 1 ? $"added song {headers[0].Title}" : $"added {headers.Count} songs"
            );
        }

        private bool HasLink(string s)
        {
            return s.Contains('.') && s.Contains('/');
        }
    }
}
