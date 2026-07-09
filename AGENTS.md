# AGENTS.md — Mexico-Postal-Codes

## Build & verify

```sh
dotnet build                        # builds solution (0 warnings expected)
```

No test, lint, or typecheck commands exist. Compiler enforces strictness at build time.

## Project structure

| Path | Target | Kind |
|---|---|---|
| `Mexico-Postal-Code/` | `net10.0` | CLI app (Spectre.Console 0.55.2) |
| `Mexico-Postal-Codes.Core/` | `netstandard2.0` | Reusable library (zero external deps) |

Solution file: `Mexico-Postal-Code.slnx`

## Language constraints

VB.NET with `Option Strict On`, `Option Infer Off`, `Option Explicit On` across all files. Every `Dim` must declare an explicit type — no type inference.

## Architecture

**`PostalCodeService`** is the single public entry point (stateful). Usage pattern:

```vbnet
Dim service As New PostalCodeService()
service.LoadOrDownload()             ' loads from cache or SEPOMEX
service.Search("query")              ' throws if not loaded
service.ExportToJson("path.json")   ' throws if not loaded
service.PostalCodes                  ' IReadOnlyList(Of PostalCodeEntry)
service.Refresh()                    ' force re-download
```

- `PostalCodeService` keeps data internally (`_postalCodes`). Caller does not manage the list.
- `Search()` and `ExportTo*()` throw `InvalidOperationException` if `Not IsLoaded`.
- Async variants (`LoadOrDownloadAsync`, `RefreshAsync`) accept optional `CancellationToken`.
- All `Await` use `.ConfigureAwait(False)` — intentional deadlock prevention for library consumers.
- Sync wrappers use `.GetAwaiter().GetResult()` (not `.Result` or `.Wait()`).

## Classes moved out of Core library

| Class | Now lives in |
|---|---|
| `PostalCodeStatistics` | CLI project (`Mexico-Postal-Code/`) — presentation logic, not domain |
| `PostalCodeEntry` (was `c_PostalCode`) | Core library — renamed, no Hungarian notation |

## Internal (Friend) classes

`PostalCodeExporter`, `Scraper` (was `c_Scraper`), `HtmlHelper`, `ZipExtractor`, `PostalCodeParser` are `Friend` — only `PostalCodeService` should call them.

## SEPOMEX scraping quirks

- The scraper simulates ASP.NET WebForms POST: extracts `__VIEWSTATE`, `__EVENTVALIDATION`, etc. from the page, generates random button click coordinates (X: 2–71, Y: 2–21), and POSTs back.
- SEPOMEX returns a ZIP with `Content-Disposition`. The ZIP is extracted and the inner TXT is renamed to `CPdescarga.txt` for consistent caching.
- The downloaded ZIP is **deleted** after extraction.
- Source data encoding: ISO-8859-1 (Latin-1), pipe-delimited (`|`), 15 columns, 2 header lines skipped.

## Caching

| Path | Purpose |
|---|---|
| `{AppData}/MexicoPostalCodes/postal_codes/CPdescarga.txt` | Cached parsed database |
| `{AppData}/MexicoPostalCodes/exports/` | Export output directory |
| `{AppData}/MexicoPostalCodes/MexicoPostalCodes.log` | Log file |

Working directory is `AppData` on Windows, `~/.local/share` on Linux. Override via `PostalCodeService(workingDirectory:=...)`.

## README is stale

The `README.md` references old class names (`c_PostalCode`, `PostalCodeQuery`, `PostalCodeStatistics` in API table) and outdated usage patterns. Do not rely on it for API guidance — use `AGENTS.md` or read source files.
