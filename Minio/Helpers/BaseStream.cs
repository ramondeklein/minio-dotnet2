namespace Minio.Helpers;

internal abstract class BaseStream : Stream
{
    private readonly Stream _baseStream;

    internal BaseStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _baseStream.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _baseStream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
        => _baseStream.FlushAsync(cancellationToken);
    
    public override void Flush() 
        => _baseStream.Flush();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _baseStream.ReadAsync(buffer, cancellationToken);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        =>  _baseStream.ReadAsync(buffer, offset, count, cancellationToken);

    public override int Read(Span<byte> buffer)
        =>  _baseStream.Read(buffer);

    public override int Read(byte[] buffer, int offset, int count)
        => _baseStream.Read(buffer, offset, count);

    public override int ReadByte()
        => _baseStream.ReadByte();

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        =>  _baseStream.BeginRead(buffer, offset, count, callback, state);

    public override int EndRead(IAsyncResult asyncResult)
        => _baseStream.EndRead(asyncResult);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        => _baseStream.WriteAsync(buffer, cancellationToken);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _baseStream.WriteAsync(buffer, offset, count, cancellationToken);

    public override void Write(ReadOnlySpan<byte> buffer)
        => _baseStream.Write(buffer);

    public override void Write(byte[] buffer, int offset, int count)
        => _baseStream.Write(buffer, offset, count);

    public override void WriteByte(byte value)
        => _baseStream.WriteByte(value);

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => _baseStream.BeginWrite(buffer, offset, count, callback, state);

    public override void EndWrite(IAsyncResult asyncResult)
        => _baseStream.EndWrite(asyncResult);

    public override void Close()
        => _baseStream.Close();

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => _baseStream.CanWrite;
    public override bool CanTimeout => _baseStream.CanTimeout;

    public override long Length => _baseStream.Length;
    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public override void SetLength(long value)
        => _baseStream.SetLength(value);

    public override long Seek(long offset, SeekOrigin origin)
        => _baseStream.Seek(offset, origin);

    public override int ReadTimeout
    {
        get => _baseStream.ReadTimeout;
        set => _baseStream.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => _baseStream.WriteTimeout;
        set => _baseStream.WriteTimeout = value;
    }

    public override void CopyTo(Stream destination, int bufferSize)
        => _baseStream.CopyTo(destination, bufferSize);

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        => _baseStream.CopyToAsync(destination, bufferSize, cancellationToken);

    public override bool Equals(object? obj) => _baseStream.Equals(obj);
    public override string ToString() => _baseStream.ToString()!;
    public override int GetHashCode()=> _baseStream.GetHashCode();
}