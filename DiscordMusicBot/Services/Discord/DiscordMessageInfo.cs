namespace DiscordMusicBot.Services.Discord
{
    public record DiscordMessageInfo(ulong RequesterId, ulong GuildId, ulong ChannelId, ulong MessageId);
}
