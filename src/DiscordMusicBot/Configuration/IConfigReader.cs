namespace DiscordMusicBot.Configuration
{
    public interface IConfigReader
    {
        void Read(List<ConfigProperty> properties);
    }
}
