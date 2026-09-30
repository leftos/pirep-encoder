# PIREP Encoder — architecture

A single-project Avalonia 12 / .NET 10 desktop app that encodes and decodes FAA Form 7110-2 pilot weather reports (PIREPs). Three layers in one assembly (`src/PirepEncoder/`): `Models/` (records), `Services/` (pure encode, decode, validate and persist logic) and `ViewModels/` + `Views/` (MVVM). The rule that shapes it: `Services/` has no Avalonia or UI references, so everything that formats, parses or validates is unit-tested in isolation. The repo has no glossary yet; `CLAUDE.md` carries the format gotchas.

## Task Index

| Task | Files, in order | Deep doc |
|---|---|---|
| Change how a field is encoded (spacing, padding, order) | `src/PirepEncoder/Services/PirepFormatter.cs` → the field's `*Formatter.cs` beside it → `tests/PirepEncoder.Tests/FormatterTests.cs` | [`CLAUDE.md`](../CLAUDE.md) (format gotchas) |
| Change how a pasted PIREP is decoded | `src/PirepEncoder/Services/PirepParser.cs` → `tests/PirepEncoder.Tests/ParserTests.cs` → `tests/PirepEncoder.Tests/RoundTripTests.cs` | [`CLAUDE.md`](../CLAUDE.md) |
| Change a validation rule | `src/PirepEncoder/Services/PirepValidator.cs` → `tests/PirepEncoder.Tests/ValidatorTests.cs` | none |
| Add a field to the PIREP | `src/PirepEncoder/Models/Pirep.cs` → `Services/PirepFormatter.cs` → `Services/PirepParser.cs` → `Services/PirepValidator.cs` → `ViewModels/PirepViewModel.cs` (`ToModel` and `LoadFromModel`) → `Views/MainWindow.axaml` → tests in all four test files | [Layers](#layers) (hybrid builder/raw fields) |
| Add a structured sub-field to /SK /WX /TB /IC | the matching `Models/*.cs` (`CloudLayer.cs`, `SkyCover.cs`, `Weather.cs`, `Turbulence.cs`, `Icing.cs`) → its `Services/*Formatter.cs` → the `TryParse*` method in `Services/PirepParser.cs` → `ViewModels/PirepViewModel.cs` | [`CLAUDE.md`](../CLAUDE.md) |
| Change the location (/OV) format | `src/PirepEncoder/Models/Location.cs` → `Services/LocationFormatter.cs` → `ParseLocation` in `Services/PirepParser.cs` → `ViewModels/LocationSegmentViewModel.cs` | [`CLAUDE.md`](../CLAUDE.md) |
| Change a form control, tooltip or layout | `src/PirepEncoder/Views/MainWindow.axaml` → `ViewModels/PirepViewModel.cs` | none |
| Add or change an app setting | `src/PirepEncoder/Models/AppSettings.cs` → `ViewModels/SettingsViewModel.cs` (`ToModel`) → `Views/SettingsWindow.axaml` → `Services/SettingsStore.cs` | none |
| Change draft saving, loading or the 50-draft cap | `src/PirepEncoder/Services/DraftStore.cs` → `ViewModels/DraftEntryViewModel.cs` → `ViewModels/MainWindowViewModel.cs` | none |
| Change the release workflow or targeted platforms | `.github/workflows/release.yml` → `src/PirepEncoder/PirepEncoder.csproj` (`RuntimeIdentifiers`) | [`CLAUDE.md`](../CLAUDE.md) (CI / releases) |

## Layers

- **`Models/`** (`src/PirepEncoder/Models/`): plain records (`Pirep`, `Location`, `CloudLayer`, `Weather`, `Turbulence`, `Icing`, `AppSettings`) and enums (`ReportType`, `SkyCover`). No logic beyond defaults. `Pirep` holds both the structured form and a nullable `*Raw` string for /SK /WX /TB /IC. References nothing.
- **`Services/`** (`src/PirepEncoder/Services/`): pure, framework-free functions. `PirepFormatter` (encode), `PirepParser` (tolerant decode returning `ParseResult` with `ParseWarning`s), `PirepValidator` (errors keyed by field), per-field formatters, and JSON persistence in `SettingsStore` and `DraftStore` (paths in `StorageLocations`, under the user's ApplicationData folder; `DraftStore.MaxDrafts` caps drafts at 50, newest first). References `Models/`; never Avalonia or `ViewModels/`.
  - `PirepFormatter`'s `ResolveSky`, `ResolveWeather`, `ResolveTurbulence` and `ResolveIcing` prefer the trimmed `*Raw` string when it is set, else format from the structured model. The SA identifier prefix (`AppSettings.PrefixWithSaIdentifier`) is applied in `PirepFormatter.Format` only; it is never stored in the `Pirep`.
  - `PirepParser` splits the input on `FieldCodeRegex`, a regex of the known two-letter field codes, so the inner `/` separators inside `/SK` do not start a new field.
- **`ViewModels/`** (`src/PirepEncoder/ViewModels/`): CommunityToolkit.Mvvm view models. `PirepViewModel` owns the form state and exposes `ToModel()` / `LoadFromModel()`; `EncodedOutput`, `Errors` and `ErrorSummary` are computed from `ToModel()` through `Services/`. `MainWindowViewModel` wires drafts, settings and paste-to-decode. References `Models/` and `Services/`.
  - Change propagation: `PirepViewModel` handles its own `PropertyChanged` (`OnAnyPropertyChanged`) and calls `BubbleOutput()`, which re-raises the three computed properties. The `Locations` and `CloudLayers` collections and each child view model's `PropertyChanged` feed the same call. The `_loading` guard suppresses it while `LoadFromModel` and `Reset` repopulate the form.
  - Hybrid builder/raw fields (/SK /WX /TB /IC): each has an `IncludeX` flag (in the output at all), an `XUseRaw` flag (builder or raw text), the structured state and an `XRaw` string. `ToModel()` fills only `XRaw` on the `Pirep` when `XUseRaw` is set, else the structured form. Switching sky to raw pre-fills `SkyRaw` from the current cloud layers.
- **`Views/`** and `ViewLocator.cs` (`src/PirepEncoder/`): Avalonia XAML windows (`MainWindow`, `SettingsWindow`). `ViewLocator` maps `FooViewModel` to `FooView` by replacing the name in the type's full name. References `ViewModels/`.
- **`tests/PirepEncoder.Tests/`**: xUnit + FluentAssertions, references the app project. Covers `Services/` only.

## Integration Footguns

- **Add or rename a `Pirep` property** → also update `PirepViewModel.ToModel()` and `PirepViewModel.LoadFromModel()`, the formatter, parser and validator. Nothing but `RoundTripTests` and manual use catches a field dropped from one direction.
- **Change the shape of `Pirep` or its child records** → drafts saved in `drafts.json` are `DraftEntry` records serialized with `System.Text.Json` (`DraftStore`), so a renamed or retyped property changes how existing drafts load. No test covers this.
- **Change `PirepViewModel` field properties** → the model is rebuilt from the view model on every property change (`EncodedOutput` calls `ToModel()`); a new property that does not raise `PropertyChanged` never refreshes the preview. Repopulating from a model runs under the `_loading` guard and ends with one `BubbleOutput()`.
- **Add a field code** → the decode split regex `FieldCodeRegex` in `PirepParser.cs` lists the two-letter codes; a code missing there is not split out of the input. The `KnownCodes` array beside it is not read anywhere.
- **Change canonical whitespace in the formatter** → `FormatterTests.Pdf_example_*` assert the canonical form, not the FAA PDF text; the parser tests accept the PDF text. See the gotchas in [`CLAUDE.md`](../CLAUDE.md).
- **Rename a `*ViewModel` type without renaming its view** → `ViewLocator` finds views by name and falls back to a "Not Found" text block at run time; no build check.
- **Avalonia XAML warnings (`AVLN####`)** do not honor `TreatWarningsAsErrors`; scan `dotnet build` output for them.

## Test locations

- `tests/PirepEncoder.Tests/FormatterTests.cs`: canonical output of `PirepFormatter`, including the two FAA PDF examples.
- `tests/PirepEncoder.Tests/ParserTests.cs`: `PirepParser` decoding and warnings.
- `tests/PirepEncoder.Tests/ValidatorTests.cs`: `PirepValidator` rules.
- `tests/PirepEncoder.Tests/RoundTripTests.cs`: decode then encode is stable for canonical strings.
- No tests exist for `ViewModels/`, `Views/`, `SettingsStore` or `DraftStore`. A new service test goes in the file for its service; a new file only for a new service.

## Deep docs

- [`CLAUDE.md`](../CLAUDE.md): commands, FAA format gotchas, CI and releases.
- [`README.md`](../README.md): build, test and single-file publish commands, the rolling `latest` release.
