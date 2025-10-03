using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace NightreignRelicEditor.Models;

public class RelicManager
{
    private const string ProcessName = "nightreign";
    private const string ModuleName = "nightreign.exe";
    
    private GameLink nightreign;

    private IntPtr relicAddress = IntPtr.Zero;
    private IntPtr gameDataManAddress = IntPtr.Zero;
    private nint relicBaseOffset = 0x2F4;

    public Relic[] CharacterRelics { get; } =
    [
        new(), new(), new(),
        new(true), new(true), new(true)
    ];

    public List<RelicEffect> AllRelicEffects = [];


    private ConnectionStates connectionStatus;
    public ConnectionStates ConnectionStatus
    {
        set => connectionStatus = value;

        get
        {
            if (connectionStatus != ConnectionStates.Connected)
                return connectionStatus;

            if (!nightreign.Connected)
                connectionStatus = ConnectionStates.ConnectionLost;

            return connectionStatus;
        }
    }

    public RelicManager()
    {
        nightreign = new GameLink(ProcessName, ModuleName);
        LoadRelicEffects();
    }

    
    public void ConnectToNightreign()
    {
        if (nightreign.CheckProcessRunning("easyanticheat_eos"))
        {
            MessageBox.Show("EAC running");
            ConnectionStatus = ConnectionStates.EACDetected;
            
            return;
        }
        
        nightreign.InitGameLink();

        if (nightreign.Connected)
        {
            SetRelicOffsetByVersion(nightreign.Version);

            ConnectionStatus = LocateRelicAddress()
                ? ConnectionStates.Connected : ConnectionStates.ConnectedOffsetsNotFound;
        }
        else
        {
            ConnectionStatus = ConnectionStates.NightreignNotFound;
        }
    }

    private bool LocateRelicAddress()
    {
        using (var scanner = new AOBScanner(nightreign.ProcessHandle,
                   nightreign.BaseAddress,
                   ".text"))
        {
            var address = scanner.FindAddress("8B 03 89 44 24 40 48 8B 0D ?? ?? ?? ?? 48 85 C9"); // CSGaitem
            if (address != IntPtr.Zero)
                relicAddress = CalculateLocation(address, 9);

            address = scanner.FindAddress("48 8B 05 ?? ?? ?? ?? 48 8B 70 08");   // GameDataMan
            if (address != IntPtr.Zero)
                gameDataManAddress = CalculateLocation(address);
        }

        return relicAddress != IntPtr.Zero && gameDataManAddress != IntPtr.Zero;
    }

    private IntPtr CalculateLocation(IntPtr address, int offset = 3)
    {
        return (IntPtr)nightreign.ReadUInt64(address + offset + 4 + (IntPtr)nightreign.ReadInt32(address + offset));
    }

    private void SetRelicOffsetByVersion(uint[] version)
    {
        if (version[1] == 1)
            relicBaseOffset = 0x2E8;
        
        relicBaseOffset = 0x2F4;
    }

    private void LoadRelicEffects()
    {
        try
        {
            string line;
            var assembly = Assembly.GetExecutingAssembly();
            var resource = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("reliceffects.tsv"));
            using var sr = new StreamReader(assembly.GetManifestResourceStream(resource));
            while ((line = sr.ReadLine()) != null)
            {
                var split = line.Split("\t");

                if (split.Length != 5)
                    continue;
                
                var re = new RelicEffect
                {
                    Id = uint.Parse(split[0]),
                    Description = split[1],
                    Category = int.Parse(split[2]),
                    OrderGroup = int.Parse(split[3])
                };

                if (uint.TryParse(split[4], out var weight))
                    re.Slot1Weight = weight;

                AllRelicEffects.Add(re);
            }
        }
        catch (Exception f)
        {
            MessageBox.Show($"Error loading relic effects - {f}");
        }
    }

    private uint GetRelicOffset(uint relicSlot)
    {
        return nightreign.ReadUInt16((IntPtr)nightreign.ReadUInt64(gameDataManAddress + 0x8) + relicBaseOffset + (IntPtr)(4 * relicSlot)) * 8 + 8;
    }

    
    public void SetRelicEffect(uint relicSlot, uint effectSlot, RelicEffect effect, bool isCurse = false, bool sort = false)
    {
        if (relicSlot >= CharacterRelics.Length)
            return;
        
        Debug.Print($"Setting {(isCurse ? "curse" : "effect")} {effect.Id} ({effect.Description}) to relic {relicSlot}");
        CharacterRelics[relicSlot].SetEffect(effect, effectSlot, isCurse);

        if (sort)
            CharacterRelics[relicSlot].SortEffects();
    }

    public void RemoveRelicEffect(uint relicSlot, uint effectSlot, bool isCurse = false, bool sort = false)
    {
        if (relicSlot >= CharacterRelics.Length)
            return;
        
        if (effectSlot >= CharacterRelics[relicSlot].EffectSlots.Count)
            return;
        
        Debug.Print($"Removing {(isCurse ? "curse" : "effect")} {effectSlot} from relic {relicSlot} ");
        CharacterRelics[relicSlot].RemoveEffect((int) effectSlot, isCurse);
        if (sort)
            CharacterRelics[relicSlot].SortEffects();
    }

    public uint GetRelicEffectId(uint relicSlot, uint effectSlot, bool isCurse = false)
    {
        if (relicSlot >= CharacterRelics.Length)
            return 0xFFFFFFFF;
        
        if (effectSlot >= CharacterRelics[relicSlot].EffectSlots.Count)
            return 0xFFFFFFFF;
        
        var effect = CharacterRelics[relicSlot].EffectSlots[(int)effectSlot];
        return (isCurse ? effect.Curse : effect.Effect).Id;
    }

    public string GetRelicEffectDescription(uint relicSlot, uint effectSlot, bool isCurse = false)
    {
        if (relicSlot >= CharacterRelics.Length)
            return "-";
        
        if (effectSlot >= CharacterRelics[relicSlot].EffectSlots.Count)
            return "-";
        
        var effect = CharacterRelics[relicSlot].EffectSlots[(int)effectSlot];
        return (isCurse ? effect.Curse : effect.Effect).Description;
    }

    public void SetRelic(uint relicSlot, RelicEffectSlot[] effects)
    {
        if (relicSlot >= CharacterRelics.Length)
            return;

        CharacterRelics[relicSlot].ClearSlot();

        for (uint i = 0; i < 3; i += 1)
        {
            var effect = AllRelicEffects.FirstOrDefault(x => x.Id == effects[i].Effect.Id);
            if (effect != null)
            {
                SetRelicEffect(relicSlot, i, effect);
            }

            var curse = AllRelicEffects.FirstOrDefault(x => x.Id == effects[i].Curse.Id);
            if (curse != null)
            {
                SetRelicEffect(relicSlot, i, curse, true);
            }
        }
        
        CharacterRelics[relicSlot].SortEffects();
    }

    public void SetRelicInGame(uint relicSlot)
    {
        var relicOffset = (nint)GetRelicOffset(relicSlot);
        const IntPtr slotOffset = 0x18;

        for (uint effectSlot = 0; effectSlot < 3; effectSlot += 1)
        {
            var preRelicOffset = relicAddress + relicOffset;
            var readPointer = (IntPtr) nightreign.ReadUInt64(preRelicOffset);
            var offset = readPointer + (IntPtr) (effectSlot * 4) + slotOffset;
            
            nightreign.WriteUInt32(offset, GetRelicEffectId(relicSlot, effectSlot));
            nightreign.WriteUInt32(offset + 0x28, GetRelicEffectId(relicSlot, effectSlot, true));
        }
    }

    public void GetRelicFromGame(uint relicSlot)
    {
        var relicOffset = (nint) GetRelicOffset(relicSlot);
        const IntPtr slotOffset = 0x18;

        var effects = new RelicEffectSlot[] { new(), new(), new() };
        for (uint effectSlot = 0; effectSlot < 3; effectSlot += 1)
        {
            var preRelicOffset = relicAddress + relicOffset;
            var readPointer = (IntPtr) nightreign.ReadUInt64(preRelicOffset);
            var offset = readPointer + (IntPtr) ((effectSlot) * 4) + slotOffset;
            var curseOffset = offset + 0x28;
            
            var effectId = nightreign.ReadUInt32(offset);
            effects[effectSlot].Effect.Id = effectId;
            
            var curseId = nightreign.ReadUInt32(curseOffset);
            effects[effectSlot].Curse.Id = curseId;
        }

        SetRelic(relicSlot, effects);
    }

    public RelicEffect? GetRelicEffectFromId(uint effectId) => AllRelicEffects.FirstOrDefault(x => x.Id == effectId);

    
    //
    // Verification
    //

    public bool VerifyEffectIsRelicEffect(RelicEffect effect, bool includeUnique = false)
    {
        if (effect.Slot1Weight != 0)
            return true;

        if (effect.Id is >= 6_000_000 and < 8_000_000 || (includeUnique && effect.Id < 100_000) || effect.Id == 0xFFFFFFFF)
            return true;

        return false;

    }

    public RelicErrors[] VerifyRelic(uint relicSlot)
    {
        Debug.Print($"Verifying relic {relicSlot}");

        var validator = new RelicErrors[6];

        for (var i = 0; i < CharacterRelics[relicSlot].EffectSlots.Count; i++)
        {
            var effect = CharacterRelics[relicSlot].EffectSlots[i].Effect;
            validator[i] = RelicErrors.Legitimate;

            Debug.Print( $"{effect.Id}: {effect.Description}");

            if (effect.Id == 0xFFFFFFFF)
                continue; // empty should always be valid

            if (!effect.IsDeepEffect && effect.Slot1Weight == 0)
            {
                if (VerifyEffectIsRelicEffect(effect, true))
                    validator[i] = RelicErrors.UniqueRelicEffect;
                else
                {
                    validator[i] = RelicErrors.NotRelicEffect;
                    continue;
                }
            }

                
            for (var j = 0; j < CharacterRelics[relicSlot].EffectSlots.Count; j++)
            {
                if (i == j)
                    continue;
                
                var other =  CharacterRelics[relicSlot].EffectSlots[j].Effect;
                if (other.Category != effect.Category)
                    continue;
                
                validator[i] = RelicErrors.MultipleFromCategory;
                break;
            }
        }
        return validator;
    }
}