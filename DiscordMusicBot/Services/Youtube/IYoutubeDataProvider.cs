using DiscordMusicBot.Services.Youtube.Data;

namespace DiscordMusicBot.Services.Youtube
{
    public interface IYoutubeDataProvider
    {
        Task<Tuple<string, VideoHeader>[]> Search(string query);
        Task<YoutubeIdsResult?> GetYoutubeIds(string arg);
        Task<VideoHeader?[]> GetHeaders(string[] youtubeIds);
    }
}
