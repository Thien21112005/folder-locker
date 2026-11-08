using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;

namespace FolderLocker.Models.Crypto
{
    /// <summary>
    /// Định dạng chunk dùng chung cho writer/reader:
    /// mỗi chunk = ciphertext (≤ chunkSize byte) + tag 16 byte.
    /// Mọi chunk trừ chunk cuối có đúng chunkSize byte plaintext; chunk cuối luôn &lt; chunkSize (có thể 0).
    /// Nonce = noncePrefix (4) || chỉ số chunk (8, big-endian) → không thể đảo thứ tự chunk.
    /// AAD = header bất biến || cờ chunk cuối (1) → không thể cắt cụt hoặc nối thêm.
    /// </summary>
    internal static class AesGcmChunk
    {
        public const int TagSize = 16;
        public const int NonceSize = 12;
        public const int NoncePrefixSize = 4;

        public static void BuildNonce(Span<byte> nonce, ReadOnlySpan<byte> prefix, ulong index)
        {
            prefix.CopyTo(nonce);
            BinaryPrimitives.WriteUInt64BigEndian(nonce[NoncePrefixSize..], index);
        }

        public static byte[] BuildAad(byte[] headerAad, bool final)
        {
            var aad = new byte[headerAad.Length + 1];
            headerAad.CopyTo(aad, 0);
            aad[^1] = final ? (byte)1 : (byte)0;
            return aad;
        }
    }

    /// <summary>Stream chỉ-ghi: mã hóa dữ liệu thành các chunk AES-256-GCM. Dispose sẽ ghi chunk cuối.</summary>
    internal sealed class AesGcmChunkWriter : Stream
    {
        private readonly Stream _inner;
        private readonly AesGcm _gcm;
        private readonly byte[] _aadNormal;
        private readonly byte[] _aadFinal;
        private readonly byte[] _noncePrefix;
        private readonly byte[] _buffer;
        private readonly byte[] _cipher;
        private readonly byte[] _nonce = new byte[AesGcmChunk.NonceSize];
        private readonly byte[] _tag = new byte[AesGcmChunk.TagSize];
        private int _length;
        private ulong _index;
        private bool _finished;

        public AesGcmChunkWriter(Stream inner, byte[] key, byte[] headerAad, byte[] noncePrefix, int chunkSize)
        {
            _inner = inner;
            _gcm = new AesGcm(key, AesGcmChunk.TagSize);
            _aadNormal = AesGcmChunk.BuildAad(headerAad, false);
            _aadFinal = AesGcmChunk.BuildAad(headerAad, true);
            _noncePrefix = noncePrefix;
            _buffer = new byte[chunkSize];
            _cipher = new byte[chunkSize];
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> data)
        {
            if (_finished) throw new ObjectDisposedException(nameof(AesGcmChunkWriter));
            while (data.Length > 0)
            {
                int n = Math.Min(_buffer.Length - _length, data.Length);
                data[..n].CopyTo(_buffer.AsSpan(_length));
                _length += n;
                data = data[n..];
                // Chunk đầy luôn là chunk "không phải cuối" (chunk cuối phải < chunkSize)
                if (_length == _buffer.Length) EmitChunk(final: false);
            }
        }

        private void EmitChunk(bool final)
        {
            AesGcmChunk.BuildNonce(_nonce, _noncePrefix, _index);
            _gcm.Encrypt(_nonce, _buffer.AsSpan(0, _length), _cipher.AsSpan(0, _length), _tag, final ? _aadFinal : _aadNormal);
            _inner.Write(_cipher, 0, _length);
            _inner.Write(_tag);
            _index++;
            _length = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_finished)
            {
                try
                {
                    EmitChunk(final: true);
                }
                finally
                {
                    _finished = true;
                    _gcm.Dispose();
                    CryptographicOperations.ZeroMemory(_buffer);
                }
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !_finished;
        public override void Flush() { }
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    /// <summary>
    /// Stream chỉ-đọc: giải mã + xác thực từng chunk. Ném <see cref="CryptographicException"/>
    /// nếu dữ liệu bị sửa, đảo thứ tự hoặc bị cắt cụt. Chỉ trả về 0 (EOF) sau khi chunk cuối hợp lệ.
    /// </summary>
    internal sealed class AesGcmChunkReader : Stream
    {
        private readonly Stream _inner;
        private readonly AesGcm _gcm;
        private readonly byte[] _aadNormal;
        private readonly byte[] _aadFinal;
        private readonly byte[] _noncePrefix;
        private readonly byte[] _cipher;
        private readonly byte[] _plain;
        private readonly byte[] _nonce = new byte[AesGcmChunk.NonceSize];
        private int _plainLength;
        private int _plainPosition;
        private ulong _index;
        private bool _done;

        public AesGcmChunkReader(Stream inner, byte[] key, byte[] headerAad, byte[] noncePrefix, int chunkSize)
        {
            _inner = inner;
            _gcm = new AesGcm(key, AesGcmChunk.TagSize);
            _aadNormal = AesGcmChunk.BuildAad(headerAad, false);
            _aadFinal = AesGcmChunk.BuildAad(headerAad, true);
            _noncePrefix = noncePrefix;
            _cipher = new byte[chunkSize + AesGcmChunk.TagSize];
            _plain = new byte[chunkSize];
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> destination)
        {
            if (destination.Length == 0) return 0;
            while (_plainPosition == _plainLength)
            {
                if (_done) return 0;
                LoadNextChunk();
            }

            int n = Math.Min(destination.Length, _plainLength - _plainPosition);
            _plain.AsSpan(_plainPosition, n).CopyTo(destination);
            _plainPosition += n;
            return n;
        }

        private void LoadNextChunk()
        {
            int got = _inner.ReadAtLeast(_cipher, _cipher.Length, throwOnEndOfStream: false);
            bool final = got < _cipher.Length;
            if (got < AesGcmChunk.TagSize)
                throw new CryptographicException("Dữ liệu mã hóa bị cắt cụt.");

            int cipherLength = got - AesGcmChunk.TagSize;
            AesGcmChunk.BuildNonce(_nonce, _noncePrefix, _index);
            _gcm.Decrypt(_nonce,
                _cipher.AsSpan(0, cipherLength),
                _cipher.AsSpan(cipherLength, AesGcmChunk.TagSize),
                _plain.AsSpan(0, cipherLength),
                final ? _aadFinal : _aadNormal);

            _plainLength = cipherLength;
            _plainPosition = 0;
            _index++;
            _done = final;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _gcm.Dispose();
                CryptographicOperations.ZeroMemory(_plain);
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override void Flush() { }
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
