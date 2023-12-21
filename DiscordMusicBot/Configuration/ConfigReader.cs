namespace DiscordMusicBot.Configuration
{
    public class ConfigReader : IConfigReader
    {
        private const string CREDENTIALS_SIGN = "token";

        private readonly IConfigParser _parser;

        private readonly IConfigStream _mainStream;
        private readonly IConfigStream _credentialsStream;

        public ConfigReader(IConfigParser parser, IConfigStream mainStream, IConfigStream credentialsStream)
        {
            _parser = parser;
            _mainStream = mainStream;
            _credentialsStream = credentialsStream;
        }

        public void Read(List<ConfigProperty> properties)
        {
            var mainProperties = properties
                .Where(p => !p.Key.Contains(CREDENTIALS_SIGN))
                .ToList();
            var credentialProperties = properties
                .Where(p => p.Key.Contains(CREDENTIALS_SIGN))
                .ToList();

            _parser.Parse(_mainStream, mainProperties);
            _parser.Parse(_credentialsStream, credentialProperties);
        }
    }
}
