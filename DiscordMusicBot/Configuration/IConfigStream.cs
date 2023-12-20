using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Configuration
{
    public interface IConfigStream
    {

        string Read();

        void Rewrite(string configStr);
    }
}
