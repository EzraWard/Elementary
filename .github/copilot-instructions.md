# Copilot Instructions for Elementary

## Build, test, and lint commands

- Prerequisites from `README.md`: Windows 10 v1809+, Visual Studio 2022, Windows 11 SDK `10.0.26100.0`.
- Build the core library:
  - `dotnet build .\src\Elementary.Core\Elementary.Core.csproj --nologo`
- Build the main test project:
  - `dotnet build .\src\Elementary.Tests\Elementary.Tests.csproj --nologo`
- Run the unit tests:
  - `dotnet test .\src\Elementary.Tests\Elementary.Tests.csproj --nologo`
- Run one test method:
  - `dotnet test .\src\Elementary.Tests\Elementary.Tests.csproj --nologo --filter "FullyQualifiedName~UsfmParserTests.ParseBook_ShouldExtractTitleAndChapters"`
- UWP projects (`src\Elementary.UWP`, `src\Elementary.Tests.UWP`) are classic UWP projects and are typically built/run from Visual Studio with x86/x64/ARM64 configs.
- No dedicated lint/analyzer script is configured.

## High-level architecture

- `src\Elementary.slnx` includes `Elementary.Core`, `Elementary.VerseOfTheDay`, `Elementary.WidgetApp`, `Elementary.Packaging`, `Elementary.Tests.UWP`, `Elementary.Tests`, `Elementary.VerseOfTheDay.ConsolePreview`, and `Elementary.UWP`.
- `Elementary.Core` (`netstandard2.0`) holds the Bible domain model, parsing, and shared services: settings, file access, Bible loading, search, and reading streak logic.
- `Elementary.UWP` is the main app shell. XAML pages and viewmodels live here, and `App.Configuration.xaml.cs` is the composition root that registers platform services and shared core services.
- `Elementary.VerseOfTheDay` is the shared Verse-of-the-Day library used by the widget app, console preview, and UWP app. `Elementary.WidgetApp` is the Windows App SDK entry point; `Elementary.VerseOfTheDay.ConsolePreview` is a .NET console app for preview/debugging.
- `Elementary.Tests` covers core behavior with MSTest + Moq, and `Elementary.Tests.UWP` covers Windows-specific provider behavior.

## Key repository conventions

- Resolve UI/viewmodel dependencies through `App.Services.GetRequiredService<T>()`; avoid `new` for services that are already registered in DI.
- Keep settings compatibility intact. Existing keys include `translation`, `book`, `chapter`, `font`, `fontSize`, `showVerseNumbers`, `theme`, `navigationHistory`, and `readingStreak`.
- Preserve serialized formats: navigation history is `BookTitle|Chapter[|BookKey]` entries joined by `;`, capped to 10 items; reading streak data is stored as `yyyyMMdd` values plus per-day reading seconds.
- Bible content loading is intentionally staged: books are discovered first, then chapter content is loaded lazily with `EnsureBookLoaded`. Keep that behavior to protect startup and navigation performance.
- When touching `SettingsService`, remember it fills in defaults and persists them immediately on read.
- Test naming follows MSTest with `Method_Condition_ExpectedResult`-style method names. `Elementary.Tests` also imports MSTest via MSBuild `<Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />`, so test files usually do not need a `using` for it.
- `Elementary.VerseOfTheDay\ApiKeys.cs` is ignored; edit `ApiKeys.Template.cs` instead.
