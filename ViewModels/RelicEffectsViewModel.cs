using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NightreignRelicEditor.Models;

namespace NightreignRelicEditor.ViewModels;

public class RelicEffectsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
    public ObservableCollection<OptionItem<int>> RelicSlots =>
    [
        new() { Name = "Relic 1", Value = 0 },
        new() { Name = "Relic 2", Value = 1 },
        new() { Name = "Relic 3", Value = 2 },
        new() { Name = "Deep Relic 4", Value = 3 },
        new() { Name = "Deep Relic 5", Value = 4 },
        new() { Name = "Deep Relic 6", Value = 5 },
    ];
    
    private int selectedRelicSlot;
    public int SelectedRelicSlot
    {
        get => selectedRelicSlot;
        set { selectedRelicSlot = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSetEffect)); }
    }
    
    
    public ObservableCollection<OptionItem<int>> RelicEffectSlots =>
    [
        new() { Name = "Effect 1", Value = 0 },
        new() { Name = "Effect 2", Value = 1 },
        new() { Name = "Effect 3", Value = 2 },
        new() { Name = "Curse 1", Value = 3 },
        new() { Name = "Curse 2", Value = 4 },
        new() { Name = "Curse 3", Value = 5 }
    ];
    
    private int selectedRelicEffectSlot;
    public int SelectedRelicEffectSlot
    {
        get => selectedRelicEffectSlot;
        set { selectedRelicEffectSlot = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSetEffect)); }
    }


    public bool CanSetEffect => (SelectedRelicSlot < 3 && SelectedRelicEffectSlot < 3) || SelectedRelicSlot >= 3;

    public RelicEffectsViewModel()
    {
        selectedRelicSlot = 0;
        selectedRelicEffectSlot = 0;
    }
}