using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Minio.Model;

public class SSE : IServerSideEncryption
{
    // SseGenericHeader is the AWS SSE header used for SSE-S3 and SSE-KMS.
    private const string SseGenericHeader = "X-Amz-Server-Side-Encryption";

    // SseKmsKeyID is the AWS SSE-KMS key id.
    private const string SseKmsKeyID = SseGenericHeader + "-Aws-Kms-Key-Id";
    // SseEncryptionContext is the AWS SSE-KMS Encryption Context data.
    private const string SseEncryptionContext = SseGenericHeader + "-Context";

    // SseCustomerAlgorithm is the AWS SSE-C algorithm HTTP header key.
    private const string SseCustomerAlgorithm = SseGenericHeader + "-Customer-Algorithm";
    // SseCustomerKey is the AWS SSE-C encryption key HTTP header key.
    private const string SseCustomerKey = SseGenericHeader + "-Customer-Key";
    // SseCustomerKeyMD5 is the AWS SSE-C encryption key MD5 HTTP header key.
    private const string SseCustomerKeyMD5 = SseGenericHeader + "-Customer-Key-MD5";
    
    private readonly byte[] _key;

    public SSE(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != 32) throw new ArgumentException("key should have 32 bytes (256 bit)", nameof(key));

        _key = key;
    }
    
    public string Type => "SSE-C";

    public void WriteHeaders(HttpHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        headers.Add(SseCustomerAlgorithm, "AES256");
        headers.Add(SseCustomerKey, Convert.ToBase64String(_key, Base64FormattingOptions.None));
#pragma warning disable CA5351  // MD5 is required here
        headers.Add(SseCustomerKeyMD5, Convert.ToBase64String(MD5.HashData(_key), Base64FormattingOptions.None));
#pragma warning restore CA5351
    }
}