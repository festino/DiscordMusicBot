using AsyncEvent;
using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.AudioRequesting
{
    public interface IAudioStreamer
    {
        event AsyncEventHandler<Video>? Finished;

        Task JoinAndPlayAsync(Video video, string path, Func<ulong[]> getRequesterIds);

        Task PauseAsync();
        Task ResumeAsync();
        Task StopAsync();

        AudioInfo? GetCurrentTime();
    }
}
