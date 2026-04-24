namespace PirepEncoder.Models;

public sealed record AppSettings
{
    public string? DefaultSaIdentifier { get; init; }

    public bool PrefixWithSaIdentifier { get; init; } = true;

    public string? DefaultAircraftType { get; init; }
}
