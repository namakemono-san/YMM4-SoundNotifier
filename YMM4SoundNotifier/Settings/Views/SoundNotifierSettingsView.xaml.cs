using System.ComponentModel;
using System.Windows;

namespace YMM4SoundNotifier.Settings.Views;

public partial class SoundNotifierSettingsView
{
    public SoundNotifierSettingsView()
    {
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this)) return;

        DataContext = SoundNotifierSettings.Default;
    }

    private void OnPreviewClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not TriggerSound sound) return;

        SoundNotifier.Current?.PlayPreview(sound);
    }

    private void OnStopClick(object sender, RoutedEventArgs e)
        => SoundNotifier.Current?.StopPreview();
}
