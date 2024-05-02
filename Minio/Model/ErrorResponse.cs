using System.Xml.Serialization;

namespace Minio.Model;

[XmlRoot("Error")]
public class ErrorResponse
{
    [XmlElement("Code")]
    public string Code { get; set; } = string.Empty;
    
    [XmlElement("Message")]
    public string Message { get; set; } = string.Empty;
    
    [XmlElement("BucketName")]
    public string BucketName { get; set; } = string.Empty;

    [XmlElement("Key")]
    public string Key { get; set; } = string.Empty;
    
    [XmlElement("Resource")]
    public string Resource { get; set; } = string.Empty;
    
    [XmlElement("RequestId")]
    public string RequestId { get; set; } = string.Empty;
    
    [XmlElement("HostId")]
    public string HostId { get; set; } = string.Empty;

    [XmlElement("Region")]
    public string Region { get; set; } = string.Empty;

    [XmlElement("Server")]
    public string Server { get; set; } = string.Empty;
}