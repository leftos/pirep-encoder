using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PirepEncoder.Models;
using PirepEncoder.Services;

namespace PirepEncoder.ViewModels;

public partial class PirepViewModel : ObservableObject
{
    private AppSettings _settings;
    private bool _loading;

    public PirepViewModel(AppSettings settings)
    {
        _settings = settings;

        PropertyChanged += OnAnyPropertyChanged;

        Locations = [];
        Locations.CollectionChanged += (_, _) => BubbleOutput();

        CloudLayers = [];
        CloudLayers.CollectionChanged += OnCollectionChanged;

        AddLocationSegment();
    }

    [ObservableProperty]
    public partial ReportType ReportType { get; set; } = ReportType.Routine;

    [ObservableProperty]
    public partial string? SaIdentifier { get; set; }

    public ObservableCollection<LocationSegmentViewModel> Locations { get; }

    [ObservableProperty]
    public partial string Time { get; set; } = "";

    [ObservableProperty]
    public partial string FlightLevel { get; set; } = "";

    [ObservableProperty]
    public partial bool FlightLevelUnknown { get; set; }

    [ObservableProperty]
    public partial string AircraftType { get; set; } = "";

    [ObservableProperty]
    public partial bool AircraftTypeUnknown { get; set; }

    // Optional section toggles
    [ObservableProperty]
    public partial bool IncludeSky { get; set; }

    [ObservableProperty]
    public partial bool SkyUseRaw { get; set; }

    [ObservableProperty]
    public partial string? SkyRaw { get; set; }

    public ObservableCollection<CloudLayerViewModel> CloudLayers { get; }

    [ObservableProperty]
    public partial bool IncludeWeather { get; set; }

    [ObservableProperty]
    public partial bool WeatherUseRaw { get; set; }

    [ObservableProperty]
    public partial string? WeatherRaw { get; set; }

    [ObservableProperty]
    public partial int? WeatherFlightVisSm { get; set; }

    [ObservableProperty]
    public partial string WeatherContractions { get; set; } = "";

    [ObservableProperty]
    public partial bool IncludeTemperature { get; set; }

    [ObservableProperty]
    public partial int? TemperatureC { get; set; }

    [ObservableProperty]
    public partial bool IncludeWind { get; set; }

    [ObservableProperty]
    public partial int? WindDirection { get; set; }

    [ObservableProperty]
    public partial int? WindSpeedKt { get; set; }

    [ObservableProperty]
    public partial bool IncludeTurbulence { get; set; }

    [ObservableProperty]
    public partial bool TurbulenceUseRaw { get; set; }

    [ObservableProperty]
    public partial string? TurbulenceRaw { get; set; }

    [ObservableProperty]
    public partial TurbulenceIntensity TurbulenceIntensity { get; set; } = TurbulenceIntensity.LGT;

    [ObservableProperty]
    public partial TurbulenceType TurbulenceType { get; set; } = TurbulenceType.None;

    [ObservableProperty]
    public partial string? TurbulenceAltitudeBand { get; set; }

    [ObservableProperty]
    public partial bool IncludeIcing { get; set; }

    [ObservableProperty]
    public partial bool IcingUseRaw { get; set; }

    [ObservableProperty]
    public partial string? IcingRaw { get; set; }

    [ObservableProperty]
    public partial IcingIntensity IcingIntensity { get; set; } = IcingIntensity.LGT;

    [ObservableProperty]
    public partial IcingType IcingType { get; set; } = IcingType.None;

    [ObservableProperty]
    public partial string? IcingAltitudeBand { get; set; }

    [ObservableProperty]
    public partial bool IncludeRemarks { get; set; }

    [ObservableProperty]
    public partial string? Remarks { get; set; }

    public string EncodedOutput => PirepFormatter.Format(ToModel(), _settings);

    public IReadOnlyList<ValidationError> Errors => PirepValidator.Validate(ToModel());

    public string ErrorSummary
    {
        get
        {
            var errs = Errors;
            return errs.Count == 0 ? "" : string.Join("  •  ", errs.Select(e => $"/{e.Field}: {e.Message}"));
        }
    }

    public static IReadOnlyList<ReportType> ReportTypeChoices { get; } =
        [ReportType.Routine, ReportType.Urgent];

    public static IReadOnlyList<SkyCover> SkyCoverChoices { get; } =
        Enum.GetValues<SkyCover>();

    public static IReadOnlyList<TurbulenceIntensity> TurbulenceIntensityChoices { get; } =
        Enum.GetValues<TurbulenceIntensity>();

    public static IReadOnlyList<TurbulenceType> TurbulenceTypeChoices { get; } =
        Enum.GetValues<TurbulenceType>();

    public static IReadOnlyList<IcingIntensity> IcingIntensityChoices { get; } =
        Enum.GetValues<IcingIntensity>();

    public static IReadOnlyList<IcingType> IcingTypeChoices { get; } =
        Enum.GetValues<IcingType>();

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        BubbleOutput();
    }

    [RelayCommand]
    public void AddLocationSegment()
    {
        var seg = new LocationSegmentViewModel();
        seg.PropertyChanged += OnChildChanged;
        Locations.Add(seg);
    }

    [RelayCommand]
    public void RemoveLocationSegment(LocationSegmentViewModel? segment)
    {
        if (segment is null || Locations.Count <= 1)
        {
            return;
        }
        segment.PropertyChanged -= OnChildChanged;
        Locations.Remove(segment);
    }

    [RelayCommand]
    public void AddCloudLayer()
    {
        var layer = new CloudLayerViewModel();
        layer.PropertyChanged += OnChildChanged;
        CloudLayers.Add(layer);
    }

    [RelayCommand]
    public void RemoveCloudLayer(CloudLayerViewModel? layer)
    {
        if (layer is null)
        {
            return;
        }
        layer.PropertyChanged -= OnChildChanged;
        CloudLayers.Remove(layer);
    }

    [RelayCommand]
    public void InsertNowAsTime()
    {
        Time = DateTime.UtcNow.ToString("HHmm", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    public void Reset()
    {
        _loading = true;
        try
        {
            SaIdentifier = null;
            ReportType = ReportType.Routine;
            Locations.Clear();
            AddLocationSegment();
            Time = "";
            FlightLevel = "";
            FlightLevelUnknown = false;
            AircraftType = "";
            AircraftTypeUnknown = false;

            IncludeSky = false;
            SkyUseRaw = false;
            SkyRaw = null;
            foreach (var layer in CloudLayers.ToList())
            {
                layer.PropertyChanged -= OnChildChanged;
            }
            CloudLayers.Clear();

            IncludeWeather = false;
            WeatherUseRaw = false;
            WeatherRaw = null;
            WeatherFlightVisSm = null;
            WeatherContractions = "";

            IncludeTemperature = false;
            TemperatureC = null;

            IncludeWind = false;
            WindDirection = null;
            WindSpeedKt = null;

            IncludeTurbulence = false;
            TurbulenceUseRaw = false;
            TurbulenceRaw = null;
            TurbulenceIntensity = TurbulenceIntensity.LGT;
            TurbulenceType = TurbulenceType.None;
            TurbulenceAltitudeBand = null;

            IncludeIcing = false;
            IcingUseRaw = false;
            IcingRaw = null;
            IcingIntensity = IcingIntensity.LGT;
            IcingType = IcingType.None;
            IcingAltitudeBand = null;

            IncludeRemarks = false;
            Remarks = null;
        }
        finally
        {
            _loading = false;
            BubbleOutput();
        }
    }

    public Pirep ToModel()
    {
        var said = string.IsNullOrWhiteSpace(SaIdentifier) ? null : SaIdentifier!.Trim().ToUpperInvariant();
        var segments = Locations
            .Select(l => new LocationSegment
            {
                Fix = (l.Fix ?? "").Trim().ToUpperInvariant(),
                RadialDistance = string.IsNullOrWhiteSpace(l.RadialDistance) ? null : l.RadialDistance!.Trim(),
            })
            .Where(s => !string.IsNullOrWhiteSpace(s.Fix))
            .ToList();

        var fl = FlightLevelUnknown ? "UNKN" : (FlightLevel ?? "").Trim().ToUpperInvariant();
        var tp = AircraftTypeUnknown ? "UNKN" : (AircraftType ?? "").Trim().ToUpperInvariant();

        var pirep = new Pirep
        {
            SaIdentifier = said,
            ReportType = ReportType,
            Location = new Location { Segments = segments },
            Time = (Time ?? "").Trim(),
            FlightLevel = string.IsNullOrWhiteSpace(fl) ? "UNKN" : fl,
            AircraftType = string.IsNullOrWhiteSpace(tp) ? "UNKN" : tp,
        };

        if (IncludeSky)
        {
            if (SkyUseRaw)
            {
                pirep = pirep with { SkyCoverRaw = (SkyRaw ?? "").Trim() };
            }
            else if (CloudLayers.Count > 0)
            {
                pirep = pirep with
                {
                    SkyCover = CloudLayers.Select(l => new CloudLayer
                    {
                        Base = string.IsNullOrWhiteSpace(l.Base) ? "UNKN" : l.Base.Trim().ToUpperInvariant(),
                        Cover = l.Cover,
                        Tops = string.IsNullOrWhiteSpace(l.Tops) ? null : l.Tops!.Trim().ToUpperInvariant(),
                    }).ToList(),
                };
            }
        }

        if (IncludeWeather)
        {
            if (WeatherUseRaw)
            {
                pirep = pirep with { WeatherRaw = (WeatherRaw ?? "").Trim() };
            }
            else
            {
                pirep = pirep with
                {
                    Weather = new Weather
                    {
                        FlightVisibilitySm = WeatherFlightVisSm,
                        Contractions = (WeatherContractions ?? "")
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => s.ToUpperInvariant())
                            .ToList(),
                    },
                };
            }
        }

        if (IncludeTemperature)
        {
            pirep = pirep with { TemperatureC = TemperatureC };
        }

        if (IncludeWind && WindDirection is int dir && WindSpeedKt is int spd)
        {
            pirep = pirep with { WindDirection = dir, WindSpeedKt = spd };
        }

        if (IncludeTurbulence)
        {
            if (TurbulenceUseRaw)
            {
                pirep = pirep with { TurbulenceRaw = (TurbulenceRaw ?? "").Trim() };
            }
            else
            {
                pirep = pirep with
                {
                    Turbulence = new Turbulence
                    {
                        Intensity = TurbulenceIntensity,
                        Type = TurbulenceType,
                        AltitudeBand = string.IsNullOrWhiteSpace(TurbulenceAltitudeBand) ? null : TurbulenceAltitudeBand!.Trim().ToUpperInvariant(),
                    },
                };
            }
        }

        if (IncludeIcing)
        {
            if (IcingUseRaw)
            {
                pirep = pirep with { IcingRaw = (IcingRaw ?? "").Trim() };
            }
            else
            {
                pirep = pirep with
                {
                    Icing = new Icing
                    {
                        Intensity = IcingIntensity,
                        Type = IcingType,
                        AltitudeBand = string.IsNullOrWhiteSpace(IcingAltitudeBand) ? null : IcingAltitudeBand!.Trim().ToUpperInvariant(),
                    },
                };
            }
        }

        if (IncludeRemarks && !string.IsNullOrWhiteSpace(Remarks))
        {
            pirep = pirep with { Remarks = Remarks!.Trim() };
        }

        return pirep;
    }

    public void LoadFromModel(Pirep p)
    {
        _loading = true;
        try
        {
            Reset();

            SaIdentifier = p.SaIdentifier;
            ReportType = p.ReportType;

            foreach (var seg in Locations.ToList())
            {
                seg.PropertyChanged -= OnChildChanged;
            }
            Locations.Clear();
            foreach (var src in p.Location.Segments)
            {
                var vm = new LocationSegmentViewModel { Fix = src.Fix, RadialDistance = src.RadialDistance };
                vm.PropertyChanged += OnChildChanged;
                Locations.Add(vm);
            }
            if (Locations.Count == 0)
            {
                AddLocationSegment();
            }

            Time = p.Time;
            FlightLevelUnknown = p.FlightLevel == "UNKN";
            FlightLevel = FlightLevelUnknown ? "" : p.FlightLevel;
            AircraftTypeUnknown = p.AircraftType == "UNKN";
            AircraftType = AircraftTypeUnknown ? "" : p.AircraftType;

            if (p.SkyCover is { Count: > 0 } layers)
            {
                IncludeSky = true;
                SkyUseRaw = false;
                foreach (var src in layers)
                {
                    var vm = new CloudLayerViewModel { Base = src.Base, Cover = src.Cover, Tops = src.Tops };
                    vm.PropertyChanged += OnChildChanged;
                    CloudLayers.Add(vm);
                }
            }
            else if (!string.IsNullOrWhiteSpace(p.SkyCoverRaw))
            {
                IncludeSky = true;
                SkyUseRaw = true;
                SkyRaw = p.SkyCoverRaw;
            }

            if (p.Weather is { } wx)
            {
                IncludeWeather = true;
                WeatherUseRaw = false;
                WeatherFlightVisSm = wx.FlightVisibilitySm;
                WeatherContractions = string.Join(' ', wx.Contractions);
            }
            else if (!string.IsNullOrWhiteSpace(p.WeatherRaw))
            {
                IncludeWeather = true;
                WeatherUseRaw = true;
                WeatherRaw = p.WeatherRaw;
            }

            if (p.TemperatureC is int t)
            {
                IncludeTemperature = true;
                TemperatureC = t;
            }

            if (p.WindDirection is int dir && p.WindSpeedKt is int spd)
            {
                IncludeWind = true;
                WindDirection = dir;
                WindSpeedKt = spd;
            }

            if (p.Turbulence is { } tb)
            {
                IncludeTurbulence = true;
                TurbulenceUseRaw = false;
                TurbulenceIntensity = tb.Intensity;
                TurbulenceType = tb.Type;
                TurbulenceAltitudeBand = tb.AltitudeBand;
            }
            else if (!string.IsNullOrWhiteSpace(p.TurbulenceRaw))
            {
                IncludeTurbulence = true;
                TurbulenceUseRaw = true;
                TurbulenceRaw = p.TurbulenceRaw;
            }

            if (p.Icing is { } ic)
            {
                IncludeIcing = true;
                IcingUseRaw = false;
                IcingIntensity = ic.Intensity;
                IcingType = ic.Type;
                IcingAltitudeBand = ic.AltitudeBand;
            }
            else if (!string.IsNullOrWhiteSpace(p.IcingRaw))
            {
                IncludeIcing = true;
                IcingUseRaw = true;
                IcingRaw = p.IcingRaw;
            }

            if (!string.IsNullOrWhiteSpace(p.Remarks))
            {
                IncludeRemarks = true;
                Remarks = p.Remarks;
            }
        }
        finally
        {
            _loading = false;
            BubbleOutput();
        }
    }

    partial void OnSkyUseRawChanged(bool value)
    {
        if (_loading)
        {
            return;
        }
        if (value && CloudLayers.Count > 0)
        {
            SkyRaw = SkyFormatter.Format(CloudLayers.Select(l => new CloudLayer
            {
                Base = string.IsNullOrWhiteSpace(l.Base) ? "UNKN" : l.Base.Trim().ToUpperInvariant(),
                Cover = l.Cover,
                Tops = string.IsNullOrWhiteSpace(l.Tops) ? null : l.Tops.Trim().ToUpperInvariant(),
            }).ToList());
        }
    }

    partial void OnIncludeSkyChanged(bool value)
    {
        if (_loading)
        {
            return;
        }
        if (value && CloudLayers.Count == 0 && !SkyUseRaw)
        {
            AddCloudLayer();
        }
    }

    partial void OnFlightLevelUnknownChanged(bool value)
    {
        if (value)
        {
            FlightLevel = "";
        }
    }

    partial void OnAircraftTypeUnknownChanged(bool value)
    {
        if (value)
        {
            AircraftType = "";
        }
    }

    private void OnChildChanged(object? sender, PropertyChangedEventArgs e) => BubbleOutput();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (INotifyPropertyChanged? item in e.NewItems)
            {
                if (item is not null)
                {
                    item.PropertyChanged += OnChildChanged;
                }
            }
        }
        if (e.OldItems is not null)
        {
            foreach (INotifyPropertyChanged? item in e.OldItems)
            {
                if (item is not null)
                {
                    item.PropertyChanged -= OnChildChanged;
                }
            }
        }
        BubbleOutput();
    }

    private void OnAnyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }
        if (e.PropertyName is nameof(EncodedOutput) or nameof(Errors) or nameof(ErrorSummary))
        {
            return;
        }
        BubbleOutput();
    }

    private void BubbleOutput()
    {
        OnPropertyChanged(nameof(EncodedOutput));
        OnPropertyChanged(nameof(Errors));
        OnPropertyChanged(nameof(ErrorSummary));
    }
}
