using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minio.Helpers;

public sealed class NanoSecTimeSpanJsonConverter : JsonConverter<TimeSpan>
{
#if NET7_0_OR_GREATER
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetInt64() / TimeSpan.NanosecondsPerTick);

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue((long)value.TotalNanoseconds);
#else
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeSpan.FromMilliseconds(reader.GetInt64() / TimeSpan.TicksPerMillisecond);

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Ticks * 100);
#endif
}