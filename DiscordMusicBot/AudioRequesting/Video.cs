using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.AudioRequesting
{
    public record Video(string YoutubeId, VideoHeader Header, DiscordMessageInfo MessageInfo);
}
