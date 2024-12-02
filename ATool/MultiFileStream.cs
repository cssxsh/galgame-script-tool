using System;
using System.IO;
using System.Linq;

namespace ATool
{
    public class MultiFileStream : Stream
    {
        private readonly FileStream[] _sources;

        private int _current;

        private long _pos;

        public MultiFileStream(params FileStream[] sources)
        {
            _sources = sources;
            _pos = _sources[_current].Position;
        }

        public override void Flush()
        {
            foreach (var source in _sources) source.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0L || offset > Length) throw new IOException("Invalid offset");
                    _pos = offset;
                    break;
                case SeekOrigin.Current:
                    if (_pos + offset < 0L || _pos + offset > Length) throw new IOException("Invalid offset");
                    _pos += offset;
                    break;
                case SeekOrigin.End:
                    if (offset > 0L || Length + offset < 0) throw new IOException("Invalid offset");
                    _pos = Length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            offset = _pos;
            for (var i = 0; i < _sources.Length; i++)
            {
                _sources[i].Seek(Math.Min(offset, _sources[i].Length), SeekOrigin.Begin);
                _current = i;
                if (_sources[_current].Length > offset) break;
                offset -= _sources[_current].Length;
            }

            return _pos;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("MultiFileStream::SetLength is unsupported.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length) throw new ArgumentException("Invalid offset");
            if (_pos + count > Length) throw new ArgumentException("Invalid count");
            var total = 0;
            do
            {
                var used = _sources[_current].Read(buffer, offset + total, count - total);
                total += used;
                if (_sources[_current].Position == _sources[_current].Length) _current++;
            } while (count != total && _current < _sources.Length);

            _pos += total;
            return total;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length) throw new ArgumentException("Invalid offset");
            if (_pos + count > Length) throw new ArgumentException("Invalid count");
            var total = 0;
            do
            {
                var used = Math.Min(count - total, (int)(_sources[_current].Length - _sources[_current].Position));
                _sources[_current].Write(buffer, offset + total, used);
                total += used;
                if (_sources[_current].Position == _sources[_current].Length) _current++;
            } while (count != total && _current < _sources.Length);

            _pos += total;
        }

        public override void Close()
        {
            foreach (var source in _sources) source.Close();
        }

        public override bool CanRead => _sources.All(sources => sources.CanRead);
        public override bool CanSeek => _sources.All(sources => sources.CanSeek);
        public override bool CanWrite => _sources.All(sources => sources.CanWrite);
        public override long Length => _sources.Sum(sources => sources.Length);

        public override long Position
        {
            get => _pos;
            set => Seek(value, SeekOrigin.Begin);
        }
    }
}