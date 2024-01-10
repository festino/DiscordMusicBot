namespace DiscordMusicBot.Abstractions
{
    public interface IAudioDownloader
    {
        record LoadCompletedArgs(string YoutubeId, Stream PcmStream);
        record LoadFailedArgs(string YoutubeId);


        void RequestDownload(string youtubeId,
                             Func<LoadCompletedArgs, Task> OnCompleted,
                             Func<LoadFailedArgs, Task> OnFailed);
    }
}