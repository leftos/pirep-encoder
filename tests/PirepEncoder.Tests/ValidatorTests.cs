using System.Linq;
using FluentAssertions;
using PirepEncoder.Models;
using PirepEncoder.Services;

namespace PirepEncoder.Tests;

public class ValidatorTests
{
    [Fact]
    public void Valid_minimal_pirep_has_no_errors()
    {
        var pirep = new Pirep
        {
            Location = new Location { Segments = [new LocationSegment { Fix = "BOS" }] },
            Time = "1200",
            FlightLevel = "100",
            AircraftType = "C172",
        };
        PirepValidator.Validate(pirep).Should().BeEmpty();
    }

    [Fact]
    public void Missing_location_is_reported()
    {
        var pirep = new Pirep { Time = "1200", FlightLevel = "100", AircraftType = "C172" };
        PirepValidator.Validate(pirep).Should().Contain(e => e.Field == "OV");
    }

    [Theory]
    [InlineData("xx")]
    [InlineData("12AB")]
    [InlineData("")]
    public void Invalid_time_is_reported(string time)
    {
        var pirep = new Pirep
        {
            Location = new Location { Segments = [new LocationSegment { Fix = "BOS" }] },
            Time = time,
            FlightLevel = "100",
            AircraftType = "C172",
        };
        PirepValidator.Validate(pirep).Should().Contain(e => e.Field == "TM");
    }

    [Fact]
    public void Time_out_of_range_is_reported()
    {
        var pirep = new Pirep
        {
            Location = new Location { Segments = [new LocationSegment { Fix = "BOS" }] },
            Time = "2460",
            FlightLevel = "100",
            AircraftType = "C172",
        };
        PirepValidator.Validate(pirep).Should().Contain(e => e.Field == "TM");
    }

    [Theory]
    [InlineData("1600")]
    [InlineData("XX")]
    [InlineData("")]
    public void Invalid_flight_level_is_reported(string fl)
    {
        var pirep = new Pirep
        {
            Location = new Location { Segments = [new LocationSegment { Fix = "BOS" }] },
            Time = "1200",
            FlightLevel = fl,
            AircraftType = "C172",
        };
        PirepValidator.Validate(pirep).Should().Contain(e => e.Field == "FL");
    }

    [Fact]
    public void Radial_distance_must_be_six_digits()
    {
        var pirep = new Pirep
        {
            Location = new Location { Segments = [new LocationSegment { Fix = "BOS", RadialDistance = "12345" }] },
            Time = "1200",
            FlightLevel = "100",
            AircraftType = "C172",
        };
        PirepValidator.Validate(pirep).Should().Contain(e => e.Field == "OV");
    }

    [Fact]
    public void Wind_requires_both_direction_and_speed()
    {
        var pirep = new Pirep
        {
            Location = new Location { Segments = [new LocationSegment { Fix = "BOS" }] },
            Time = "1200",
            FlightLevel = "100",
            AircraftType = "C172",
            WindDirection = 270,
        };
        PirepValidator.Validate(pirep).Should().Contain(e => e.Field == "WV");
    }
}
