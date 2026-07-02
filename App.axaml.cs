using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using WordRecorder.ViewModels;
using WordRecorder.Views;

namespace WordRecorder;

public partial class App : Application
{
    public static App? Instance { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Instance = this;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void ApplyAccentColor(string colorHex)
    {
        try
        {
            var color = Color.Parse(colorHex);

            // Update the system accent color resources
            if (Resources.ContainsKey("SystemAccentColor"))
                Resources["SystemAccentColor"] = color;
            else
                Resources.Add("SystemAccentColor", color);

            // Update related brushes
            var lightColor = Color.FromArgb(40, color.R, color.G, color.B);
            var mediumColor = Color.FromArgb(80, color.R, color.G, color.B);

            if (Resources.ContainsKey("SystemAccentColorLight1"))
                Resources["SystemAccentColorLight1"] = lightColor;
            else
                Resources.Add("SystemAccentColorLight1", lightColor);

            if (Resources.ContainsKey("SystemAccentColorLight2"))
                Resources["SystemAccentColorLight2"] = mediumColor;
            else
                Resources.Add("SystemAccentColorLight2", mediumColor);
        }
        catch
        {
            // Ignore invalid color
        }
    }
}
