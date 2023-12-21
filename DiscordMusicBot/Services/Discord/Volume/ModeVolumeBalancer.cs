namespace DiscordMusicBot.Services.Discord.Volume
{
    public class ModeVolumeBalancer : IVolumeBalancer
    {
        private const int MaxSample = short.MaxValue;
        private const int BucketSize = 1 << 8;
        private const int BucketCount = MaxSample / BucketSize + 1;

        private long[] _buckets = new long[BucketCount];

        public float BlockAverageVolume { get; private set; }

        public void UpdateVolume(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < offset + count; i += 2)
            {
                short sample = (short)(buffer[i] | buffer[i + 1] << 8);
                int bucketIndex = Math.Abs(sample / BucketSize);
                _buckets[Math.Min(bucketIndex, _buckets.Length - 1)]++;
            }

            BlockAverageVolume = GetVolume();
            //Console.WriteLine(string.Join(", ", _buckets));
        }

        private float GetVolume()
        {
            int maxBucket = _buckets.ToList().IndexOf(_buckets.Max());
            return BucketSize * (maxBucket + 0.5f);
        }
    }
}
