using System.Dynamic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DiscordMusicBot.Configuration
{
    public class YamlConfigParser : IConfigParser
    {
        private readonly ISerializer _serializer = new SerializerBuilder()
                            .WithNamingConvention(HyphenatedNamingConvention.Instance)
                            .Build();
        private readonly IDeserializer _deserializer = new DeserializerBuilder()
                            .WithNamingConvention(HyphenatedNamingConvention.Instance)
                            .Build();

        public void Parse(IConfigStream configStream, List<ConfigProperty> properties)
        {
            string text = configStream.Read();
            dynamic content = _deserializer.Deserialize<ExpandoObject>(text) ?? new ExpandoObject();

            List<ConfigProperty> missingProperties = new();
            foreach (ConfigProperty property in properties)
            {
                string v = TryGetString(content, property.Key);
                if (v is null)
                {
                    property.Value = property.DefaultValue;
                    missingProperties.Add(property);
                }
                else
                {
                    property.Value = Desanitize(v);
                }
            }

            if (missingProperties.Count > 0)
            {
                Dictionary<string, string> missingPairs = new();
                foreach (ConfigProperty property in missingProperties)
                {
                    missingPairs.Add(property.Key, Sanitize(property.DefaultValue));
                }

                if (text.Length > 0 && text[^1] != '\n')
                    text += '\n';

                configStream.Rewrite(text + _serializer.Serialize(missingPairs));
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

        private static string Sanitize(string value)
        {
            return value.Replace(@"\", @"\\").Replace("\n", @"\n");
        }

        private static string Desanitize(string value)
        {
            return value.Replace(@"\n", "\n").Replace(@"\\", @"\");
        }
    }
}
