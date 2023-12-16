﻿using AsyncEvent;
using System.Diagnostics;

namespace DiscordMusicBot.AudioRequesting
{
    public interface IAudioDownloader
    {
        record LoadCompletedArgs(string YoutubeId, Process PcmStreamProcess);
        record LoadFailedArgs(string YoutubeId);

        event AsyncEventHandler<LoadCompletedArgs>? LoadCompleted;
        event AsyncEventHandler<LoadFailedArgs>? LoadFailed;

        void RequestDownload(string youtubeId, bool notify);
        void StopDownloading(string youtubeId);
    }
}