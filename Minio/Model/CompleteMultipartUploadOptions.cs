namespace Minio.Model;

public class CompleteMultipartUploadOptions
{
    public ChecksumAlgorithm? ChecksumAlgorithm { get; set; }
    public byte[]? Checksum { get; set; }
}