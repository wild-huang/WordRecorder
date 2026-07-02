using CommunityToolkit.Mvvm.ComponentModel;
using WordRecorder.Models;

namespace WordRecorder.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    public static AppSettings DesignTimeSettings { get; } = AppSettings.Default;
}
