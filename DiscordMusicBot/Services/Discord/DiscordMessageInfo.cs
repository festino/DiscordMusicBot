namespace DiscordMusicBot.Services.Discord
{
    public record DiscordMessageInfo(string RequesterName, ulong RequesterId, ulong GuildId, ulong ChannelId, ulong MessageId);
}
