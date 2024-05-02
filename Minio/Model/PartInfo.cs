namespace Minio.Model;

public class PartInfo
{
    public ChecksumAlgorithm? ChecksumAlgorithm { get; set; }
    public byte[]? Checksum { get; set; }
    public string Etag { get; set; }
}