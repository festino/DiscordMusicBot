namespace DiscordMusicBot.Abstractions
{
    public interface IAudioStreamer
    {
        ulong? GuildId { get; set; }

        bool IsConnected { get; }

        TimeSpan? GetCurrentTime();

        Task JoinAsync(Func<ulong[]> getRequesterIds, CancellationToken cancellationToken);
        Task LeaveAsync(int delayMs, CancellationToken cancellationToken);

        Task PlayAudioAsync(Stream pcmStream, Func<ulong[]> getRequesterIds, CancellationToken cancellationToken);
    }
}
