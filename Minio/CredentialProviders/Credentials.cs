namespace Minio.CredentialProviders;

public readonly record struct Credentials(string AccessKey, string SecretKey, string SessionToken = "");