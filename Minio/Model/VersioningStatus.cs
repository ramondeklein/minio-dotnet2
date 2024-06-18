namespace Minio.Model;

public enum VersioningStatus
{
    Off = 0,
    Enabled = 1,
    Suspended = 2
}

internal static class VersioningStatusExtensions
{
    public static string Serialize(VersioningStatus versioningStatus)
    {
        return versioningStatus switch
        {
            VersioningStatus.Enabled => "Enabled",
            VersioningStatus.Suspended => "Suspended",
            _ => throw new ArgumentException("Invalid versioning status", nameof(versioningStatus))
        };
    }

    public static VersioningStatus Deserialize(string versioningStatus)
    {
        return versioningStatus switch
        {
            "Enabled" => VersioningStatus.Enabled,
            "Suspended" => VersioningStatus.Suspended,
            _ => throw new ArgumentException("Invalid versioning status", nameof(versioningStatus))
        };
    }
}