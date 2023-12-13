using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Youtube
{
    public record VideoHeader(string ChannelName, string Title, TimeSpan DurationMs);
}
