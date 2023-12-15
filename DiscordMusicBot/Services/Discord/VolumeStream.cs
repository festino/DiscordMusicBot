using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Discord
{
    public class VolumeStream : Stream
    {
        private readonly AnonymousPipeServerStream pipeServer;
        private readonly AnonymousPipeClientStream pipeClient;

        private float _volume;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public VolumeStream()
        {
            pipeServer = new();
            pipeClient = new(pipeServer.GetClientHandleAsString());

            _volume = 1.0f;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            pipeServer.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ApplyVolume(buffer, offset, count);
            await pipeServer.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int countReal = await pipeClient.ReadAsync(buffer, offset, count, cancellationToken);
            return countReal;
        }

        private void ApplyVolume(byte[] buffer, int offset, int count)
        {
            if (count % 2 != 0)
                throw new InvalidOperationException($"{nameof(VolumeStream)} can not give odd bytes count");

            int minSample = short.MinValue;
            int maxSample = short.MaxValue;
            for (int i = offset; i < offset + count; i += 2)
            {
                short sample = (short) (buffer[i] | buffer[i + 1] << 8);
                int v = (int) (sample * _volume);
                sample = (short)Math.Clamp(v, minSample, maxSample);
                buffer[i] = (byte)sample;
                buffer[i + 1] = (byte) (sample >> 8);
            }
        }
    }
}
