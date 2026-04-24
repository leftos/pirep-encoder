# PIREP Encoder

A small Windows desktop app for air traffic controllers to encode and decode FAA Form 7110-2 pilot weather reports (PIREPs). Guided UI with dropdowns, inline per-field help, live preview of the encoded string, copy-to-clipboard, local draft persistence, and a paste-to-decode workflow.

Built with .NET 10 + Avalonia 12 (MVVM).

## Build & run (dev)

```
dotnet build
dotnet run --project src/PirepEncoder
```

## Tests

```
dotnet test
```

## Release: single-file Windows .exe

The main project is pre-configured for self-contained single-file publish to `win-x64`. One command produces a standalone `PirepEncoder.exe` with no .NET runtime prerequisite on the target machine:

```
dotnet publish src/PirepEncoder -c Release -o publish
```

Output: `publish/PirepEncoder.exe` (ready-to-run, compressed, all native libs embedded).

To target a different architecture:

```
dotnet publish src/PirepEncoder -c Release -r win-arm64 -o publish
```

## Field reference

See `src/PirepEncoder/Assets/field-help.json` for the per-field help text shown in the app, sourced from FAA Form 7110-2.
