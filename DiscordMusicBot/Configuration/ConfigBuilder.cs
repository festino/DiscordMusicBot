using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Configuration
{
    public class ConfigBuilder
    {
        private enum ConfigPropertyName { CommandPrefix, DiscordToken, YoutubeToken }

        private readonly IConfigReader _reader;

        public ConfigBuilder(IConfigReader reader)
        {
            _reader = reader;
        }

        public Config Build()
        {
            Dictionary<ConfigPropertyName, ConfigProperty> properties = new()
            {
                {
                    ConfigPropertyName.CommandPrefix,
                    new ConfigProperty("command-prefix", "!")
                },
                {
                    ConfigPropertyName.DiscordToken,
                    new ConfigProperty("discord-token",
                        "ENTER-Y0UR-T0KEN-HERE.AzNA.G6SEGD.UDtpQJwJq9pRvoZvRRDJCF-hEpBT2yuvhJFuh4", false)
                },
                {
                    ConfigPropertyName.YoutubeToken,
                    new ConfigProperty("youtube-token",
                        "YourToken_5AqzV6B44r9CfU3cn2KBbZZkaDf9k", false)
                },
            };

            _reader.Read(properties.Values.ToList());

            Dictionary<ConfigPropertyName, string> propertyValues = new();
            string errorMessage = "";
            foreach ((ConfigPropertyName name, ConfigProperty property) in properties)
            {
                if (property.Value is null)
                {
                    errorMessage += $"Property \"{property.Key}\" is not set";
                    if (!property.AllowDefault)
                        errorMessage += $" or has the dafault value \"{property.DefaultValue}\"";

                    errorMessage += "\n";
                    continue;
                }

                propertyValues.Add(name, property.Value);
            }

            if (errorMessage.Length > 0)
            {
                Console.WriteLine(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            return new Config(
                CommandPrefix: propertyValues[ConfigPropertyName.CommandPrefix],
                DiscordToken: propertyValues[ConfigPropertyName.DiscordToken],
                YoutubeToken: propertyValues[ConfigPropertyName.YoutubeToken]
            );
        }
    }
}
