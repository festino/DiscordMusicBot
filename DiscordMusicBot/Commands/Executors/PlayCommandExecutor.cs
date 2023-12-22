using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;
using DiscordMusicBot.Services.Youtube.Data;

namespace DiscordMusicBot.Commands.Executors
{
    public class PlayCommandExecutor : ICommandExecutor
    {
        private readonly int SearchResultCount = 3;
        private readonly RequestQueue _queue;
        private readonly IYoutubeDataProvider _youtubeDataProvider;

        private readonly INotificationService _notificationService;

        public PlayCommandExecutor(INotificationService notificationService,
                                   RequestQueue queue, IYoutubeDataProvider youtubeDataProvider)
        {
            _notificationService = notificationService;
            _queue = queue;
            _youtubeDataProvider = youtubeDataProvider;
        }

        public async Task ExecuteAsync(string args, DiscordMessageInfo discordMessageInfo)
        {
            string[] argsStrs = args.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (argsStrs.Length == 0)
            {
                await _notificationService.SendAsync(new CommandResponse(CommandResponseStatus.Error, "no argument"));
                return;
            }

            if (HasLink(args))
            {
                await _notificationService.SendAsync(await AddVideos(argsStrs, discordMessageInfo));
                return;
            }

            await _notificationService.SendAsync(await SuggestSearch(args, discordMessageInfo));
        }

        private async Task<CommandResponse> SuggestSearch(string query, DiscordMessageInfo discordMessageInfo)
        {
            Tuple<string, VideoHeader>[] options = await _youtubeDataProvider.Search(query);
            string[] topOptions = options.Take(SearchResultCount).Select(t => t.Item2.Title).ToArray();
            if (topOptions.Length == 0)
                return new CommandResponse(CommandResponseStatus.Error, "no search results");

            return new CommandResponse(CommandResponseStatus.Suggest, "choose:\n" + string.Join('\n', topOptions));
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
                return new CommandResponse(CommandResponseStatus.Error, $"could not get ids: {string.Join(", ", badArgs)}");

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
                return new CommandResponse(CommandResponseStatus.Error, $"there are invalid ids: {string.Join(", ", badIds)}");

            for (int i = 0; i < headers.Count; i++)
            {
                _queue.Add(new Video(youtubeIds[i], headers[i], discordMessageInfo));
            }

            return new CommandResponse(CommandResponseStatus.Ok,
                headers.Count == 1 ? $"added song {headers[0].Title}" : $"added {headers.Count} songs"
            );
        }

        private bool HasLink(string s)
        {
            return s.Contains('.') && s.Contains('/');
        }
    }
}
