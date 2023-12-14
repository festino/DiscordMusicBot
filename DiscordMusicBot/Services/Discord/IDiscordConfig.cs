namespace DiscordMusicBot.Services.Discord
{
    public interface IDiscordConfig
    {
        string DiscordToken { get; }
        string CommandPrefix { get; }
    }
}