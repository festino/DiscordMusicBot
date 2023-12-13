using AsyncEvent;

namespace DiscordMusicBot.AudioRequesting
{
    public interface IAudioDownloader
    {
        //delegate void OnLoadCompleted(string youtubeId, string path);
        //delegate void OnLoadFailed(string youtubeId);
        record LoadCompletedArgs(string YoutubeId, string Path);
        record LoadFailedArgs(string YoutubeId);

        event AsyncEventHandler<LoadCompletedArgs>? LoadCompleted;
        event AsyncEventHandler<LoadFailedArgs>? LoadFailed;

        void RequestDownload(string youtubeId, bool notify);
        void StopDownloading(string youtubeId);
    }
}