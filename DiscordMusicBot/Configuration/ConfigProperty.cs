using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Configuration
{
    public class ConfigProperty
    {

        private string? _value = null;

        public string Key { get; init; }

        public string? Value {
            get => _value;
            set {
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
            Key = key;
            DefaultValue = defaultValue;
            AllowDefault = allowDefault;
        }
    }
}
