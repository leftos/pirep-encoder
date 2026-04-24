using CommunityToolkit.Mvvm.ComponentModel;
using PirepEncoder.Models;

namespace PirepEncoder.ViewModels;

public partial class CloudLayerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Base { get; set; } = "UNKN";

    [ObservableProperty]
    public partial SkyCover Cover { get; set; } = SkyCover.OVC;

    [ObservableProperty]
    public partial string? Tops { get; set; }
}
