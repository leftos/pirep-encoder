using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PirepEncoder.Models;

namespace PirepEncoder.Services;

public sealed record DraftEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;
    public string Label { get; init; } = "";
    public Pirep Pirep { get; init; } = new();
}

public sealed class DraftStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public const int MaxDrafts = 50;

    public IReadOnlyList<DraftEntry> LoadAll()
    {
        if (!File.Exists(StorageLocations.DraftsPath))
        {
            return [];
        }
        try
        {
            var json = File.ReadAllText(StorageLocations.DraftsPath);
            var list = JsonSerializer.Deserialize<List<DraftEntry>>(json, SerializerOptions);
            return list ?? new List<DraftEntry>();
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    public DraftEntry Save(Pirep pirep, string? label = null)
    {
        var drafts = LoadAll().ToList();
        var entry = new DraftEntry
        {
            Pirep = pirep,
            Label = string.IsNullOrWhiteSpace(label) ? AutoLabel(pirep) : label.Trim(),
        };
        drafts.Insert(0, entry);
        if (drafts.Count > MaxDrafts)
        {
            drafts = drafts.Take(MaxDrafts).ToList();
        }
        Write(drafts);
        return entry;
    }

    public void Delete(string id)
    {
        var drafts = LoadAll().Where(d => d.Id != id).ToList();
        Write(drafts);
    }

    private static void Write(IReadOnlyList<DraftEntry> drafts)
    {
        StorageLocations.EnsureRoot();
        var json = JsonSerializer.Serialize(drafts, SerializerOptions);
        File.WriteAllText(StorageLocations.DraftsPath, json);
    }

    private static string AutoLabel(Pirep pirep)
    {
        var fix = pirep.Location.Segments.FirstOrDefault()?.Fix ?? "----";
        var time = string.IsNullOrWhiteSpace(pirep.Time) ? "----" : pirep.Time;
        var said = string.IsNullOrWhiteSpace(pirep.SaIdentifier) ? "" : pirep.SaIdentifier + " ";
        return $"{said}{time} {fix}".Trim();
    }
}
