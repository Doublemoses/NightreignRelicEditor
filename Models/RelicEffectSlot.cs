using System.Text.Json.Serialization;

namespace NightreignRelicEditor.Models;

public class RelicEffectSlot
{
    [JsonInclude]
    public RelicEffect Effect { get; set; } = new();
    
    [JsonInclude]
    public RelicEffect Curse { get; set; } = new();

    public void ClearEffect()
    {
        Effect = new RelicEffect();
    }

    public void ClearCurse()
    {
        Curse = new RelicEffect();
    }

    public void ClearSlot()
    {
        ClearEffect();
        ClearCurse();
    }

    public override string ToString()
    {
        return $"Effect: {Effect}, Curse: {Curse}";
    }
}