namespace DiscordMusicBot.Configuration
{
    public interface IConfigParser
    {
        void Parse(IConfigStream configStream, List<ConfigProperty> properties);
    }
}
