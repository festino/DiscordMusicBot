using AsyncEvent;
using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.AudioRequesting
{
    public interface IAudioStreamer
    {
        enum PlaybackEndedStatus { OK, STOPPED, DISCONNECTED }
        record PlaybackEndedArgs(PlaybackEndedStatus Status, Video Video);
        event AsyncEventHandler<PlaybackEndedArgs>? Finished;

        Task JoinAndPlayAsync(Video video, Stream pcmStream, Func<ulong[]> getRequesterIds);

        Task PauseAsync();
        Task ResumeAsync();
        Task StopAsync();

        AudioInfo? GetCurrentTime();
    }
}
