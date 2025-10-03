using System.Text.Json.Serialization;
using System.Windows;

namespace NightreignRelicEditor.Models;

public class Relic(bool isDeepRelic = false)
{
    [JsonInclude]
    public List<RelicEffectSlot> EffectSlots { get; set; } = [new(), new(), new()];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsDeepRelic { get; init; } = isDeepRelic;

    public bool SetEffect(RelicEffect effect, uint slot, bool isCurse = false)
    {
        if (!IsDeepRelic && (effect.IsCurse || isCurse))
            return false;
        
        if (slot >= EffectSlots.Count)
            EffectSlots.Add(new RelicEffectSlot());
        
        var currentEffect = EffectSlots[(int)slot];
        if (isCurse)
            currentEffect.Curse = effect;
        else
            currentEffect.Effect = effect;

        return true;
    }

    public bool RemoveEffect(int slotIndex, bool isCurse = false)
    {
        if (slotIndex < 0 || slotIndex >= EffectSlots.Count)
            return false;
        
        if (isCurse)
        {
            EffectSlots[slotIndex].ClearCurse();

            return true;
        }
        
        EffectSlots[slotIndex].ClearEffect();

        return true;
    }

    public void ClearSlot()
    {
        foreach (var effectSlot in EffectSlots)
        {
            effectSlot.ClearSlot();
        }
    }

    public void SortEffects()
    {
        EffectSlots.Sort((a, b) =>
        {
            var groupCompare = a.Effect.OrderGroup.CompareTo(b.Effect.OrderGroup);
            return groupCompare != 0 
                ? groupCompare 
                : a.Effect.Id.CompareTo(b.Effect.Id);
        });
    }
}
