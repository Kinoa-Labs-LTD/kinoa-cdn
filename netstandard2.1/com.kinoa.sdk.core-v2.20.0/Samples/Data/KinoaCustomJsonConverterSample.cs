using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Data;

/// <summary>
///     Sample custom JSON converter for the <see cref="CustomBool"/> type.
///     Serializes boolean as 0/1 integer instead of true/false.
///     Add custom converters before initializing SDK via JsonUtils.AddCustomConverter().
/// </summary>
public class KinoaCustomJsonConverterSample : JsonConverter<CustomBool>
{
    public override CustomBool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new CustomBool() { Value = reader.GetInt64() > 0 };
    }

    public override void Write(Utf8JsonWriter writer, CustomBool value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value ? 1 : 0);
    }
}
