using DiscordMusicBot.Services.Youtube.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Youtube
{
    public interface IYoutubeDataProvider
    {
        Task<Tuple<string, VideoHeader>[]> Search(string query);
        Task<YoutubeIdsResult?> GetYoutubeIds(string arg);
        Task<VideoHeader?[]> GetHeaders(string[] youtubeIds);
    }
}
