namespace DiscordMusicBot.Configuration.Parsing
{
    public interface IConfigParser
    {
        void Parse(IConfigStream configStream, List<ConfigProperty> properties);
    }
}
