using System.Text.Json.Serialization;

namespace NightreignRelicEditor.Models;

public class RelicEffect
{
    [JsonInclude]
    public uint Id { get; set; } = uint.MaxValue;
    
    [JsonIgnore]
    public string Description { get; init; } = "Empty";
    
    [JsonIgnore]
    public int Category { get; init; }
    
    [JsonIgnore]
    public int OrderGroup { get; init; } = int.MaxValue;
    
    [JsonIgnore]
    public uint Slot1Weight { get; set; }
    
    
    [JsonIgnore]
    public bool IsDeepEffect => Id is >= 6_000_000 and < 7_000_000;

    [JsonIgnore]
    public bool IsCurse => Id is >= 6_820_000 and < 7_000_000;
    
    [JsonIgnore]
    public bool IsEmpty => Id == uint.MaxValue;

    
    public override string ToString()
    {
        return $"{Id}: {Description}";
    }
}