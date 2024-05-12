using System.Globalization;
using System.Xml.Linq;
using ArgumentNullException = System.ArgumentNullException;

namespace Minio.Model;

public abstract class RetentionRule
{
    protected RetentionRule(RetentionMode mode)
    {
        Mode = mode;
    }
    
    public RetentionMode Mode { get; }

    public abstract XElement Serialize();

    public static RetentionRule Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        if (xElement.Element(Constants.S3Ns + "Days") != null)
            return RetentionRuleDays.Deserialize(xElement);
        if (xElement.Element(Constants.S3Ns + "Years") != null)
            return RetentionRuleYears.Deserialize(xElement);
        throw new InvalidOperationException("No 'Days' or 'Years' element found.");
    }
}

public class RetentionRuleDays : RetentionRule
{
    public RetentionRuleDays(RetentionMode mode, int days) : base(mode)
    {
        Days = days;
    }

    public int Days { get; }

    public override XElement Serialize()
        => new XElement(Constants.S3Ns + "DefaultRetention",
            new XElement(Constants.S3Ns + "Mode", RetentionModeExtensions.Serialize(Mode)),
            new XElement(Constants.S3Ns + "Days", Days));

    public static RetentionRuleDays Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var mode = RetentionModeExtensions.Deserialize(xElement.Element(Constants.S3Ns + "Mode")?.Value ?? string.Empty);
        var days = int.Parse(xElement.Element(Constants.S3Ns + "Days")?.Value ?? string.Empty, CultureInfo.InvariantCulture);
        return new RetentionRuleDays(mode, days);
    }
}

public class RetentionRuleYears : RetentionRule
{
    public RetentionRuleYears(RetentionMode mode, int years) : base(mode)
    {
        Years = years;
    }

    public int Years { get; }

    public override XElement Serialize()
        => new XElement(Constants.S3Ns + "DefaultRetention",
            new XElement(Constants.S3Ns + "Mode", RetentionModeExtensions.Serialize(Mode)),
            new XElement(Constants.S3Ns + "Years", Years));

    public static RetentionRuleYears Deserialize(XElement xElement)
    {
        if (xElement == null) throw new ArgumentNullException(nameof(xElement));
        var mode = RetentionModeExtensions.Deserialize(xElement.Element(Constants.S3Ns + "Mode")?.Value ?? string.Empty);
        var years = int.Parse(xElement.Element(Constants.S3Ns + "Years")?.Value ?? string.Empty, CultureInfo.InvariantCulture);
        return new RetentionRuleYears(mode, years);
    }
}
