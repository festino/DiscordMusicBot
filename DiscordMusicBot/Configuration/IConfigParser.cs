using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Configuration
{
    public interface IConfigParser
    {
        void Parse(IConfigStream configStream, List<ConfigProperty> properties);
    }
}
