namespace DiscordMusicBot.Services.Discord.Volume
{
    public interface IVolumeBalancer
    {
        float BlockAverageVolume { get; }

        void UpdateVolume(byte[] buffer, int offset, int count);
    }
}
