using DiscordMusicBot.Abstractions;

namespace DiscordMusicBot.Youtube.Data
{
    public interface IYoutubeDataProvider
    {
        Task<Tuple<string, VideoHeader>[]> Search(string query);
        Task<YoutubeIdsResult?> GetYoutubeIds(string arg);
        Task<VideoHeader?[]> GetHeaders(string[] youtubeIds);
    }
}
