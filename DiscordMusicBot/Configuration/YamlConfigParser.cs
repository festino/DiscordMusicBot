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
    public class YamlConfigParser : IConfigParser
    {
        private readonly IDeserializer _deserializer = new DeserializerBuilder()
                            .WithNamingConvention(HyphenatedNamingConvention.Instance)
                            .Build();

        public void Parse(IConfigStream configStream, List<ConfigProperty> properties)
        {
            string text = configStream.Read();
            dynamic content = _deserializer.Deserialize<ExpandoObject>(text) ?? new ExpandoObject();

            // TODO test if broken format

            bool isTextEdited = false;
            foreach (ConfigProperty property in properties)
            {
                string v = TryGetString(content, property.Key);
                property.Value = v;
                if (v is null)
                {
                    isTextEdited = true;
                    if (text.Length > 0 && text[^1] == '\n')
                        text += '\n';

                    text += $"{property.Key}: {property.DefaultValue}";
                }
            }

            if (isTextEdited)
            {
                configStream.Rewrite(text);
            }
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
