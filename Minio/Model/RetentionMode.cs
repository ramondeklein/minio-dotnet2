namespace Minio.Model;

public enum RetentionMode
{
    Governance,
    Compliance,
}

internal static class RetentionModeExtensions
{
    public static string Serialize(RetentionMode retentionMode)
    {
        return retentionMode switch
        {
            RetentionMode.Compliance => "COMPLIANCE",
            RetentionMode.Governance => "GOVERNANCE",
            _ => throw new ArgumentException("Invalid object lock mode", nameof(retentionMode))
        };
    }

    public static RetentionMode Deserialize(string retentionMode)
    {
        return retentionMode switch
        {
            "COMPLIANCE" => RetentionMode.Compliance,
            "GOVERNANCE" => RetentionMode.Governance,
            _ => throw new ArgumentException("Invalid object lock mode", nameof(retentionMode))
        };
    }
}