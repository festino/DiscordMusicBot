﻿namespace DiscordMusicBot.Services.Discord.Volume
{
    public class VolumeStream : Stream
    {
        private readonly IVolumeBalancer _volumeBalancer;
        private readonly Stream _source;

        private float _volume;

        private const int BytesPerSample = sizeof(short);
        private const int ChannelCount = 2;
        private const int SamplesPerSecond = 48100 * ChannelCount;
        private const int InitSampleCount = 5 * SamplesPerSecond;
        private int _initSampleCount = InitSampleCount;

        private long _bytesRead = 0;

        public float? AverageVolume { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TimeSpan TimeRead => TimeSpan.FromSeconds(_bytesRead / (double)(BytesPerSample * SamplesPerSecond));

        public VolumeStream(IVolumeBalancer volumeBalancer, float? averageVolume, Stream source)
        {
            _volumeBalancer = volumeBalancer;
            _source = source;
            _volume = 0.25f;
            AverageVolume = averageVolume;
            if (AverageVolume is not null)
            {
                _initSampleCount = 0;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _source.ReadAsync(buffer, offset, count).Result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count % BytesPerSample != 0)
                throw new InvalidOperationException($"{nameof(VolumeStream)} was expecting 16-bit numbers");

            int copyCount = await _source.ReadAsync(buffer, offset, count, cancellationToken);
            if (copyCount == 0)
            {
                AverageVolume = _volumeBalancer.BlockAverageVolume;
                return copyCount;
            }

            if (AverageVolume is null)
            {
                _volumeBalancer.UpdateVolume(buffer, offset, copyCount);
            }
            ApplyVolume(buffer, offset, copyCount, AverageVolume ?? _volumeBalancer.BlockAverageVolume);

            _bytesRead += copyCount;
            return copyCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            _source.Flush();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        private void ApplyVolume(byte[] buffer, int offset, int count, float avgVolume)
        {
            float targetAvgVolume = 1 << 13;
            float minAvgMult = 0.1f;
            float blockVolumeMult = 1.0f / Math.Max(minAvgMult, avgVolume / targetAvgVolume);

            float power = Math.Max(0.0f, _initSampleCount / (float)InitSampleCount);
            blockVolumeMult = 1.0f * (1.0f - power) + blockVolumeMult * power;
            blockVolumeMult *= _volume;

            int minSample = short.MinValue;
            int maxSample = short.MaxValue;
            for (int i = offset; i < offset + count; i += BytesPerSample)
            {
                short sample = (short)(buffer[i] | buffer[i + 1] << 8);
                int v = (int)(sample * blockVolumeMult);
                sample = (short)Math.Clamp(v, minSample, maxSample);
                buffer[i] = (byte)sample;
                buffer[i + 1] = (byte)(sample >> 8);
            }
        }
    }
}
