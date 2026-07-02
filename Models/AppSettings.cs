using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WordRecorder.Models;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    private string _theme = "Default"; // Default, Light, Dark

    [ObservableProperty]
    private string _accentColor = "#FF0078D4"; // Windows Blue

    // AI Settings
    [ObservableProperty]
    private string _aiEndpoint = "https://maas-api.cn-huabei-1.xf-yun.com/v2";

    [ObservableProperty]
    private string _aiApiKey = "248b71c69c68ccc321bdaabe7650ce94:MDNhYmQ1ZjYxNjJlNzRkYzc0Zjc4NWRm";

    [ObservableProperty]
    private string _aiModel = "xop35qwen2b";

    [ObservableProperty]
    private bool _aiEnabled = true;

    // Dictionary Settings
    [ObservableProperty]
    private string _lingoesDictPath = "";

    [ObservableProperty]
    private bool _dictEnabled;

    public List<string> Themes { get; } = new()
    {
        "Default",
        "Light",
        "Dark"
    };

    public static AppSettings Default => new();
}
