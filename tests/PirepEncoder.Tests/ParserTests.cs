using FluentAssertions;
using PirepEncoder.Models;
using PirepEncoder.Services;

namespace PirepEncoder.Tests;

public class ParserTests
{
    [Fact]
    public void Parses_required_fields_from_minimal_report()
    {
        var result = PirepParser.Parse("UA /OV BOS/TM 1200/FL100/TP C172");
        result.Pirep.ReportType.Should().Be(ReportType.Routine);
        result.Pirep.Location.Segments.Should().HaveCount(1);
        result.Pirep.Location.Segments[0].Fix.Should().Be("BOS");
        result.Pirep.Time.Should().Be("1200");
        result.Pirep.FlightLevel.Should().Be("100");
        result.Pirep.AircraftType.Should().Be("C172");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Parses_sa_identifier_prefix()
    {
        var result = PirepParser.Parse("BOS UA /OV RFD/TM 1200/FL100/TP C172");
        result.Pirep.SaIdentifier.Should().Be("BOS");
    }

    [Fact]
    public void Parses_urgent_report_type()
    {
        var result = PirepParser.Parse("UUA /OV BOS/TM 1200/FL100/TP C172");
        result.Pirep.ReportType.Should().Be(ReportType.Urgent);
    }

    [Fact]
    public void Parses_negative_temperature()
    {
        var result = PirepParser.Parse("UA /OV BOS/TM 1200/FL100/TP C172/TA -21");
        result.Pirep.TemperatureC.Should().Be(-21);
    }

    [Fact]
    public void Parses_fix_to_fix_chain_with_radial()
    {
        var result = PirepParser.Parse("UA /OV DHT 360015-AMA-CDS/TM 2116/FL050/TP PA32");
        result.Pirep.Location.Segments.Should().HaveCount(3);
        result.Pirep.Location.Segments[0].Fix.Should().Be("DHT");
        result.Pirep.Location.Segments[0].RadialDistance.Should().Be("360015");
        result.Pirep.Location.Segments[1].Fix.Should().Be("AMA");
        result.Pirep.Location.Segments[1].RadialDistance.Should().BeNull();
        result.Pirep.Location.Segments[2].Fix.Should().Be("CDS");
    }

    [Fact]
    public void Parses_multiple_sky_layers()
    {
        var result = PirepParser.Parse("UA /OV BOS/TM 1200/FL100/TP C172/SK 025 OVC 095/180 OVC");
        result.Pirep.SkyCover.Should().NotBeNull();
        result.Pirep.SkyCover!.Should().HaveCount(2);
        result.Pirep.SkyCover![0].Base.Should().Be("025");
        result.Pirep.SkyCover![0].Cover.Should().Be(SkyCover.OVC);
        result.Pirep.SkyCover![0].Tops.Should().Be("095");
        result.Pirep.SkyCover![1].Base.Should().Be("180");
        result.Pirep.SkyCover![1].Tops.Should().BeNull();
    }

    [Fact]
    public void Parses_wind_six_digit_format()
    {
        var result = PirepParser.Parse("UA /OV BOS/TM 1200/FL100/TP C172/WV 270048");
        result.Pirep.WindDirection.Should().Be(270);
        result.Pirep.WindSpeedKt.Should().Be(48);
    }

    [Fact]
    public void Parses_weather_with_flight_visibility()
    {
        var result = PirepParser.Parse("UA /OV BOS/TM 1200/FL100/TP C172/WX FV03 R");
        result.Pirep.Weather.Should().NotBeNull();
        result.Pirep.Weather!.FlightVisibilitySm.Should().Be(3);
        result.Pirep.Weather!.Contractions.Should().ContainSingle().Which.Should().Be("R");
    }

    [Fact]
    public void Parses_turbulence_intensity()
    {
        var result = PirepParser.Parse("UA /OV BOS/TM 1200/FL100/TP C172/TB LGT-MOD CAT BLO-090");
        result.Pirep.Turbulence.Should().NotBeNull();
        result.Pirep.Turbulence!.Intensity.Should().Be(TurbulenceIntensity.LGT_MOD);
        result.Pirep.Turbulence!.Type.Should().Be(TurbulenceType.CAT);
        result.Pirep.Turbulence!.AltitudeBand.Should().Be("BLO-090");
    }

    [Fact]
    public void Parses_icing_intensity_and_type()
    {
        var result = PirepParser.Parse("UA /OV BOS/TM 1200/FL100/TP C172/IC SVR CLR 028-045");
        result.Pirep.Icing.Should().NotBeNull();
        result.Pirep.Icing!.Intensity.Should().Be(IcingIntensity.SVR);
        result.Pirep.Icing!.Type.Should().Be(IcingType.CLR);
        result.Pirep.Icing!.AltitudeBand.Should().Be("028-045");
    }

    [Fact]
    public void Captures_remarks_as_trailing_free_text()
    {
        var result = PirepParser.Parse("UA /OV BOS/TM 1200/FL100/TP C172/RM HVY RAIN");
        result.Pirep.Remarks.Should().Be("HVY RAIN");
    }

    [Fact]
    public void Parses_pdf_example_one_verbatim()
    {
        var result = PirepParser.Parse("UA /OV RFD 170030/TM 1315/FL160/TP PA60 /SK 025 OVC 095/180 OVC /TA -21/WV 270048");
        result.Warnings.Should().BeEmpty();
        result.Pirep.Location.Segments[0].Fix.Should().Be("RFD");
        result.Pirep.Time.Should().Be("1315");
        result.Pirep.FlightLevel.Should().Be("160");
        result.Pirep.AircraftType.Should().Be("PA60");
        result.Pirep.SkyCover.Should().HaveCount(2);
        result.Pirep.TemperatureC.Should().Be(-21);
        result.Pirep.WindDirection.Should().Be(270);
        result.Pirep.WindSpeedKt.Should().Be(48);
    }

    [Fact]
    public void Parses_pdf_example_two_verbatim()
    {
        var result = PirepParser.Parse("UA /OV DHT 360015-AMA-CDS/TM 2116/FL050/TP PA32 /SK UNKN OVC/WX FV03 R /TB LGT/TA 04/RM HVY RAIN");
        result.Warnings.Should().BeEmpty();
        result.Pirep.Location.Segments.Should().HaveCount(3);
        result.Pirep.Weather!.FlightVisibilitySm.Should().Be(3);
        result.Pirep.Turbulence!.Intensity.Should().Be(TurbulenceIntensity.LGT);
        result.Pirep.TemperatureC.Should().Be(4);
        result.Pirep.Remarks.Should().Be("HVY RAIN");
    }
}
