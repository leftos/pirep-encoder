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

The architecture entry point is [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): Task Index, layers, integration footguns, test locations and the deep docs.

Never bake the SA identifier prefix into the `Pirep` model; `PirepFormatter.Format` applies it at format time only.

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
