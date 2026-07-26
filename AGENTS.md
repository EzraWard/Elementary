# Repository Guidelines

## Project Structure

Elementary is a Windows UWP Bible reading app. The main app lives in `src/Elementary.UWP`, shared logic is in `src/Elementary.Core`, Verse of the Day logic is in `src/Elementary.VerseOfTheDay`, and tests are in `src/Elementary.Tests`. The solution file is `src/Elementary.slnx`.

## Build, Test, and Review Commands

- `dotnet test src\Elementary.Tests\Elementary.Tests.csproj` runs the main test suite.
- Build the UWP app shell with Visual Studio MSBuild, not `dotnet build`. Example for this ARM64 dev machine:
  `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe" src\Elementary.UWP\Elementary.UWP.csproj /t:Build /p:Configuration=Debug /p:Platform=ARM64 /p:AppxPackageValidationEnabled=false /p:AppxBundle=Never`.
- When reviewing XAML changes against a loose debug install, stop the app and copy fresh XBF files into the registered `AppX` layout after MSBuild. MSBuild updates `bin\<platform>\Debug`, but the installed loose package may still point at `bin\<platform>\Debug\AppX`.

Example ARM64 refresh:

```powershell
Get-Process Elementary -ErrorAction SilentlyContinue | Stop-Process -Force
Copy-Item src\Elementary.UWP\bin\ARM64\Debug\App.xbf src\Elementary.UWP\bin\ARM64\Debug\AppX\App.xbf -Force
Copy-Item src\Elementary.UWP\bin\ARM64\Debug\Pages\*.xbf src\Elementary.UWP\bin\ARM64\Debug\AppX\Pages -Force
```

- Register the debug layout if needed:
  `Add-AppxPackage -Register "src\Elementary.UWP\bin\ARM64\Debug\AppX\AppxManifest.xml" -DisableDevelopmentMode`.

## Hygiene

Do not commit generated app packages, build outputs, logs, local databases, or Visual Studio state.
