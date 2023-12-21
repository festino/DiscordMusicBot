namespace DiscordMusicBot.Services.Discord.Volume
{
    public class AverageVolumeBalancer : IVolumeBalancer
    {
        private int _sampleCount = 0;

        public float BlockAverageVolume { get; private set; }

        public void UpdateVolume(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < offset + count; i += 2)
            {
                short sample = (short)(buffer[i] | buffer[i + 1] << 8);

                float frac = 1.0f / (_sampleCount + 1);
                BlockAverageVolume = BlockAverageVolume * (_sampleCount * frac) + Math.Abs(sample * frac);
                _sampleCount++;
            }
        }
    }
}
