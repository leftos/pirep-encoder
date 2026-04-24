using System;
using PirepEncoder.Services;

namespace PirepEncoder.ViewModels;

public sealed record DraftEntryViewModel(string Id, string Label, DateTimeOffset SavedAt)
{
    public string DisplayLabel => $"{SavedAt.LocalDateTime:yyyy-MM-dd HH:mm} — {Label}";

    public static DraftEntryViewModel From(DraftEntry entry)
        => new(entry.Id, entry.Label, entry.SavedAt);
}
