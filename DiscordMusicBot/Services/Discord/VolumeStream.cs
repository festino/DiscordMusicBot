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
        /*private readonly byte[] _buffer;
        private int _length;

        private bool _stopped = false;

        private object _locker = new();
        private SemaphoreSlim _semaphoreWrite = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _semaphoreRead = new SemaphoreSlim(0, 1);*/

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
            /*_buffer = new byte[bufferSize];
            _length = 0;*/

            _volume = 1.0f;
        }


        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            /*while (count > 0)
            {
                if (_stopped)
                    throw new InvalidOperationException($"{nameof(VolumeStream)} was flushed and cannot be written");

                if (_length == _buffer.Length)
                {
                    Console.WriteLine("Waiting write");
                    await _semaphoreWrite.WaitAsync();
                }

                lock (_locker)
                {

                    int copyCount = Math.Min(count, _buffer.Length - _length);
                    Array.Copy(buffer, offset, _buffer, 0, copyCount);
                    count -= copyCount;
                    int prevLength = _length;
                    _length += copyCount;

                    if (prevLength == 0)
                    {
                        Console.WriteLine("Releasing read");
                        _semaphoreRead.Release();
                    }
                }
            }*/
            ApplyVolume(buffer, offset, count);
            await pipeServer.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            /*if (_length == 0)
            {
                Console.WriteLine("Waiting read");
                await _semaphoreRead.WaitAsync();
            }

            if (_stopped)
                return 0;

            int copyCount;
            lock (_locker)
            {
                copyCount = Math.Min(count, _length);
                ApplyVolume(_buffer, 0, copyCount);
                Array.Copy(_buffer, 0, buffer, offset, copyCount);
                int prevLength = _length;
                _length -= copyCount;
                // TODO replace shift with wrapping
                ShiftLeft(copyCount, _length);

                if (prevLength == _buffer.Length)
                {
                    Console.WriteLine("Releasing write");
                    _semaphoreWrite.Release();
                }
            }

            return copyCount;*/
            
            int countReal = await pipeClient.ReadAsync(buffer, offset, count, cancellationToken);
            return countReal;
        }

        public override void Flush()
        {
            /*Console.WriteLine("Stopping");
            _stopped = true;
            _semaphoreWrite.Release();
            _semaphoreRead.Release();*/
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

        /*private void ShiftLeft(int offset, int count)
        {
            //Array.Copy(_buffer, offset, _buffer, 0, count);
            for (int i = 0; i < count; i++)
            {
                _buffer[i] = _buffer[offset + i];
            }
        }*/

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

        public override int Read(byte[] buffer, int offset, int count)
        {
            //return ReadAsync(buffer, offset, count).Result;
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            //ReadAsync(buffer, offset, count).Wait();
            throw new NotImplementedException();
        }
    }
}
