# Scripture preparation for Uno

`Elementary.ScriptureCompiler` runs automatically before Uno resources are assigned.
It uses the shared `UsfmParser` to prepare all 66 books in each of ASV, KJV and NET
from the existing UWP content. It checks each book's canonical chapter count.

The versioned, gzip-compressed binary resources contain the parsed verses,
footnotes, and the exact `ToDisplayLines()` output. The Uno reader and search use
these prepared lines, avoiding repeated USFM/HTML parsing in WebAssembly.

Generated files live in `src/Elementary.Uno/Elementary.Uno/obj/Scripture`; do not
commit them. The target reruns when its source scripture, parser/model, or compiler
changes. Normal `dotnet build` and `dotnet publish` include the resources. The tool
requires the same .NET 10 SDK as the Uno project; no extra packages are needed.
