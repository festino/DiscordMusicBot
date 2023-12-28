namespace DiscordMusicBot.Configuration
{
    public static class LangConfig
    {
        private enum PropertyName
        {
            CommandHelp,
            CommandUnknown,
            CommandListNoVideos,
            CommandListTemplate,
            CommandPlayNoArgument,
            CommandPlaySearchNoOptions,
            CommandPlaySearchOptions,
            CommandPlayBadArgs,
            CommandPlayBadIds,
            CommandPlayAddOne,
            CommandPlayAddMany,
            CommandSkipNoVideos,
            CommandSkipOne,
            CommandSkipMany,
            CommandStopNoVideos,
            CommandStopMany,

            AudioLoadError,
            QueueIsEmpty,
            JoiningVoiceChannel,
            Loading,
            LoadingAudio,
            PlayingAudio,
        }

        public static string CommandHelp { get; private set; }
        public static string CommandUnknown { get; private set; }
        public static string CommandListNoVideos { get; private set; }
        public static string CommandListTemplate { get; private set; }
        public static string CommandPlayNoArgument { get; private set; }
        public static string CommandPlaySearchNoOptions { get; private set; }
        public static string CommandPlaySearchOptions { get; private set; }
        public static string CommandPlayBadArgs { get; private set; }
        public static string CommandPlayBadIds { get; private set; }
        public static string CommandPlayAddOne { get; private set; }
        public static string CommandPlayAddMany { get; private set; }
        public static string CommandSkipNoVideos { get; private set; }
        public static string CommandSkipOne { get; private set; }
        public static string CommandSkipMany { get; private set; }
        public static string CommandStopNoVideos { get; private set; }
        public static string CommandStopMany { get; private set; }

        public static string AudioLoadError { get; private set; }
        public static string QueueIsEmpty { get; private set; }
        public static string JoiningVoiceChannel { get; private set; }
        public static string Loading { get; private set; }
        public static string LoadingAudio { get; private set; }
        public static string PlayingAudio { get; private set; }

        static LangConfig()
        {
            SingleConfigReader reader = new(new YamlConfigParser(), new FileConfigStream("lang.yml"));
            List<ConfigProperty> propertyValues = LoadValues(reader);

            CommandHelp = Get(propertyValues, PropertyName.CommandHelp);
            CommandUnknown = Get(propertyValues, PropertyName.CommandUnknown);
            CommandSkipNoVideos = Get(propertyValues, PropertyName.CommandSkipNoVideos);
            CommandSkipOne = Get(propertyValues, PropertyName.CommandSkipOne);
            CommandSkipMany = Get(propertyValues, PropertyName.CommandSkipMany);
            CommandStopNoVideos = Get(propertyValues, PropertyName.CommandStopNoVideos);
            CommandStopMany = Get(propertyValues, PropertyName.CommandStopMany);
            CommandListNoVideos = Get(propertyValues, PropertyName.CommandListNoVideos);
            CommandListTemplate = Get(propertyValues, PropertyName.CommandListTemplate);
            CommandPlayNoArgument = Get(propertyValues, PropertyName.CommandPlayNoArgument);
            CommandPlaySearchNoOptions = Get(propertyValues, PropertyName.CommandPlaySearchNoOptions);
            CommandPlaySearchOptions = Get(propertyValues, PropertyName.CommandPlaySearchOptions);
            CommandPlayBadArgs = Get(propertyValues, PropertyName.CommandPlayBadArgs);
            CommandPlayBadIds = Get(propertyValues, PropertyName.CommandPlayBadIds);
            CommandPlayAddOne = Get(propertyValues, PropertyName.CommandPlayAddOne);
            CommandPlayAddMany = Get(propertyValues, PropertyName.CommandPlayAddMany);

            AudioLoadError = Get(propertyValues, PropertyName.AudioLoadError);
            QueueIsEmpty = Get(propertyValues, PropertyName.QueueIsEmpty);
            JoiningVoiceChannel = Get(propertyValues, PropertyName.JoiningVoiceChannel);
            Loading = Get(propertyValues, PropertyName.Loading);
            LoadingAudio = Get(propertyValues, PropertyName.LoadingAudio);
            PlayingAudio = Get(propertyValues, PropertyName.PlayingAudio);
        }

        private static List<ConfigProperty> LoadValues(IConfigReader reader)
        {
            List<ConfigProperty> properties = new()
            {
                new(PropertyName.CommandHelp, "Available commands:\nhelp, play, skip, undo, stop, list, now"),
                new(PropertyName.CommandUnknown, "Unknown command! Use `help` command"),
                new(PropertyName.CommandListNoVideos, "Queue is empty"),
                new(PropertyName.CommandListTemplate, "{0} songs, {1}\n{2}"),
                new(PropertyName.CommandPlayNoArgument, "I can't get it!\nTry `play youtu.be/dQw4w9WgXcQ` or query `play gangnam style`"),
                new(PropertyName.CommandPlaySearchNoOptions, "No search results :("),
                new(PropertyName.CommandPlaySearchOptions, "Choose:\n{0}"),
                new(PropertyName.CommandPlayBadArgs, "Could not get youtube ids from:\n{0}"),
                new(PropertyName.CommandPlayBadIds, "Could not load videos:\n{0}"),
                new(PropertyName.CommandPlayAddOne, "Added song {0}"),
                new(PropertyName.CommandPlayAddMany, "Added {0} songs\n{1}"),
                new(PropertyName.CommandSkipNoVideos, "Could not skip video"),
                new(PropertyName.CommandSkipOne, "Skipped {0}"),
                new(PropertyName.CommandSkipMany, "Skipped {0} videos:\n{1}"),
                new(PropertyName.CommandStopNoVideos, "Queue is already empty"),
                new(PropertyName.CommandStopMany, "Drop queue\n{0}"),
                new(PropertyName.AudioLoadError, "Could not load {0}"),
                new(PropertyName.QueueIsEmpty, "No videos left :skull:"),
                new(PropertyName.JoiningVoiceChannel, "Joining voice channel{0}"),
                new(PropertyName.Loading, "Loading{0}"),
                new(PropertyName.LoadingAudio, "Loading{0}\n{1} {2}"),
                new(PropertyName.PlayingAudio, "Playing{0}\n{1} {2}"),
            };

            reader.Read(properties);

            string errorMessage = "";
            foreach (ConfigProperty property in properties)
            {
                if (property.Value is null)
                {
                    errorMessage += $"Property \"{property.Key}\" is not set";
                    if (!property.AllowDefault)
                        errorMessage += $" or has the dafault value \"{property.DefaultValue}\"";

                    errorMessage += "\n";
                    continue;
                }
            }

            if (errorMessage.Length > 0)
            {
                Console.WriteLine(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            return properties;
        }

        private static string Get(List<ConfigProperty> properties, PropertyName name)
        {
            string key = new ConfigProperty(name, "").Key;
            ConfigProperty? property = properties.Find(p => p.Key == key);
            if (property is null || property.Value is null)
                throw new NullReferenceException($"Unexpected null property error: {name}");

            return property.Value;
        }
    }
}
