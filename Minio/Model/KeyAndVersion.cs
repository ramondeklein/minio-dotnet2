namespace Minio.Model;

public readonly struct KeyAndVersion
{
    public string Key { get; }
    public string? VersionId { get; }

    public KeyAndVersion(string key)
    {
        Key = key;
    }
    public KeyAndVersion(string key, string versionId) : this(key)
    {
        VersionId = versionId;
    }
}