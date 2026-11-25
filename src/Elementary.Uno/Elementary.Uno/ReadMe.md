# Getting Started

Welcome to the Uno Platform!

To discover how to get started with your new app: https://aka.platform.uno/get-started

For more information on how to use the Uno.Sdk or upgrade Uno Platform packages in your solution: https://aka.platform.uno/using-uno-sdk
## Running the Windows App SDK head

From the repository root, build and register the ARM64 Debug app on a Windows development machine with Developer Mode enabled.

```powershell
$project = 'src/Elementary.Uno/Elementary.Uno'
dotnet build "$project/Elementary.Uno.csproj" -c Debug -p:ElementaryWindowsHead=true -p:Platform=ARM64 -r win-arm64
Add-AppxPackage -Register "$project/bin/ARM64/Debug/net11.0-windows10.0.26100.0/win-arm64/AppxManifest.xml"
$package = Get-AppxPackage com.grantstreetdesign.elementary.uno.dev
Start-Process explorer.exe "shell:AppsFolder\$($package.PackageFamilyName)!App"
```

Subsequent launches are available from Start under **Elementary Uno Dev**. The Debug package has its own identity and settings. WebAssembly remains the default build target.
