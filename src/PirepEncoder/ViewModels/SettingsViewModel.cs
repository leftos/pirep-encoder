using CommunityToolkit.Mvvm.ComponentModel;
using PirepEncoder.Models;

namespace PirepEncoder.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? DefaultSaIdentifier { get; set; }

    [ObservableProperty]
    public partial bool PrefixWithSaIdentifier { get; set; } = true;

    [ObservableProperty]
    public partial string? DefaultAircraftType { get; set; }

    public AppSettings ToModel() => new()
    {
        DefaultSaIdentifier = string.IsNullOrWhiteSpace(DefaultSaIdentifier) ? null : DefaultSaIdentifier!.Trim().ToUpperInvariant(),
        PrefixWithSaIdentifier = PrefixWithSaIdentifier,
        DefaultAircraftType = string.IsNullOrWhiteSpace(DefaultAircraftType) ? null : DefaultAircraftType!.Trim().ToUpperInvariant(),
    };

    public void LoadFrom(AppSettings s)
    {
        DefaultSaIdentifier = s.DefaultSaIdentifier;
        PrefixWithSaIdentifier = s.PrefixWithSaIdentifier;
        DefaultAircraftType = s.DefaultAircraftType;
    }
}
