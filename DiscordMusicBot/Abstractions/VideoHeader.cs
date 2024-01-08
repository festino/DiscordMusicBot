namespace DiscordMusicBot.Abstractions
{
    public record VideoHeader(string ChannelName, string Title, TimeSpan Duration,
                              bool Live, bool Copyright, string? ReasonUnplayable);
}
