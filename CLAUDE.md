# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```
dotnet build                                 # zero warnings expected; TreatWarningsAsErrors=true
dotnet run --project src/PirepEncoder        # launch the desktop app (Windows dev default)
dotnet test                                  # run all 42 xUnit tests
dotnet test --filter FullyQualifiedName~Pdf_example_one   # run a single test
```

Publish a single-file self-contained binary (RID required — csproj lists `win-x64;linux-x64;osx-arm64`, no default):

```
dotnet publish src/PirepEncoder -c Release -r <win-x64|linux-x64|osx-arm64> -o publish/<rid>
```

Avalonia XAML warnings (`AVLN####`) do **not** honor `TreatWarningsAsErrors`, so they'll slip through a "succeeded" build. Scan `dotnet build` output for them and fix at source.

## Architecture

Three layers with strict, enforced separation:

1. **`Models/`** — plain `record` types. No logic beyond defaults. Raw and structured forms coexist on `Pirep` (e.g. both `SkyCover` layers *and* `SkyCoverRaw` string) to support the UI's hybrid builder/raw toggle.

2. **`Services/`** — pure, framework-free functions. No Avalonia, no UI references. Anything that formats, parses, validates, or persists lives here and is fully unit-tested in isolation.
   - `PirepFormatter.Format(Pirep, AppSettings)` — canonical encoder. Per-field `Resolve*` helpers prefer the raw override string when set, else format from the structured model. The SA identifier prefix is applied here at format time only; it's never baked into the model's encoded form.
   - `PirepParser.Parse(string)` — tolerant decoder returning `(Pirep, IReadOnlyList<ParseWarning>)`. Splits on a regex of known two-letter codes (`OV TM FL TP SK WX TA WV TB IC RM`) so inner `/` separators inside `/SK` don't confuse it.
   - `PirepValidator.Validate(Pirep)` — per-field rules; returns errors keyed by field so the VM can surface inline messages.
   - `SettingsStore` / `DraftStore` — JSON persistence to `%APPDATA%/PirepEncoder/` (or platform equivalent via `SpecialFolder.ApplicationData`). `DraftStore` caps at 50 entries, newest first.

3. **`ViewModels/` + `Views/`** — Avalonia MVVM with CommunityToolkit.Mvvm source generators. `ViewLocator` maps `FooViewModel` → `FooView` by convention.

### The `PirepViewModel` change-propagation model

`EncodedOutput`, `Errors`, and `ErrorSummary` are **computed** properties, not backed fields. The VM subscribes to its own `PropertyChanged` event and, on any field change, calls `BubbleOutput()` which re-raises notifications for those three. Child collections (`Locations`, `CloudLayers`) and each child VM's `PropertyChanged` are also wired into `BubbleOutput()`. The `_loading` guard suppresses re-bubbling while `LoadFromModel` / `Reset` repopulate the form — otherwise every assignment would re-render the preview mid-load.

### Hybrid builder/raw toggle (for /SK /WX /TB /IC)

Each hybrid field has three VM properties:
- `IncludeX` — is this field in the output at all?
- `XUseRaw` — builder mode vs raw-text mode?
- Structured state (e.g. `CloudLayers`, `TurbulenceIntensity`) **and** `XRaw` string.

On the model side, `Pirep` holds both the structured form and a nullable `XRaw`. `PirepFormatter`'s `Resolve*` helpers prefer `XRaw` when set — so flipping the UI to raw mode just populates that string and the structured data is ignored. Flipping back to builder mode attempts to parse the raw; failure leaves the UI in raw mode with a hint.

## PIREP format gotchas (FAA Form 7110-2)

The formatter's behavior reflects these spec quirks — don't "fix" them without checking the spec:

- `/FL` is the **only** required field with no space between code and value: `/FL160`, `/FLUNKN`. Every other field has a space.
- There is **one** space between the required section (ends with `/TP <type>`) and the first included optional field. Subsequent optional fields join with no separator.
- Temperature below zero is hyphen-prefixed and two-digit padded: `/TA -06`.
- Wind is exactly six digits: `dddsss` (direction + speed kt). `/WV 270045` = 270° at 45 kt.
- `/OV` supports a single fix, fix + 6-digit radial/distance, or hyphen-joined chain: `RFD`, `RFD 170030`, `DHT 360015-AMA-CDS`.
- `/SK` layers are `/`-separated; each layer is `base cover [tops]` with 3-digit altitudes or `UNKN`.

The two FAA PDF examples have inconsistent inter-field whitespace (likely manual-entry artifacts). `FormatterTests.Pdf_example_*` assert against the **canonical** form our formatter emits, not the PDF text verbatim; `ParserTests.Parses_pdf_example_*` do accept the PDF's text and decode it. Don't chase char-for-char equality with the PDF.

## CI / releases

`.github/workflows/release.yml` fires on every push to `main`: matrix builds win-x64 / linux-x64 / osx-arm64, runs tests once on Linux, deletes the existing `latest` tag + release, then recreates `latest` as a prerelease with all three binaries attached. All GitHub Actions are pinned to commit SHAs — when bumping, use `gh api repos/<org>/<repo>/git/ref/tags/<tag>` to resolve a new SHA rather than switching to a moving tag.
