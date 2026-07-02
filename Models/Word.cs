using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WordRecorder.Models;

public partial class Word : ObservableObject
{
    [ObservableProperty]
    private string _term = string.Empty;

    [ObservableProperty]
    private string _phonetic = string.Empty;

    [ObservableProperty]
    private string _definition = string.Empty;

    [ObservableProperty]
    private string _translation = string.Empty;

    [ObservableProperty]
    private DateTime _addedTime = DateTime.Now;

    [ObservableProperty]
    private DateTime _lastLookupTime = DateTime.Now;

    [ObservableProperty]
    private int _lookupCount = 1;

    [ObservableProperty]
    private bool _isPossibleMistake;

    [ObservableProperty]
    private string _suggestedCorrection = string.Empty;

    [ObservableProperty]
    private List<string> _possibleWords = new();

    [JsonIgnore]
    public bool ShowLookupCount => LookupCount > 1;

    [JsonIgnore]
    public string LookupCountText => $"已查 {LookupCount} 次";

    public override string ToString()
    {
        return $"{Term} - {Translation}";
    }
}
