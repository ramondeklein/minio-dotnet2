using System.Xml.Linq;

namespace Minio.Model;

public class ObjectLockConfiguration
{
    public RetentionRule? DefaultRetentionRule { get; set; }

    public XElement Serialize()
    {
        var xConfig = new XElement(Constants.S3Ns + "ObjectLockConfiguration", 
            new XElement(Constants.S3Ns + "ObjectLockEnabled", "Enabled"));
        if (DefaultRetentionRule != null)
            xConfig.Add(new XElement(Constants.S3Ns + "Rule", DefaultRetentionRule.Serialize())); 
        return xConfig;
    }

    public static ObjectLockConfiguration Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var xDefaultRetention = xElement.Element(Constants.S3Ns + "Rule")?.Element(Constants.S3Ns + "DefaultRetention");
        var defaultRetentionRule = xDefaultRetention != null ? RetentionRule.Deserialize(xDefaultRetention) : null;
        return new ObjectLockConfiguration
        {
            DefaultRetentionRule = defaultRetentionRule,
        };
    }
}
