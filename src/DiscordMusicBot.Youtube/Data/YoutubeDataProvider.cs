using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using DiscordMusicBot.Services.Youtube;
using DiscordMusicBot.Youtube.Configuration;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Xml;
using Youtube.Extensions;

namespace DiscordMusicBot.Youtube.Data
{
    public class YoutubeDataProvider : IYoutubeDataProvider
    {
        private const int MaxResults = 50;

        private readonly YouTubeService _youtubeService;

        private readonly ILogger _logger;

        public YoutubeDataProvider(ILogger logger, IYoutubeConfig config)
        {
            _logger = logger;
            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = config.YoutubeToken,
                ApplicationName = GetType().ToString()
            });
        }

        public async Task<Tuple<string, VideoHeader>[]> Search(string query, int maxCount)
        {
            var searchListRequest = _youtubeService.Search.List("snippet");
            searchListRequest.Q = query;
            searchListRequest.MaxResults = MaxResults;

            var searchListResponse = await searchListRequest.ExecuteAsync();
            // search Resource does not contain duration
            List<string> videoIds = new();
            foreach (var searchResult in searchListResponse.Items)
            {
                if (searchResult.Id.Kind != "youtube#video")
                    continue;

                videoIds.Add(searchResult.Id.VideoId);
            }
            VideoHeader?[] videos = await GetHeaders(videoIds.ToArray(), false);

            List<Tuple<string, VideoHeader>> result = new();
            for (int i = 0; i < videos.Length && result.Count < maxCount; i++)
            {
                VideoHeader? header = videos[i];
                if (header is null || await GetReasonUnplayable(videoIds[i]) is not null)
                    continue;

                result.Add(Tuple.Create(videoIds[i], header));
            }
            return result.ToArray();
        }

        public async Task<YoutubeIdsResult?> GetYoutubeIds(string arg)
        {
            string? videoId = TryParseVideoId(arg);
            string? listId = TryParsePlaylistId(arg);
            if (videoId is null)
            {
                if (listId is null)
                    return null;

                var ids = await GetPlaylistIds(listId);
                if (ids is null)
                    return null;

                return new YoutubeIdsResult(YoutubeIdSource.Playlist, ids);
            }

            // TODO suggest video, list and list from position if videoId and listId
            return new YoutubeIdsResult(YoutubeIdSource.DirectLinks, new string[] { videoId });
        }

        public async Task<VideoHeader?[]> GetHeaders(string[] youtubeIds)
        {
            return await GetHeaders(youtubeIds, true);
        }

        private async Task<VideoHeader?[]> GetHeaders(string[] youtubeIds, bool checkPlayability)
        {
            Dictionary<string, List<int>> idsIndices = new();
            for (int i = 0; i < youtubeIds.Length; i++)
            {
                string youtubeId = youtubeIds[i];
                if (!idsIndices.ContainsKey(youtubeId))
                    idsIndices[youtubeIds[i]] = new();

                idsIndices[youtubeId].Add(i);
            }

            List<Task<VideoListResponse>> tasks = new();
            foreach (var chunk in youtubeIds.Chunk(MaxResults))
            {
                var videoListRequest = _youtubeService.Videos.List("snippet, contentDetails");
                videoListRequest.Id = string.Join(",", chunk);
                videoListRequest.MaxResults = MaxResults;
                tasks.Add(videoListRequest.ExecuteAsync());
            }
            var taskResults = await Task.WhenAll(tasks.ToArray());

            VideoHeader?[] result = new VideoHeader?[youtubeIds.Length];
            foreach (var videoListResponse in taskResults)
            {
                foreach (var video in videoListResponse.Items)
                {
                    string? reasonUnplayable = checkPlayability ? await GetReasonUnplayable(video.Id) : null;
                    VideoHeader? header = GetHeader(video, reasonUnplayable);
                    if (header == null)
                        continue;

                    // TODO cache VideoHeader if checkPlayability and is not live
                    foreach (int index in idsIndices[video.Id])
                        result[index] = header;
                }
            }
            return result;
        }

        private async Task<string[]?> GetPlaylistIds(string playlistId)
        {
            List<string> result = new();
            var nextPageToken = "";
            while (nextPageToken != null)
            {
                var playlistItemsListRequest = _youtubeService.PlaylistItems.List("snippet");
                playlistItemsListRequest.PlaylistId = playlistId;
                playlistItemsListRequest.MaxResults = MaxResults;
                playlistItemsListRequest.PageToken = nextPageToken;

                var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();
                foreach (var playlistItem in playlistItemsListResponse.Items)
                {
                    result.Add(playlistItem.Snippet.ResourceId.VideoId);
                }

                nextPageToken = playlistItemsListResponse.NextPageToken;
            }
            return result.ToArray();
        }

        private static VideoHeader? GetHeader(Google.Apis.YouTube.v3.Data.Video video, string? reasonUnplayable)
        {
            if (video.Snippet.LiveBroadcastContent != "none" && video.Snippet.LiveBroadcastContent != "live")
                return null;

            // bool regionBlocked = video.ContentDetails.RegionRestriction.Blocked.Contains("RU");
            return new VideoHeader(
                ChannelName: video.Snippet.ChannelTitle,
                Title: video.Snippet.Title,
                Duration: XmlConvert.ToTimeSpan(video.ContentDetails.Duration),
                Live: video.Snippet.LiveBroadcastContent == "live",
                Copyright: video.ContentDetails.LicensedContent ?? false,
                ReasonUnplayable: reasonUnplayable
            );
        }

        private static string? TryParseVideoId(string arg)
        {
            string? videoId = GetQueryParam(arg, "v");
            if (videoId is not null)
                return videoId;

            int startIndex = arg.LastIndexOf('/');
            if (startIndex < 0)
                return null;

            startIndex++;
            int endIndex = arg.IndexOf('?', startIndex);
            endIndex = endIndex < 0 ? arg.Length : endIndex;
            videoId = arg[startIndex..endIndex];
            return YoutubeUtils.IsValidYoutubeId(videoId) ? videoId : null;
        }

        private static string? TryParsePlaylistId(string arg)
        {
            return GetQueryParam(arg, "list");
        }

        private static string? GetQueryParam(string arg, string param)
        {
            int index = arg.IndexOf(param + "=");
            if (index < 0)
                return null;

            int startIndex = index + param.Length + 1;
            int endIndex = arg.IndexOf("&", startIndex);
            endIndex = endIndex < 0 ? arg.Length : endIndex;
            return arg[startIndex..endIndex];
        }

        private async Task<string?> GetReasonUnplayable(string id)
        {
            HttpClient httpClient = new();
            var response = await httpClient.GetAsync($"https://youtu.be/{id}");
            string responseText = await response.Content.ReadAsStringAsync();
            int index = responseText.LastIndexOf("UNPLAYABLE");
            if (index < 0)
                return null;

            int jsonStart = responseText.LastIndexOf('{', index);
            int jsonEnd = index;
            int bracketsDepth = 1;
            while (bracketsDepth > 0 && jsonEnd < responseText.Length)
            {
                if (responseText[jsonEnd] == '{')
                    bracketsDepth++;
                else if (responseText[jsonEnd] == '}')
                    bracketsDepth--;
                jsonEnd++;
            }
            if (jsonStart < 0 || bracketsDepth > 0)
            {
                _logger.Here().Warning("Could not select json for {YoutubeId}", id);
                return null;
            }

            JToken? jsonObject = JToken.Parse(responseText[jsonStart..jsonEnd]);
            if (jsonObject is null)
            {
                _logger.Here().Warning("Could not parse json for {YoutubeId}", id);
                return null;
            }

            // "playabilityStatus":{"status":"UNPLAYABLE","reason":"...",
            // "errorScreen":{"playerErrorMessageRenderer":
            //     {"subreason":{"runs":[{"text":"..."},{"text":"..."}]},
            //      "reason":{"simpleText":"..."},...}}}
            // subreason is optional
            List<JToken> reasonsTokens = jsonObject.FindTokens("reason");
            string? reason = reasonsTokens.Count == 0 ? null : reasonsTokens.First().ToString();

            List<JToken> subreasonsTokens = jsonObject.FindTokens("subreason");
            List<string>? subreasons = subreasonsTokens.Count == 0 ? null : subreasonsTokens.First().GetInnerStrings();
            string res = reason is null ? "Unknown reason" : reason;
            if (subreasons is null)
                return res;

            return res + ": " + string.Join("", subreasons);
        }
    }
}
