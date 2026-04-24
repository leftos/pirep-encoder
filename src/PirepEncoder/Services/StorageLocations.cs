using System;
using System.IO;

namespace PirepEncoder.Services;

public static class StorageLocations
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PirepEncoder");

    public static string SettingsPath { get; } = Path.Combine(RootDirectory, "settings.json");

    public static string DraftsPath { get; } = Path.Combine(RootDirectory, "drafts.json");

    public static void EnsureRoot()
    {
        Directory.CreateDirectory(RootDirectory);
    }
}
