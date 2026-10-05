# Local WebAssembly Release preview

Publish from the repository root:

```powershell
dotnet publish src/Elementary.Uno/Elementary.Uno/Elementary.Uno.csproj -c Release -f net10.0-browserwasm
```

Release publishes enable ahead-of-time compilation. The first publish can take several minutes; subsequent publishes reuse compiler caches. Debug builds remain unchanged.

Serve the generated `src/Elementary.Uno/Elementary.Uno/bin/Release/net10.0-browserwasm/publish/wwwroot` directory with a static web server that supports WebAssembly MIME types and Brotli/gzip compression. Do not serve the repository root.

For an interpreter-only comparison, override the publish property:

```powershell
dotnet publish src/Elementary.Uno/Elementary.Uno/Elementary.Uno.csproj -c Release -f net10.0-browserwasm -p:RunAOTCompilation=false
```

AOT improves managed-code execution but can increase download size. Compare initial load separately from chapter navigation, scrolling, and full-Bible searches. Local hosting does not measure real-world network latency. Separate localhost ports have separate saved settings and reading history.
