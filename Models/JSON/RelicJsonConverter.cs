using System.Text.Json;
using System.Text.Json.Serialization;

namespace NightreignRelicEditor.Models.JSON;

public class RelicJsonConverter: JsonConverter<Relic>
{
    public override Relic? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var subOptions = new JsonSerializerOptions(options);
        
        // prevent recursion
        var jsonConverter = subOptions.Converters.FirstOrDefault(c => c.GetType() == typeof(RelicJsonConverter));
        if (jsonConverter is not null)
            subOptions.Converters.Remove(jsonConverter);
        
        return JsonSerializer.Deserialize<Relic>(ref reader, subOptions);
    }

    public override void Write(Utf8JsonWriter writer, Relic value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var subOptions = new JsonSerializerOptions(options);
        subOptions.Converters.Add(new RelicEffectSlotJsonConverter(value.IsDeepRelic));
        
        if (value.IsDeepRelic)
        {
            subOptions.Converters.Add(new RelicEffectSlotJsonConverter(value.IsDeepRelic));
            
            writer.WritePropertyName(nameof(Relic.IsDeepRelic));
            JsonSerializer.Serialize(writer, value.IsDeepRelic, subOptions);
        } 

        writer.WritePropertyName(nameof(Relic.EffectSlots));
        JsonSerializer.Serialize(writer, value.EffectSlots, subOptions);
        
        writer.WriteEndObject();
    }
}