using AsyncEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.AudioRequesting
{
    public interface IAudioStreamer
    {
        //delegate void OnFinished(Video video);
        event AsyncEventHandler<Video>? Finished;

        Task<bool> JoinAsync(ulong[] requesterIds);

        Task StartAsync(Video video, string path);
        Task PauseAsync();
        Task ResumeAsync();
        Task StopAsync();
    }
}
