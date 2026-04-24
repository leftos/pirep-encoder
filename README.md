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

## Release: single-file binaries

The main project is pre-configured for self-contained, compressed, single-file publish. Each invocation produces a standalone executable with no .NET runtime prerequisite on the target machine.

```
dotnet publish src/PirepEncoder -c Release -r win-x64   -o publish/win-x64
dotnet publish src/PirepEncoder -c Release -r linux-x64 -o publish/linux-x64
dotnet publish src/PirepEncoder -c Release -r osx-arm64 -o publish/osx-arm64
```

Each output directory contains a single binary (`PirepEncoder.exe` on Windows, `PirepEncoder` elsewhere).

### Rolling "latest" release on GitHub

Pushes to `main` trigger the `.github/workflows/release.yml` workflow, which builds all four RIDs above, replaces the `latest` tag/prerelease on GitHub, and attaches the binaries. The rolling release is always downloadable from:

```
https://github.com/leftos/pirep-encoder/releases/tag/latest
```

## Field reference

Per-field format rules, valid values, and examples are surfaced as inline tooltips on every field in the main window, sourced from FAA Form 7110-2.
