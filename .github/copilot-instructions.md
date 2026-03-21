# Copilot Instructions for Elementary

## Build, test, and lint commands

- Prerequisites (from README): Windows 10 v1809+, Visual Studio 2022, Windows 11 SDK `10.0.26100.0`.
- Build shared core library:
  - `dotnet build .\src\Elementary.Core\Elementary.Core.csproj --nologo`
- Run .NET unit tests:
  - `dotnet test .\src\Elementary.Tests\Elementary.Tests.csproj --nologo`
- Run a single MSTest:
  - `dotnet test .\src\Elementary.Tests\Elementary.Tests.csproj --nologo --filter "FullyQualifiedName~UsfmParserTests.ParseBook_ShouldExtractTitleAndChapters"`
- UWP app/tests (`src\Elementary.UWP`, `src\Elementary.Tests.UWP`) are classic UWP projects and are typically built/run from Visual Studio with x86/x64/ARM64 configs.
- Lint: there is no dedicated lint command or analyzer script configured in this repository.

## High-level architecture

- `src\Elementary.slnx` currently wires four projects: `Elementary.Core`, `Elementary.UWP`, `Elementary.Tests`, and `Elementary.Tests.UWP`.
- `Elementary.Core` (`netstandard2.0`) contains domain logic and contracts:
  - Interfaces (`IBibleService`, `ISettingsService`, `IFileService`, providers)
  - Services (`BibleService`, `SettingsService`, `FileService`, `VerseOfTheDayService`)
  - Parsing/model logic for Bible content (USFM parser + models/enums/dictionaries)
- `Elementary.UWP` is the app shell and UI layer (XAML pages + viewmodels). Dependency injection is composed in `App.Configuration.xaml.cs`, where platform implementations (`WindowsSettingsProvider`, `WindowsFilePathProvider`, `UWPFileService`) are registered and core services are resolved.
- Data flow is: page/viewmodel -> core service interfaces -> platform providers/file services. `BibleService` loads translation content (USFM/EPUB paths), and `SettingsService` persists reading state/theme/font/navigation history.
- `Elementary.Tests` (`net8.0`, MSTest + Moq) covers core behavior. `Elementary.Tests.UWP` contains UWP-specific tests for Windows providers.

## Key repository conventions

- Use dependency injection via `App.Services.GetRequiredService<T>()` from UI/viewmodels; avoid direct `new` for services that already have interfaces/registrations.
- Persist user state through `ISettingsService`/`ISettingsProvider` using established keys (`translation`, `book`, `chapter`, `font`, `fontSize`, `showVerseNumbers`, `theme`, `navigationHistory`) instead of adding ad-hoc storage paths.
- Navigation history format is string-serialized as `BookTitle|Chapter` entries joined by `;` and capped to 10 items (`SettingsService`); keep this format compatible.
- Bible loading is intentionally staged: books are listed first and chapter content is loaded lazily via `EnsureBookLoaded`; preserve this behavior for performance.
- Tests use MSTest attributes and method names in `Method_Condition_ExpectedResult` style; follow existing patterns in `src\Elementary.Tests\*.cs`.
