using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Discord.Volume
{
    public interface IVolumeBalancer
    {
        float BlockAverageVolume { get; }

        void UpdateVolume(byte[] buffer, int offset, int count);
    }
}
