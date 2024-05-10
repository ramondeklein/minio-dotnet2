namespace Minio.Helpers;

internal sealed class ContentLengthStream : BaseStream
{
    public ContentLengthStream(Stream baseStream, long length) : base(baseStream)
    {
        Length = length;
    }

    public override long Length { get; }
}