# pirep-encoder — plan index

Open work only; a finished line is deleted in the commit that lands it.

## Backlog

- [ ] `KnownCodes` in `PirepParser.cs` is never read (the split uses `FieldCodeRegex`): delete it, or use it (found 2026-09-30 while drafting `docs/ARCHITECTURE.md`).
- [ ] CLAUDE.md used to say that switching a field from raw back to builder mode parses the raw text and, on failure, stays in raw mode with a hint; no view model does that (`OnSkyUseRawChanged` only pre-fills `SkyRaw` when switching to raw, and only sky has a switch handler). Decide whether the round trip is wanted; if so, build it, else nothing to do (found 2026-09-30).
