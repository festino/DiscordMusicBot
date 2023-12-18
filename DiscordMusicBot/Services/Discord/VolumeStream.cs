﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Discord
{
    public class VolumeStream : Stream
    {
        private readonly Stream _source;
        private readonly int _channelCount;

        private float _volume;

        private int _sampleCount = 0;
        private float _avgVolume = 0.0f;

        public float? AverageVolume { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public VolumeStream(Stream source, float? averageVolume)
        {
            _source = source;
            _volume = 0.25f;
            _channelCount = 2;
            AverageVolume = averageVolume;
            if (averageVolume is not null)
            {
                _sampleCount = int.MaxValue;
                _avgVolume = (float)averageVolume;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _source.ReadAsync(buffer, offset, count).Result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count % 2 != 0)
                throw new InvalidOperationException($"{nameof(VolumeStream)} was expecting 16-bit numbers");

            int copyCount = await _source.ReadAsync(buffer, offset, count, cancellationToken);
            if (AverageVolume is null)
            {
                UpdateVolume(buffer, offset, copyCount);
                if (copyCount == 0)
                {
                    AverageVolume = _avgVolume;
                }
            }
            ApplyVolume(buffer, offset, copyCount);

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

        private void UpdateVolume(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < offset + count; i += 2)
            {
                short sample = (short)(buffer[i] | buffer[i + 1] << 8);

                float frac = 1.0f / (_sampleCount + 1);
                _avgVolume = _avgVolume * (_sampleCount * frac) + Math.Abs(sample * frac);
                _sampleCount++;
            }
        }

        private void ApplyVolume(byte[] buffer, int offset, int count)
        {
            float targetAvgVolume = 1 << 13;
            float minAvgMult = 0.1f;
            float blockVolumeMult = 1.0f / Math.Max(minAvgMult, _avgVolume / targetAvgVolume);

            int initSampleCount = 48100 * 5 * _channelCount;
            float power = Math.Max(0.0f, (initSampleCount - _sampleCount) / (float)initSampleCount);
            blockVolumeMult = 1.0f * power + blockVolumeMult * (1.0f - power);
            blockVolumeMult *= _volume;

            int minSample = short.MinValue;
            int maxSample = short.MaxValue;
            for (int i = offset; i < offset + count; i += 2)
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
