using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Configuration
{
    public class FileConfigStream : IConfigStream
    {
        private readonly string _path;

        public FileConfigStream(string path)
        {
            _path = path;
        }

        public string Read()
        {
            if (!File.Exists(_path))
            {
                Console.WriteLine("Configuration file did not exist: " + Path.GetFullPath(_path));
                File.CreateText(_path).Close();
                return "";
            }

            return File.ReadAllText(_path);
        }

        public void Rewrite(string configStr)
        {
            File.WriteAllText(_path, configStr);
        }
    }
}
