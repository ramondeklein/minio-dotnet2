using System.Xml.Linq;

namespace Minio.Model;

public class ObjectLockConfiguration
{
    public bool Enabled { get; set; }
    public RetentionRule? DefaultRetentionRule { get; set; }

    public XElement Serialize()
    {
        var xConfig = new XElement(Constants.S3Ns + "ObjectLockConfiguration");
        if (Enabled)
            xConfig.Add(new XElement(Constants.S3Ns + "ObjectLockEnabled", "Enabled"));
        if (DefaultRetentionRule != null)
            xConfig.Add(new XElement(Constants.S3Ns + "Rule", DefaultRetentionRule.Serialize())); 
        return xConfig;
    }

    public static ObjectLockConfiguration Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var enabled = xElement.Element(Constants.S3Ns + "ObjectLockEnabled")?.Value == "Enabled";
        var xDefaultRetention = xElement.Element(Constants.S3Ns + "Rule")?.Element(Constants.S3Ns + "DefaultRetention");
        var defaultRetentionRule = xDefaultRetention != null ? RetentionRule.Deserialize(xDefaultRetention) : null;
        return new ObjectLockConfiguration
        {
            Enabled = enabled,
            DefaultRetentionRule = defaultRetentionRule,
        };
    }
}
