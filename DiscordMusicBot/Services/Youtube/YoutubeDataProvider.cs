using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DiscordMusicBot.Services.Youtube.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace DiscordMusicBot.Services.Youtube
{
    public class YoutubeDataProvider : IYoutubeDataProvider
    {
        private readonly int MAX_RESULTS = 50;
        private readonly YouTubeService _youtubeService;

        public YoutubeDataProvider(IYoutubeConfig config)
        {
            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = config.YoutubeToken,
                ApplicationName = this.GetType().ToString()
            });
        }

        public async Task<Tuple<string, VideoHeader>[]> Search(string query)
        {
            var searchListRequest = _youtubeService.Search.List("snippet");
            searchListRequest.Q = query;
            searchListRequest.MaxResults = MAX_RESULTS;

            List<Tuple<string, VideoHeader>> result = new();
            var searchListResponse = await searchListRequest.ExecuteAsync();
            foreach (var searchResult in searchListResponse.Items)
            {
                if (searchResult.Id.Kind != "youtube#video")
                    continue;

                result.Add(Tuple.Create(searchResult.Id.VideoId, GetHeader(searchResult)));
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

                return new YoutubeIdsResult(YoutubeIdSource.PLAYLIST, ids);
            }

            // TODO suggest video, list and list from position if videoId and listId
            return new YoutubeIdsResult(YoutubeIdSource.DIRECT_LINKS, new string[] { videoId });
        }

        public async Task<VideoHeader?[]> GetHeaders(string[] youtubeIds)
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
            foreach (var chunk in youtubeIds.Chunk(MAX_RESULTS))
            {
                var videoListRequest = _youtubeService.Videos.List("snippet, contentDetails");
                videoListRequest.Id = string.Join(",", chunk);
                videoListRequest.MaxResults = MAX_RESULTS;
                tasks.Add(videoListRequest.ExecuteAsync());
            }
            var taskResults = await Task.WhenAll(tasks.ToArray());

            VideoHeader?[] result = new VideoHeader?[youtubeIds.Length];
            foreach (var videoListResponse in taskResults)
            {
                foreach (var video in videoListResponse.Items)
                {
                    if (video.Snippet.LiveBroadcastContent != "none")
                        continue;

                    var header = GetHeader(video);
                    // TODO cache VideoHeader
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
                playlistItemsListRequest.MaxResults = MAX_RESULTS;
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

        private static VideoHeader GetHeader(Video video)
        {
            return new VideoHeader(
                video.Snippet.ChannelTitle,
                video.Snippet.Title,
                XmlConvert.ToTimeSpan(video.ContentDetails.Duration)
            );
        }
        private static VideoHeader GetHeader(SearchResult video)
        {
            return new VideoHeader(
                video.Snippet.ChannelTitle,
                video.Snippet.Title,
                new TimeSpan(-1)
            );
        }

        private string? TryParseVideoId(string arg)
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

        private string? TryParsePlaylistId(string arg)
        {
            return GetQueryParam(arg, "list");
        }

        private string? GetQueryParam(string arg, string param)
        {
            int index = arg.IndexOf(param + "=");
            if (index < 0)
                return null;

            int startIndex = index + param.Length + 1;
            int endIndex = arg.IndexOf("&", startIndex);
            endIndex = endIndex < 0 ? arg.Length : endIndex;
            return arg[startIndex..endIndex];
        }
    }
}
