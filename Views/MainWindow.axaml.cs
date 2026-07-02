using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using WordRecorder.ViewModels;

namespace WordRecorder.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.AddWordCommand.ExecuteAsync(null);
            }
            e.Handled = true;
            
            // 保持输入框焦点
            if (sender is TextBox textBox)
            {
                textBox.Focus();
            }
        }
    }

    private void OnSuggestionTapped(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is string word)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SelectSuggestionCommand.Execute(word);
            }
        }
    }

    private void OnColorSelected(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is string color)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ApplyPresetColorCommand.Execute(color);
            }
        }
    }

    private void OnPresetColorSelected(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ColorPreset preset)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ApplyPresetColorCommand.Execute(preset.Hex);
            }
        }
    }

    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ApplySettingsCommand.Execute(null);
        }
    }
}
