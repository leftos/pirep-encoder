using System.IO;
using System.Text.Json;
using PirepEncoder.Models;

namespace PirepEncoder.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public AppSettings Load()
    {
        if (!File.Exists(StorageLocations.SettingsPath))
        {
            return new AppSettings();
        }
        try
        {
            var json = File.ReadAllText(StorageLocations.SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        StorageLocations.EnsureRoot();
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(StorageLocations.SettingsPath, json);
    }
}
