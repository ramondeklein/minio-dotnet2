namespace Minio.Model;

public readonly record struct DeleteResult(string Key, string? VersionId = null, bool? DeleteMarker = null, string? DeleteMarkerVersionId = null, string? ErrorCode = null, string? ErrorMessage = null);