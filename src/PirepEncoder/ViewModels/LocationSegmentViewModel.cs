using CommunityToolkit.Mvvm.ComponentModel;

namespace PirepEncoder.ViewModels;

public partial class LocationSegmentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Fix { get; set; } = "";

    [ObservableProperty]
    public partial string? RadialDistance { get; set; }
}
