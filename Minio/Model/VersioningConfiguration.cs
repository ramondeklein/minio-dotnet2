using System.Xml.Linq;

namespace Minio.Model;

public class VersioningConfiguration
{
    private static readonly XNamespace Ns = Constants.S3Ns;
    
    public VersioningStatus Status { get; init; }
    public bool MfaDelete { get; set; }

    
    public XElement Serialize()
    {
        return new XElement(Ns + "VersioningConfiguration",
            new XElement(Ns + "Status", VersioningStatusExtensions.Serialize(Status)),
            new XElement(Ns + "MfaDelete", MfaDelete ? "Enabled" : "Disabled"));
    }
    
    public static VersioningConfiguration Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var status = VersioningStatusExtensions.Deserialize(xElement.Element(Constants.S3Ns + "Status")?.Value ?? string.Empty);
        var mfaDelete = xElement.Element(Constants.S3Ns + "Status")?.Value is "Enabled";
        return new VersioningConfiguration
        {
            Status = status,
            MfaDelete = mfaDelete,
        };
    }
}