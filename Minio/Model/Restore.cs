namespace Minio.Model;

public readonly record struct Restore(bool OngoingRestore, DateTimeOffset? ExpiryTime = null);