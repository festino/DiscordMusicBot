using System.Text.RegularExpressions;

namespace DiscordMusicBot.Configuration
{
    public class ConfigProperty
    {

        private string? _value = null;

        public string Key { get; init; }

        public string? Value
        {
            get => _value;
            set
            {
                if (!AllowDefault && value == DefaultValue)
                    _value = null;
                else
                    _value = value;
            }
        }

        public string DefaultValue { get; init; }

        public bool AllowDefault { get; init; }

        public ConfigProperty(string key, string defaultValue, bool allowDefault = true)
        {
            Key = PascalToKebabCase(key);
            DefaultValue = defaultValue;
            AllowDefault = allowDefault;
        }

        public ConfigProperty(object key, string defaultValue, bool allowDefault = true)
        {
            Key = PascalToKebabCase(key.ToString() ?? "null");
            DefaultValue = defaultValue;
            AllowDefault = allowDefault;
        }

        private static string PascalToKebabCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(
                value,
                "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])",
                "-$1",
                RegexOptions.Compiled)
                .Trim()
                .ToLower();
        }
    }
}
