using System.Text.Json;
using System.Text.Json.Serialization;

namespace NightreignRelicEditor.Models.JSON;

public class RelicEffectJsonConverter : JsonConverter<RelicEffect>
{
    public override RelicEffect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt32(out var id))
        {
            return new RelicEffect { Id = id };
        }
        throw new JsonException("Expected number for RelicEffect");
    }

    public override void Write(Utf8JsonWriter writer, RelicEffect value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Id);
    }
}