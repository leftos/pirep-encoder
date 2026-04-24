using FluentAssertions;
using PirepEncoder.Models;
using PirepEncoder.Services;

namespace PirepEncoder.Tests;

public class RoundTripTests
{
    [Theory]
    [InlineData("UA /OV BOS/TM 1200/FL100/TP C172")]
    [InlineData("UUA /OV BOS/TM 0915/FL095/TP UNKN")]
    [InlineData("UA /OV RFD 170030/TM 1315/FL160/TP PA60 /SK 025 OVC 095/180 OVC/TA -21/WV 270048")]
    [InlineData("UA /OV DHT 360015-AMA-CDS/TM 2116/FL050/TP PA32 /SK UNKN OVC/WX FV03 R/TA 04/TB LGT/RM HVY RAIN")]
    public void Decode_then_encode_is_stable(string canonical)
    {
        var first = PirepParser.Parse(canonical);
        first.Warnings.Should().BeEmpty();
        var reencoded = PirepFormatter.Format(first.Pirep, new AppSettings { PrefixWithSaIdentifier = false });
        reencoded.Should().Be(canonical);
    }
}
