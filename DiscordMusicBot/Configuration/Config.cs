using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;

public class Config : IDiscordConfig, IYoutubeConfig
{
    public string DiscordToken { get; init; }
    public string YoutubeToken { get; init; }

    public Config(string configPath, string credentialsPath)
    {
        DiscordToken = "MTE4MTE3MTY4MzE0NzY0OTAzNA.G6SEGD.UDtpQJwJq9pRvoZvRRDJCF-hEpBT2yuvhJFuh4";
        YoutubeToken = "AIzaSyBY_5AqazV6B44r9CfU3cn2KBbZZkaDf9k";
    }
}