using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minio.Helpers;

public class JsonCollectionItemConverter<TDatatype, TConverterType> : JsonConverter<List<TDatatype>>
    where TConverterType : JsonConverter
{
    public override List<TDatatype> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        var jsonSerializerOptions = new JsonSerializerOptions(options);
        jsonSerializerOptions.Converters.Clear();
        jsonSerializerOptions.Converters.Add(Activator.CreateInstance<TConverterType>());

        var returnValue = new List<TDatatype>();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                returnValue.Add((TDatatype)JsonSerializer.Deserialize(ref reader, typeof(TDatatype), jsonSerializerOptions));
            reader.Read();
        }

        return returnValue;
    }

    public override void Write(Utf8JsonWriter writer, List<TDatatype>? value, JsonSerializerOptions options)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
            
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var jsonSerializerOptions = new JsonSerializerOptions(options);
        jsonSerializerOptions.Converters.Clear();
        jsonSerializerOptions.Converters.Add(Activator.CreateInstance<TConverterType>());

        writer.WriteStartArray();
        foreach (TDatatype data in value)
            JsonSerializer.Serialize(writer, data, jsonSerializerOptions);
        writer.WriteEndArray();
    }
}