namespace DiscordMusicBot.Youtube.Downloading
{
    public class CountingReadonlyStream : Stream
    {
        private readonly Stream _source;

        private long _count;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();
        public override long Position { get => _count; set => throw new NotImplementedException(); }

        public CountingReadonlyStream(Stream source, long startPosition)
        {
            _source = source;
            _count = startPosition;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int copyCount = _source.Read(buffer, offset, count);
            _count += copyCount;
            return copyCount;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int copyCount = await _source.ReadAsync(buffer, offset, count, cancellationToken);
            _count += copyCount;
            return copyCount;
        }

        public override void Flush()
        {
            _source.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public async Task SkipAsync(long count)
        {
            if (count < 0)
                throw new ArgumentException($"{nameof(CountingReadonlyStream)} is not able to skip backwards");

            if (count == 0)
                return;

            int bufferSize = 1024 * 1024;
            byte[] buffer = new byte[bufferSize];
            while (count > 0)
            {
                int countSkip = (int)Math.Min(bufferSize, count);
                countSkip = await ReadAsync(buffer, 0, countSkip);
                count -= countSkip;
            }
        }
    }
}
