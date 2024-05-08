using System.Xml.Linq;

namespace Minio.Model;

public sealed class BucketNotification
{
    public IList<LambdaConfig> LambdaConfigs { get; } = new List<LambdaConfig>();
    public IList<TopicConfig> TopicConfigs { get; } = new List<TopicConfig>();
    public IList<QueueConfig> QueueConfigs { get; } = new List<QueueConfig>();

    public XElement Serialize()
    {
        return new XElement(Constants.S3Ns + "NotificationConfiguration",
            LambdaConfigs.Select(c => c.Serialize()),
            TopicConfigs.Select(c => c.Serialize()),
            QueueConfigs.Select(c => c.Serialize()));
    }

    public static BucketNotification Deserialize(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (xElement.Name != Constants.S3Ns + "NotificationConfiguration")
            throw new InvalidOperationException("Invalid XML element encountered");

        var bucketNotification = new BucketNotification();
        foreach (var xConfig in xElement.Elements().Where(x => x.Name.NamespaceName == Constants.S3Ns))
        {
            switch (xConfig.Name.LocalName)
            {
                case "CloudFunctionConfiguration":
                    bucketNotification.LambdaConfigs.Add(LambdaConfig.Deserialize(xConfig));
                    break;
                case "QueueConfiguration":
                    bucketNotification.QueueConfigs.Add(QueueConfig.Deserialize(xConfig));
                    break;
                case "TopicConfiguration":
                    bucketNotification.TopicConfigs.Add(TopicConfig.Deserialize(xConfig));
                    break;
            }
        }
        return bucketNotification;
    }
}

public abstract class NotificationConfiguration
{
    public string Id { get; set; } = string.Empty;
    public IList<EventType> Events { get; } = new List<EventType>();
    public IDictionary<string, string> Filter { get; } = new Dictionary<string, string>();

    protected void SerializeInner(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (!string.IsNullOrEmpty(Id))
            xElement.Add(new XElement(Constants.S3Ns + "Id", Id));
        foreach (var evt in Events)
            xElement.Add(new XElement(Constants.S3Ns + "Event", evt.ToString()));

        if (Filter.Count > 0)
            xElement.Add(new XElement(Constants.S3Ns + "Filter",
                new XElement(Constants.S3Ns + "S3Key",
                    Filter.Select(kv => new XElement(Constants.S3Ns + "FilterRule",
                        new XElement(Constants.S3Ns + "Name", kv.Key),
                        new XElement(Constants.S3Ns + "Value", kv.Value))))));
    }

    protected void DeserializeInner(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        Id = xElement.Element(Constants.S3Ns + "Id")?.Value ?? string.Empty;
        foreach (var xEvent in xElement.Elements(Constants.S3Ns + "Event"))
            Events.Add(new EventType(xEvent.Value));
        var xFilterRules = xElement
            .Element(Constants.S3Ns + "Filter")?
            .Element(Constants.S3Ns + "S3Key")?
            .Elements(Constants.S3Ns + "FilterRule");
        if (xFilterRules != null)
        {
            foreach (var xFilter in xFilterRules)
            {
                var name = xFilter.Element(Constants.S3Ns + "Name")?.Value ??
                           throw new InvalidOperationException("Missing Name in XML");
                var value = xFilter.Element(Constants.S3Ns + "Value")?.Value ??
                            throw new InvalidOperationException("Missing Value in XML");
                Filter[name] = value;
            }
        }
    }
}

public sealed class LambdaConfig : NotificationConfiguration
{
    public required string Lambda { get; set; }

    public XElement Serialize()
    {
        var xElement = new XElement(Constants.S3Ns + "CloudFunctionConfiguration",
            new XElement(Constants.S3Ns + "CloudFunction", Lambda));
        SerializeInner(xElement);
        return xElement;
    }

    public static LambdaConfig Deserialize(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (xElement.Name != Constants.S3Ns + "CloudFunctionConfiguration")
            throw new InvalidOperationException("Invalid XML element encountered");

        var lambdaConfig = new LambdaConfig
        {
            Lambda = xElement.Element(Constants.S3Ns + "CloudFunction")?.Value ?? 
                     throw new InvalidOperationException("Missing CloudFunction in XML")
        };
        lambdaConfig.DeserializeInner(xElement);
        return lambdaConfig;
    }
}

public sealed class TopicConfig : NotificationConfiguration
{
    public string Topic { get; set; }

    public XElement Serialize()
    {
        var xElement = new XElement(Constants.S3Ns + "TopicConfiguration",
            new XElement(Constants.S3Ns + "Topic", Topic));
        SerializeInner(xElement);
        return xElement;
    }

    public static TopicConfig Deserialize(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (xElement.Name != Constants.S3Ns + "TopicConfiguration")
            throw new InvalidOperationException("Invalid XML element encountered");

        var topicConfig = new TopicConfig
        {
            Topic = xElement.Element(Constants.S3Ns + "Topic")?.Value ?? 
                    throw new InvalidOperationException("Missing Topic in XML")
        };
        topicConfig.DeserializeInner(xElement);
        return topicConfig;
    }
}

public sealed class QueueConfig: NotificationConfiguration
{
    public string Queue { get; set; }

    public XElement Serialize()
    {
        var xElement = new XElement(Constants.S3Ns + "QueueConfiguration",
            new XElement(Constants.S3Ns + "Queue", Queue));
        SerializeInner(xElement);
        return xElement;
    }

    public static QueueConfig Deserialize(XElement xElement)
    {
        ArgumentNullException.ThrowIfNull(xElement);
        if (xElement.Name != Constants.S3Ns + "QueueConfiguration")
            throw new InvalidOperationException("Invalid XML element encountered");

        var queueConfig = new QueueConfig
        {
            Queue = xElement.Element(Constants.S3Ns + "Queue")?.Value ?? 
                     throw new InvalidOperationException("Missing Queue in XML")
        };
        queueConfig.DeserializeInner(xElement);
        return queueConfig;
    }
}

public readonly struct EventType : IEquatable<EventType>
{
    public static EventType ObjectCreatedAll { get; } = new("s3:ObjectCreated:*");
    public static EventType ObjectCreatedPut { get; } = new("s3:ObjectCreated:Put");
    public static EventType ObjectCreatedPost { get; } = new("s3:ObjectCreated:Post");
    public static EventType ObjectCreatedCopy { get; } = new("s3:ObjectCreated:Copy");
    public static EventType ObjectCreatedCompleteMultipartUpload { get; } = new("s3:ObjectCreated:CompleteMultipartUpload");
    public static EventType ObjectAccessedGet { get; } = new("s3:ObjectAccessed:Get");
    public static EventType ObjectAccessedHead { get; } = new("s3:ObjectAccessed:Head");
    public static EventType ObjectAccessedAll { get; } = new("s3:ObjectAccessed:*");
    public static EventType ObjectRemovedAll { get; } = new("s3:ObjectRemoved:*");
    public static EventType ObjectRemovedDelete { get; } = new("s3:ObjectRemoved:Delete");
    public static EventType ObjectRemovedDeleteMarkerCreated { get; } = new("s3:ObjectRemoved:DeleteMarkerCreated");
    public static EventType ReducedRedundancyLostObject { get; } = new("s3:ReducedRedundancyLostObject");

    private readonly string _value;

    public EventType(string value)
    {
        _value = value;
    }

    public override string ToString() => _value;
    public static implicit operator string(EventType et) => et._value;

    public override bool Equals(object obj)
    {
        return obj is EventType other && 
               other._value.Equals(_value, StringComparison.Ordinal);
    }

    public bool Equals(EventType other) =>
        _value.Equals(other._value, StringComparison.Ordinal);

    public override int GetHashCode() => _value.GetHashCode();
    public static bool operator ==(EventType left, EventType right) => left.Equals(right);
    public static bool operator !=(EventType left, EventType right) => !(left == right);
}
