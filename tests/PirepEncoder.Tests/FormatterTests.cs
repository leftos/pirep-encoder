using FluentAssertions;
using PirepEncoder.Models;
using PirepEncoder.Services;

namespace PirepEncoder.Tests;

public class FormatterTests
{
    [Fact]
    public void Required_fields_only_produces_compact_header()
    {
        var pirep = new Pirep
        {
            ReportType = ReportType.Routine,
            Location = new Location { Segments = [new LocationSegment { Fix = "RFD" }] },
            Time = "1315",
            FlightLevel = "160",
            AircraftType = "PA60",
        };

        var s = PirepFormatter.Format(pirep);
        s.Should().Be("UA /OV RFD/TM 1315/FL160/TP PA60");
    }

    [Fact]
    public void Flight_level_uses_no_space_after_code()
    {
        var pirep = BasicPirep() with { FlightLevel = "095" };
        PirepFormatter.Format(pirep).Should().Contain("/FL095");
    }

    [Fact]
    public void Unknown_flight_level_serializes_as_FLUNKN()
    {
        var pirep = BasicPirep() with { FlightLevel = "UNKN" };
        PirepFormatter.Format(pirep).Should().Contain("/FLUNKN");
    }

    [Fact]
    public void Negative_temperature_uses_hyphen_prefix_with_two_digits()
    {
        PirepFormatter.FormatTemperature(-6).Should().Be("-06");
        PirepFormatter.FormatTemperature(-21).Should().Be("-21");
        PirepFormatter.FormatTemperature(15).Should().Be("15");
        PirepFormatter.FormatTemperature(4).Should().Be("04");
    }

    [Fact]
    public void Wind_is_six_digits_direction_plus_speed()
    {
        PirepFormatter.FormatWind(270, 48).Should().Be("270048");
        PirepFormatter.FormatWind(280, 110).Should().Be("280110");
        PirepFormatter.FormatWind(90, 5).Should().Be("090005");
    }

    [Fact]
    public void Sa_identifier_prefix_is_applied_when_enabled()
    {
        var pirep = BasicPirep() with { SaIdentifier = "BOS" };
        var s = PirepFormatter.Format(pirep, new AppSettings { PrefixWithSaIdentifier = true });
        s.Should().StartWith("BOS UA");
    }

    [Fact]
    public void Sa_identifier_suppressed_when_toggle_off()
    {
        var pirep = BasicPirep() with { SaIdentifier = "BOS" };
        var s = PirepFormatter.Format(pirep, new AppSettings { PrefixWithSaIdentifier = false });
        s.Should().StartWith("UA ");
    }

    [Fact]
    public void Default_settings_identifier_is_used_when_pirep_has_none()
    {
        var pirep = BasicPirep();
        var s = PirepFormatter.Format(pirep, new AppSettings
        {
            PrefixWithSaIdentifier = true,
            DefaultSaIdentifier = "ZNY",
        });
        s.Should().StartWith("ZNY UA");
    }

    [Fact]
    public void Urgent_report_uses_UUA_code()
    {
        var pirep = BasicPirep() with { ReportType = ReportType.Urgent };
        PirepFormatter.Format(pirep).Should().StartWith("UUA /OV");
    }

    [Fact]
    public void Sky_layer_with_tops_is_space_separated()
    {
        SkyFormatter.Format([
            new CloudLayer { Base = "025", Cover = SkyCover.OVC, Tops = "095" },
            new CloudLayer { Base = "180", Cover = SkyCover.OVC },
        ]).Should().Be("025 OVC 095/180 OVC");
    }

    [Fact]
    public void Location_fix_to_fix_chain_uses_hyphens()
    {
        LocationFormatter.Format(new Location
        {
            Segments =
            [
                new LocationSegment { Fix = "DHT", RadialDistance = "360015" },
                new LocationSegment { Fix = "AMA" },
                new LocationSegment { Fix = "CDS" },
            ],
        }).Should().Be("DHT 360015-AMA-CDS");
    }

    [Fact]
    public void Raw_overrides_take_precedence_over_structured_models()
    {
        var pirep = BasicPirep() with
        {
            SkyCover = [new CloudLayer { Base = "038", Cover = SkyCover.BKN }],
            SkyCoverRaw = "UNKN OVC",
        };
        PirepFormatter.Format(pirep).Should().Contain("/SK UNKN OVC");
    }

    [Fact]
    public void Pdf_example_one_round_trips_semantically()
    {
        var pirep = new Pirep
        {
            Location = new Location { Segments = [new LocationSegment { Fix = "RFD", RadialDistance = "170030" }] },
            Time = "1315",
            FlightLevel = "160",
            AircraftType = "PA60",
            SkyCover =
            [
                new CloudLayer { Base = "025", Cover = SkyCover.OVC, Tops = "095" },
                new CloudLayer { Base = "180", Cover = SkyCover.OVC },
            ],
            TemperatureC = -21,
            WindDirection = 270,
            WindSpeedKt = 48,
        };

        var encoded = PirepFormatter.Format(pirep);
        encoded.Should().Be("UA /OV RFD 170030/TM 1315/FL160/TP PA60 /SK 025 OVC 095/180 OVC/TA -21/WV 270048");
    }

    [Fact]
    public void Pdf_example_two_encodes_correctly()
    {
        var pirep = new Pirep
        {
            Location = new Location
            {
                Segments =
                [
                    new LocationSegment { Fix = "DHT", RadialDistance = "360015" },
                    new LocationSegment { Fix = "AMA" },
                    new LocationSegment { Fix = "CDS" },
                ],
            },
            Time = "2116",
            FlightLevel = "050",
            AircraftType = "PA32",
            SkyCover = [new CloudLayer { Base = "UNKN", Cover = SkyCover.OVC }],
            Weather = new Weather { FlightVisibilitySm = 3, Contractions = ["R"] },
            Turbulence = new Turbulence { Intensity = TurbulenceIntensity.LGT },
            TemperatureC = 4,
            Remarks = "HVY RAIN",
        };

        var encoded = PirepFormatter.Format(pirep);
        encoded.Should().Be("UA /OV DHT 360015-AMA-CDS/TM 2116/FL050/TP PA32 /SK UNKN OVC/WX FV03 R/TA 04/TB LGT/RM HVY RAIN");
    }

    private static Pirep BasicPirep() => new()
    {
        Location = new Location { Segments = [new LocationSegment { Fix = "BOS" }] },
        Time = "1200",
        FlightLevel = "100",
        AircraftType = "C172",
    };
}
