using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WordRecorder.ViewModels;

namespace WordRecorder.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnColorSelected(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Background is SolidColorBrush brush)
        {
            if (DataContext is SettingsViewModel vm)
            {
                var color = brush.Color;
                vm.SelectedAccentColor = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }
    }
}
