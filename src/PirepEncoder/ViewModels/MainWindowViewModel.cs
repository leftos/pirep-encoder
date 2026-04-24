using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PirepEncoder.Models;
using PirepEncoder.Services;

namespace PirepEncoder.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SettingsStore _settingsStore = new();
    private readonly DraftStore _draftStore = new();

    public MainWindowViewModel()
    {
        var settings = _settingsStore.Load();
        Pirep = new PirepViewModel(settings);
        Settings = new SettingsViewModel();
        Settings.LoadFrom(settings);
        Settings.PropertyChanged += (_, _) => Pirep.ApplySettings(Settings.ToModel());
        Pirep.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PirepViewModel.EncodedOutput))
            {
                OnPropertyChanged(nameof(Pirep));
            }
        };
        RefreshDrafts();
    }

    public PirepViewModel Pirep { get; }

    public SettingsViewModel Settings { get; }

    public ObservableCollection<DraftEntryViewModel> Drafts { get; } = [];

    [ObservableProperty]
    public partial string DecodeInput { get; set; } = "";

    [ObservableProperty]
    public partial string? DecodeWarnings { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [RelayCommand]
    private async Task CopyAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            StatusMessage = "Clipboard unavailable.";
            return;
        }
        await clipboard.SetTextAsync(Pirep.EncodedOutput);
        StatusMessage = "Copied encoded PIREP to clipboard.";
    }

    [RelayCommand]
    private void SaveDraft()
    {
        var entry = _draftStore.Save(Pirep.ToModel());
        RefreshDrafts();
        StatusMessage = $"Saved draft: {entry.Label}";
    }

    [RelayCommand]
    private void LoadDraft(DraftEntryViewModel? draft)
    {
        if (draft is null)
        {
            return;
        }
        var entry = _draftStore.LoadAll().FirstOrDefault(d => d.Id == draft.Id);
        if (entry is null)
        {
            StatusMessage = "Draft not found.";
            return;
        }
        Pirep.LoadFromModel(entry.Pirep);
        StatusMessage = $"Loaded draft: {entry.Label}";
    }

    [RelayCommand]
    private void DeleteDraft(DraftEntryViewModel? draft)
    {
        if (draft is null)
        {
            return;
        }
        _draftStore.Delete(draft.Id);
        RefreshDrafts();
        StatusMessage = $"Deleted draft: {draft.Label}";
    }

    [RelayCommand]
    private void Decode()
    {
        if (string.IsNullOrWhiteSpace(DecodeInput))
        {
            StatusMessage = "Paste an encoded PIREP first.";
            return;
        }
        var result = PirepParser.Parse(DecodeInput);
        Pirep.LoadFromModel(result.Pirep);
        DecodeWarnings = result.Warnings.Count == 0
            ? null
            : string.Join(Environment.NewLine, result.Warnings.Select(w => $"[{w.Level}] {(w.Field is null ? "" : "/" + w.Field + ": ")}{w.Message}"));
        StatusMessage = result.Warnings.Count == 0
            ? "Decoded successfully."
            : $"Decoded with {result.Warnings.Count} warning(s).";
    }

    [RelayCommand]
    private void Reset()
    {
        Pirep.Reset();
        DecodeInput = "";
        DecodeWarnings = null;
        StatusMessage = "Form reset.";
    }

    [RelayCommand]
    private void PersistSettings()
    {
        _settingsStore.Save(Settings.ToModel());
        StatusMessage = "Settings saved.";
    }

    private void RefreshDrafts()
    {
        Drafts.Clear();
        foreach (var d in _draftStore.LoadAll())
        {
            Drafts.Add(DraftEntryViewModel.From(d));
        }
    }

    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } window)
        {
            return window.Clipboard;
        }
        return null;
    }
}
