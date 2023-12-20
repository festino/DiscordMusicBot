using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Configuration
{
    public interface IConfigReader
    {
        void Read(List<ConfigProperty> properties);
    }
}
