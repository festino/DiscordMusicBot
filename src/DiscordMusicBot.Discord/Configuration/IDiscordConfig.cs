namespace DiscordMusicBot.Discord.Configuration
{
    public interface IDiscordConfig
    {
        string DiscordToken { get; }
        string CommandPrefix { get; }
    }
}