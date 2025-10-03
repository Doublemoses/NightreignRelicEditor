using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using NightreignRelicEditor.Models;
using NightreignRelicEditor.ViewModels;
using NightreignRelicEditor.Views.Controls;

namespace NightreignRelicEditor.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel viewModel;
    private const int RelicSlotCount = 6;

    System.Windows.Threading.DispatcherTimer monitorTimer = new();

    public MainWindow()
    {
        InitializeComponent();
        
        viewModel = (MainWindowViewModel) DataContext;
        RelicEffects.RelicManager = viewModel.RelicManager;
        RelicPresets.RelicManager = viewModel.RelicManager;
        
        InitUI();
        LoadSettingsFile();

        monitorTimer.Tick += monitorTimer_Tick;
        monitorTimer.Interval = TimeSpan.FromMilliseconds(1000);

        Closing += ExitProgram;

        if (CheckboxAutoConnect.IsChecked ?? false)
            Connect();
    }

    private void RefreshUI()
    {
        RelicDataSlot0.UpdateUIElements();
        RelicDataSlot1.UpdateUIElements();
        RelicDataSlot2.UpdateUIElements();
        RelicDataSlot3.UpdateUIElements();
        RelicDataSlot4.UpdateUIElements();
        RelicDataSlot5.UpdateUIElements();
            
        InvalidateVisual(); 
        UpdateLayout();
    }

    private void InitUI()
    {
        RelicEffects.RequestRefresh += (s, e) => RefreshUI();
        RelicPresets.RequestRefresh += (s, e) => RefreshUI();
        
        SetUIConnectionStatus();

        if (viewModel.RelicManager.ConnectionStatus != ConnectionStates.Connected)
        {
            ButtonSetRelicsInGame.IsEnabled = false;
            ButtonImportRelicsFromGame.IsEnabled = false;
        }

        for (uint x = 1; x < RelicSlotCount; x++)
            UpdateRelicUIElements(x);
    }

    private void Connect()
    {
        ButtonConnect.IsEnabled = false;
        viewModel.RelicManager.ConnectToNightreign();

        if (viewModel.RelicManager.ConnectionStatus == ConnectionStates.Connected)            
            monitorTimer.Start();
        else
            ButtonConnect.IsEnabled = true;

        SetUIConnectionStatus();
    }

    private void monitorTimer_Tick(object? sender, EventArgs e)
    {
        var cs = viewModel.RelicManager.ConnectionStatus;

        if (cs == ConnectionStates.Connected)
            return;
        
        monitorTimer.Stop();
        SetUIConnectionStatus();
    }

    private void SetUIConnectionStatus()
    {
        if (viewModel.RelicManager.ConnectionStatus == ConnectionStates.Connected)
        {
            ButtonConnect.IsEnabled = false;
            ButtonSetRelicsInGame.IsEnabled = true;
            ButtonImportRelicsFromGame.IsEnabled = true;

            TextblockConnectionStatus.Foreground = Brushes.Black;
            TextblockConnectionStatus.Text = "Connected";
        }
        else
        {
            ButtonConnect.IsEnabled = true;
            ButtonSetRelicsInGame.IsEnabled = false;
            ButtonImportRelicsFromGame.IsEnabled = false;

            TextblockConnectionStatus.Foreground = Brushes.Red;
        }

        switch (viewModel.RelicManager.ConnectionStatus)
        {
            
            case ConnectionStates.EACDetected:
                TextblockConnectionStatus.Text = "EAC detected";
                break;
            case ConnectionStates.NightreignNotFound:
                TextblockConnectionStatus.Text = "Nightreign process not found";
                break;
            case ConnectionStates.ConnectedOffsetsNotFound:
                TextblockConnectionStatus.Text = "Nightreign process found, but relic memory addresses not found";
                break;
            case ConnectionStates.ConnectionLost:
                TextblockConnectionStatus.Text = "Nightreign connection lost";
                break;
            case ConnectionStates.NotConnected:
                TextblockConnectionStatus.Text = "Not connected";
                TextblockConnectionStatus.Foreground = Brushes.Black;
                break;
        }
    }

    private void ExitProgram(object? sender, EventArgs e)
    {
        SaveSettingsFile();
    }

    private void AddRelicEffectFromList(uint relic)
    {
        // RelicEffect selected = (RelicEffect) listviewRelicEffects.SelectedItem;
        //
        // if (selected != null)
        //     AddRelicEffect(relic, selected);
    }


    private void UpdateRelicUIElements(uint relic)
    {
        var relicDataElement = (RelicData?) FindName($"RelicDataSlot{relic}");
        relicDataElement?.UpdateUIElements();
    }

    //
    // Main UI Buttons
    //

    private void Button_SetRelicsInGame(object sender, RoutedEventArgs e)
    {
        if (viewModel.RelicManager.ConnectionStatus != ConnectionStates.Connected)
            return;
        
        for (uint i = 0; i < RelicSlotCount; i++)
        {
            var relicDataElement = (RelicData?) FindName($"RelicDataSlot{i}");
            if (relicDataElement is { IsActive: false })
                continue;

            var errors = viewModel.RelicManager.VerifyRelic(i);

            foreach (var error in errors)
            {
                switch (error)
                {
                    case RelicErrors.NotRelicEffect:
                        MessageBox.Show("Cannot inject relic with illegal effects.");
                        return;
                    case RelicErrors.MultipleFromCategory:
                        MessageBox.Show("Cannot inject relic with multiple effects from the same category.");
                        return;
                }
            }
        }

        for (uint i = 0; i < RelicSlotCount; i++)
        {
            var relicDataElement = (RelicData?) FindName($"RelicDataSlot{i}");
            if (relicDataElement is { IsActive: false })
                continue;
            
            viewModel.RelicManager.SetRelicInGame(i);
        }
    }

    private void Button_GetRelicsFromGame(object sender, RoutedEventArgs e)
    {
        if (viewModel.RelicManager.ConnectionStatus != ConnectionStates.Connected)
            return;

        for (uint x = 0; x < 6; x++)
        {
            viewModel.RelicManager.GetRelicFromGame(x);
            UpdateRelicUIElements(x);
        }
    }

    private void Button_Connect(object sender, RoutedEventArgs e)
    {
        Connect();
    }

    //
    // Settings file
    //

    enum Settings
    {
        autoconnect,
    }

    private void SaveSettingsFile()
    {
        string file = System.AppDomain.CurrentDomain.BaseDirectory + "settings.nre";
        try
        {
            using (StreamWriter sw = new StreamWriter(file))
            {
                sw.WriteLine(Settings.autoconnect.ToString() + "\t" +
                    ((bool)CheckboxAutoConnect.IsChecked ? "1" : "0"));
            }
        }
        catch (Exception e)
        {
            Debug.Print("Problem saving settings file to " + file);
        }
    }

    private void LoadSettingsFile()
    {
        try
        {
            string file = System.AppDomain.CurrentDomain.BaseDirectory + "settings.nre";

            if (!File.Exists(file))
                return;

            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] split = line.Split("\t");
                    if (split.Length == 2)
                    {
                        switch (split[0])
                        {
                            case "autoconnect":
                                CheckboxAutoConnect.IsChecked = (Convert.ToUInt32(split[1]) == 1) ? true : false;
                                break;
                        }
                    }
                }
            }
        }
        catch (Exception f)
        {
            MessageBox.Show("Problem loading settings file.");
        }
    }
}