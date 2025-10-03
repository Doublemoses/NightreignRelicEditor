using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NightreignRelicEditor.Models;

namespace NightreignRelicEditor.Views.Controls;

partial class RelicData : UserControl
{
    public static readonly DependencyProperty RelicManagerProperty =
        DependencyProperty.Register(nameof(RelicManager), typeof(RelicManager), typeof(RelicData), new PropertyMetadata(null));
    public RelicManager? RelicManager
    {
        get => (RelicManager) GetValue(RelicManagerProperty);
        set => SetValue(RelicManagerProperty, value);
    }
    
    public static readonly DependencyProperty RelicSlotProperty =
        DependencyProperty.Register(nameof(RelicSlot), typeof(uint), typeof(RelicData), new PropertyMetadata(uint.MinValue));
    public uint RelicSlot
    {
        get => (uint) GetValue(RelicSlotProperty);
        set => SetValue(RelicSlotProperty, value);
    }

    public bool IsActive { get; set; } = true;

    // slot 4 - 6 inclusive
    public bool IsDeepRelic => RelicSlot > 2;
    public string RelicName => $"{(IsDeepRelic ? "Deep " : "")}Relic {RelicSlot + 1}";
    
    
    protected TextBlock[] EffectTextBlocks;
    protected TextBlock[] CurseTextBlocks;
    protected Button[] EffectClearButtons;
    protected Button[] CurseClearButtons;
    
    
    public RelicData()
    {
        InitializeComponent();
        
        EffectTextBlocks = [TextBlockEffect1, TextBlockEffect2, TextBlockEffect3];
        CurseTextBlocks = [TextBlockCurse1, TextBlockCurse2, TextBlockCurse3];
        EffectClearButtons = [ButtonClearEffect1, ButtonClearEffect2, ButtonClearEffect3];
        CurseClearButtons = [ButtonClearCurse1, ButtonClearCurse2, ButtonClearCurse3];

        Loaded += (s, e) => AfterInit();
        
        ButtonClearEffect1.Click += (s, e) => RemoveRelicEffect(0);
        ButtonClearEffect2.Click += (s, e) => RemoveRelicEffect(1);
        ButtonClearEffect3.Click += (s, e) => RemoveRelicEffect(2);
        
        ButtonClearCurse1.Click += (s, e) => RemoveRelicEffect(0, true);
        ButtonClearCurse2.Click += (s, e) => RemoveRelicEffect(1, true);
        ButtonClearCurse3.Click += (s, e) => RemoveRelicEffect(2, true);
    }

    private void AfterInit()
    {
        UpdateUIElements();
    }
    
    private void RemoveRelicEffect(uint slot, bool curse = false)
    {
        if (RelicManager is null)
            return;
        
        RelicManager.RemoveRelicEffect(RelicSlot, slot, curse, true);
        UpdateUIElements();
    }
    

    private void VerifyRelic()
    {
        if (RelicManager is null)
            return;
        
        var errors = RelicManager.VerifyRelic(RelicSlot);

        for (uint slot = 0; slot < 3; slot++)
        {
            switch (errors[slot])
            {
                case RelicErrors.Legitimate:
                    EffectTextBlocks[slot].Foreground = Brushes.Black;
                    EffectTextBlocks[slot].ToolTip = null;
                    break;
                case RelicErrors.NotRelicEffect:
                    SetTextVerify(slot, "Effect is not a valid relic effect.", Brushes.Red);
                    break;
                case RelicErrors.MultipleFromCategory:
                    SetTextVerify(slot, "This effect has the same category as another effect in this relic.", Brushes.Red);
                    break;
                case RelicErrors.UniqueRelicEffect:
                    SetTextVerify(slot, "Relic effect is only for special unique relics.", Brushes.Orange);
                    break;
            }
        }
    }
    
    private void SetTextVerify(uint slot, string errorText, Brush colour)
    {
        EffectTextBlocks[slot].Foreground = colour;
        EffectTextBlocks[slot].ToolTip = errorText;
    }
    
    public void UpdateUIElements()
    {
        if (RelicManager is null)
            return;
        
        for (uint i = 0; i < 3; i++)
        {
            EffectTextBlocks[i].Text = RelicManager.GetRelicEffectDescription(RelicSlot, i);

            var effectId = RelicManager.GetRelicEffectId(RelicSlot, i);

            EffectClearButtons[i].Visibility = effectId == 0xFFFFFFFF
                ? Visibility.Hidden : Visibility.Visible;
        }

        if (IsDeepRelic)
        {
            for (uint i = 0; i < 3; i++)
            {
                CurseTextBlocks[i].Text = RelicManager.GetRelicEffectDescription(RelicSlot, i, true);
                
                var effectId = RelicManager.GetRelicEffectId(RelicSlot, i, true);
                CurseClearButtons[i].Visibility = effectId == 0xFFFFFFFF
                    ? Visibility.Hidden : Visibility.Visible;
            } 
        }
        else
        {
            for (uint i = 0; i < 3; i++)
            {
                CurseClearButtons[i].Visibility = Visibility.Hidden;
            } 
        }

        VerifyRelic();
    }
}