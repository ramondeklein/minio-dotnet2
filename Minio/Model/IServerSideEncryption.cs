using System.Net.Http.Headers;

namespace Minio.Model;

public interface IServerSideEncryption
{
    string Type { get; }
    void WriteHeaders(HttpHeaders headers);
}