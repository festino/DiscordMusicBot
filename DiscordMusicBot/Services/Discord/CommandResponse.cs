using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Discord
{
    public record CommandResponse(CommandResponseStatus Status, string Message);
}
