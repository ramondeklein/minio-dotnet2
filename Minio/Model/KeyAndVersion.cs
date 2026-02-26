namespace Minio.Model;

public readonly struct KeyAndVersion
{
    public readonly string Key { get; init; }
    public string? VersionId { get;  init;}
}