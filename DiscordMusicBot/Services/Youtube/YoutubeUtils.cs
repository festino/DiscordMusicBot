namespace DiscordMusicBot.Services.Youtube
{
    public static class YoutubeUtils
    {
        public static bool IsValidYoutubeId(string youtubeId)
        {
            return youtubeId.Length == 11 && youtubeId.All(c => IsValidYoutubeIdChar(c));
        }

        public static bool IsValidYoutubeIdChar(char c)
        {
            return 'a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || '0' <= c && c <= '9' || c == '-' || c == '_';
        }
    }
}
