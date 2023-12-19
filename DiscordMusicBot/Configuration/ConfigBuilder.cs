using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DiscordMusicBot.Configuration
{
    public class ConfigBuilder
    {
        private readonly IDeserializer _deserializer = new DeserializerBuilder()
                            .WithNamingConvention(HyphenatedNamingConvention.Instance)
                            .Build();

        private readonly string _configPath;
        private readonly string _credentialsPath;

        public ConfigBuilder(string configPath, string credentialsPath)
        {
            _configPath = configPath;
            _credentialsPath = credentialsPath;
        }

        public Config Build()
        {
            if (!File.Exists(_configPath))
            {
                Console.WriteLine("Configuration file did not exist: " + Path.GetFullPath(_configPath));
                // TODO log
                // TODO create file
                throw new FileNotFoundException("", _configPath);
            }

            if (!File.Exists(_credentialsPath))
            {
                Console.WriteLine("Credentials file did not exist: " + Path.GetFullPath(_credentialsPath));
                // TODO log
                // TODO create and fill file
                throw new FileNotFoundException("", _credentialsPath);
            }

            string? commandPrefix = null;
            string? discordToken = null;
            string? youtubeToken = null;

            dynamic configContent = _deserializer.Deserialize<ExpandoObject>(File.ReadAllText(_configPath));
            if (configContent is not null)
            {
                commandPrefix = TryGetString(configContent, "command-prefix");
            }

            dynamic credentialsContent = _deserializer.Deserialize<ExpandoObject>(File.ReadAllText(_credentialsPath));
            if (credentialsContent is not null)
            {
                discordToken = TryGetString(credentialsContent, "discord-token");
                youtubeToken = TryGetString(credentialsContent, "youtube-token");
            }

            if (commandPrefix is null || discordToken is null || youtubeToken is null)
            {
                // TODO log nice error message
                throw new ArgumentException("");
            }

            return new Config(commandPrefix, discordToken, youtubeToken);
        }

        private static string? TryGetString(dynamic settings, string name)
        {
            if (((IDictionary<string, object>)settings).TryGetValue(name, out object? property))
                return property is string ? (string)property : null;

            return null;
        }

        private static bool DoesPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }
    }
}
