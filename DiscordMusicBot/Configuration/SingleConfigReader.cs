namespace DiscordMusicBot.Configuration
{
    public class SingleConfigReader : IConfigReader
    {
        private readonly IConfigParser _parser;

        private readonly IConfigStream _stream;

        public SingleConfigReader(IConfigParser parser, IConfigStream stream)
        {
            _parser = parser;
            _stream = stream;
        }

        public void Read(List<ConfigProperty> properties)
        {
            _parser.Parse(_stream, properties);
        }
    }
}
