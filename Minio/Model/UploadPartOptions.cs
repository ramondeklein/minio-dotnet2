namespace Minio.Model;

public class UploadPartOptions
{
    public ChecksumAlgorithm? ChecksumAlgorithm { get; set; }
    public byte[]? Checksum { get; set; }
    public byte[]? ContentMD5 { get; set; }
}