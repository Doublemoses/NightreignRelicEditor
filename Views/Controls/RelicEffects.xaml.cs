using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NightreignRelicEditor.Models;
using NightreignRelicEditor.ViewModels;

namespace NightreignRelicEditor.Views.Controls;

public partial class RelicEffects : UserControl
{
    public static readonly DependencyProperty RelicManagerProperty =
        DependencyProperty.Register(nameof(RelicManager), typeof(RelicManager), typeof(RelicEffects), new FrameworkPropertyMetadata(defaultValue: null));

    public RelicManager RelicManager
    {
        get => (RelicManager) GetValue(RelicManagerProperty);
        set => SetValue(RelicManagerProperty, value);
    }
    

    public event EventHandler? RequestRefresh;

    private void RefreshUI()
    {
        RequestRefresh?.Invoke(this, EventArgs.Empty);
    }
    

    public RelicEffects()
    {
        InitializeComponent();
        
        Loaded += (s, e) => AfterInit();
    }

    
    protected void AfterInit()
    {
        ListViewRelicEffects.ItemsSource = RelicManager.AllRelicEffects;
        FilterRelicEffectsBox();
    }

    private void TextboxEffectsFilter_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterRelicEffectsBox();
    }

    private void ToggleShowUniqueEffects(object sender, RoutedEventArgs e)
    {
        FilterRelicEffectsBox();
    }

    private void ClearEffectFilter(object sender, RoutedEventArgs e)
    {
        TextboxEffectsFilter.Text = "";
    }
    
    public void FilterRelicEffectsBox()
    {
        var filterText = TextboxEffectsFilter.Text;
        
        // additional filter for curses
        Func<RelicEffect, bool> curseFilter = _ => true;
        if (filterText.Contains("curse:1"))
        {
            filterText = filterText.Replace("curse:1 ", "").Replace(" curse:1", "").Replace("curse:1", "");
            curseFilter = x => x.IsCurse;
        }
        else if (filterText.Contains("curse:0"))
        {
            filterText = filterText.Replace("curse:0 ", "").Replace(" curse:0", "").Replace("curse:0", "");
            curseFilter = x => !x.IsCurse;
        }
        
        // additional filter for depth
        Func<RelicEffect, bool> depthFilter = _ => true;
        if (filterText.Contains("depth:1"))
        {
            filterText = filterText.Replace("depth:1 ", "").Replace(" depth:1", "").Replace("depth:1", "");
            depthFilter = x => x.IsDeepEffect;
        }
        else if (filterText.Contains("depth:0"))
        {
            filterText = filterText.Replace("depth:0 ", "").Replace(" depth:0", "").Replace("depth:0", "");
            depthFilter = x => !x.IsDeepEffect;
        }

        Func<RelicEffect, bool> descriptionFilter = x => x.Description.ToLower().Contains(filterText.ToLower());
        
        var view = CollectionViewSource.GetDefaultView(RelicManager.AllRelicEffects);
        view.Filter = (entry) =>
        {
            var effect = (RelicEffect) entry;

            return (filterText.Length == 0 || descriptionFilter(effect))
                   & RelicManager.VerifyEffectIsRelicEffect(effect, (CheckboxShowUnique.IsChecked ?? false)) 
                   & curseFilter(effect) & depthFilter(effect);
        };
    }

    
    private void Button_SetRelicEffect(object sender, RoutedEventArgs e)
    {
        var selected = (RelicEffect) ListViewRelicEffects.SelectedItem;

        if (selected == null)
            return;
        
        var viewModel = (RelicEffectsViewModel) DataContext;

        var isCurse = viewModel.RelicEffectSlots
            .First(x => viewModel.SelectedRelicEffectSlot == x.Value)
            .Name.ToLower().Contains("curse");
        
        RelicManager.SetRelicEffect((uint) viewModel.SelectedRelicSlot, (uint) viewModel.SelectedRelicEffectSlot, selected, isCurse, true);
        RefreshUI();
    }
}