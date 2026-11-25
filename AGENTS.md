# Repository Guidelines

## Project Structure

Elementary is a Windows UWP Bible reading app. The main app lives in `src/Elementary.UWP`, shared logic is in `src/Elementary.Core`, Verse of the Day logic is in `src/Elementary.VerseOfTheDay`, and tests are in `src/Elementary.Tests`. The solution file is `src/Elementary.slnx`.

## Build, Test, and Review Commands

Run the main suite with `dotnet test --project src\Elementary.Tests\Elementary.Tests.csproj`. Build the UWP projects with Visual Studio MSBuild and a concrete platform. Build the packaged test host before invoking Microsoft.Testing.Platform.

```powershell
$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe"
& $msbuild src\Elementary.slnx /restore /p:Configuration=Debug /p:Platform=ARM64
& $msbuild src\Elementary.Tests.UWP\Elementary.Tests.UWP.csproj /t:InvokeTestingPlatform /p:Platform=ARM64
```

Publish a Release UWP Native AOT package with Visual Studio MSBuild. Install the Visual C++ tools for the target architecture first. Use the development package identity for local deployment alongside the Store app.

```powershell
& $msbuild src\Elementary.UWP\Elementary.UWP.csproj /restore /t:Publish /p:Configuration=Release /p:Platform=ARM64 /p:ElementaryDevelopmentPackage=true /p:GenerateAppxPackageOnBuild=true /p:AppxPackageSigningEnabled=false /p:AppxBundle=Never
```

`src\Elementary.Packaging\Elementary.Packaging.wapproj` forwards to single-project MSIX packaging with the existing Elementary Dev package identity.

## Hygiene

Do not commit generated app packages, build outputs, logs, local databases, or Visual Studio state.
