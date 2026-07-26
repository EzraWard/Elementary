# Elementary 1.0 release-readiness review

Review date: 2026-07-25  
Reviewed commit: `531a01c` plus the existing local working-tree changes  
Verdict: **the core reader is promising and testable, but the current package is not ready for a production 1.0 submission.**

## What is already in good shape

- The reader has a clear value proposition: focused offline reading, three translations, search, history, typography controls, themes, and private on-device streaks.
- The main portable test suite passes: 69 passed, 0 failed, 0 skipped.
- Collected coverage is 82.14% line coverage (1,063/1,294) and 61.84% branch coverage (282/456).
- ARM64 Debug, x86 Debug, and x86 Release builds completed successfully when the installed Windows SDK version was supplied as an MSBuild override.
- The x86 Release build completed the .NET Native toolchain and produced a signed test MSIX and MSIX upload artifact.
- The portable/test and WidgetApp package audits reported no known vulnerable packages from the configured feeds.
- The current UWP image family has the expected dimensions and transparent backgrounds, including a usable 300×300 Store tile image.
- The new verse-number styling has been built and checked in the running app: numbers remain smaller and quieter without being pulled above the verse line.
- The first chapter now sits closer to its book title while later chapter spacing remains unchanged.
- The Bible page's top fade now refreshes correctly when navigating back to the cached page after switching in either direction between light and dark themes.

## Stop-ship issues

### 1. Verse of the Day crashes the app

Opening Verse of the Day reproducibly closes the app after the dialog begins loading. Windows Error Reporting recorded an Application Error in `combase.dll` with exception `0xc000027b`; the underlying HRESULT maps to `E_ASYNC_OPERATION_NOT_STARTED`.

The riskiest path starts an unobserved image-load task and disposes the backing random-access stream around `BitmapImage.SetSourceAsync`:

- `src/Elementary.UWP/Services/VerseOfTheDayDialogService.cs:71`
- `src/Elementary.UWP/Services/VerseOfTheDayDialogService.cs:86`
- `src/Elementary.UWP/Pages/VerseOfTheDayPage.xaml.cs:66`

For 1.0, either remove/feature-flag Verse of the Day or fix it before submission. The fix should await the full load lifecycle, avoid fire-and-forget work, keep the WinRT stream alive for as long as decoding requires, support cancellation when the dialog closes, and capture actionable failure telemetry. Exercise the result on x86, x64, and ARM64, online and offline.

Microsoft Store policy 10.4.2 requires apps to remain responsive and not close unexpectedly:  
https://learn.microsoft.com/en-us/windows/apps/publish/store-policies

### 2. A live Unsplash API credential is tracked in source

`src/Elementary.VerseOfTheDay/ApiKeys.cs` is tracked even though its template says it must never be committed. The credential was not copied into this report.

Before any public release:

1. Revoke and rotate the key.
2. Remove the secret from the current tree and repository history.
3. Add an automated secret scan to CI.
4. Do not ship a privileged secret in a client package. Prefer packaged/public-domain artwork, a controlled backend, or a provider flow designed for public client credentials.

### 3. The current Unsplash flow does not meet the published API requirements

The app downloads photo bytes and composes/caches a derivative. The dialog does not present linked attribution, and the model does not retain the photographer profile URL or `download_location`.

Unsplash currently requires:

- direct use of returned image URLs (hotlinking);
- notice for download or comparable events;
- attribution to Unsplash and the photographer with a link to the photographer profile; and
- a published privacy policy for every developer app.

References:

- https://unsplash.com/api-terms
- https://unsplash.com/documentation

The lowest-risk 1.0 choice is to ship without Unsplash-backed artwork. Revisit the feature only after the data flow, attribution UI, caching, privacy disclosure, and API-event reporting are compliant.

### 4. Verify the right to redistribute the bundled NET Bible electronically

The package includes the complete NET text and the Settings page contains an acknowledgement, but the repository does not contain evidence of distribution permission. Bible.org’s copyright page says its electronic material cannot be duplicated electronically without written permission and points NET Bible users to its current copyright terms.

Reference: https://bible.org/article/copyright-and-trademark-information

Retain the applicable written license/permission and the exact source/version in release records. If that permission is not available, do not ship the NET files in 1.0. Also add a complete third-party notices document for NET, KJV, ASV, fonts, libraries, and any remote content provider. This is a release-management recommendation, not legal advice.

### 5. Production identity and package metadata are unfinished

Both manifests still identify version `0.10.0.0` and display `Elementary Dev`:

- `src/Elementary.UWP/Package.appxmanifest:12`
- `src/Elementary.UWP/Package.appxmanifest:17`
- `src/Elementary.Packaging/Package.appxmanifest:18`
- `src/Elementary.Packaging/Package.appxmanifest:23`

Before certification:

- set the package version to `1.0.0.0`;
- use the final reserved Store/product name consistently in both manifests and Partner Center;
- remove “Dev” descriptions;
- preserve the Partner Center identity name/publisher rather than inventing a new identity;
- make the supported OS floor and `MaxVersionTested` consistent;
- rationalize the packaging manifest’s overlapping Universal and Desktop device-family declarations; and
- confirm which project is the sole source of the Store upload package.

The UWP manifest currently claims `10.0.0.0`, the project minimum is `10.0.17763.0`, and the packaging manifest also declares Desktop `10.0.19041.0`.

### 6. The packaging project can ship stale branding

`src/Elementary.Packaging/Images` contains a small older asset set, while `src/Elementary.UWP/Assets` contains the newer complete scale/target-size family. Consolidate the source of truth and inspect the final MSIX contents before upload.

The packaging project references only the UWP project. It does not reference or register `Elementary.WidgetApp`. Do not advertise the widget in 1.0 unless it is intentionally packaged, registered, tested, and accepted by certification. The widget provider also has a placeholder-looking COM GUID and a cache-hit path where `result` can remain null while its image is reused.

## High-priority product work

### Accessibility

A UI Automation review found:

- the book and chapter combo boxes have no accessible names;
- the Search close button has no accessible name;
- reader list items expose `Elementary.ViewModels.BibleReaderItem` rather than meaningful scripture text;
- the invisible `InitialFocusStealer` appears in the automation/focus tree; and
- Verse of the Day imagery lacks a complete accessible attribution/link experience.

Add `AutomationProperties.Name`, headings, sensible focus order, and useful item names. Then test with Narrator, keyboard only, 200% text scaling, high contrast, and a narrow window.

### Reliability and lifecycle

- `ContentFrame_NavigationFailed` is empty, while the app-level handler throws a generic exception.
- Suspension/state-save methods still contain template TODOs.
- `ThemeListener` is created as a local variable and its lifetime is not explicit.
- Verse of the Day uses UTC date keys while user-facing dates and streaks use local time.
- `VotdCacheService` can run duplicate factories for concurrent misses and uses a synchronous wait in invalidation.
- Remote calls need explicit timeouts, cancellation, retry/backoff limits, and persisted last-known-good fallback behavior.
- Add a release-safe crash/error reporting strategy that does not collect reading content.

### Search

Full-Bible search loads and walks every book, chapter, and display line (`src/Elementary.Core/Services/SearchService.cs:32-74`) and has no cancellation token or result limit. Add debounce/cancellation, cap or virtualize results, and consider a background index. Keep the UI responsive during a cold search.

### Accessibility-friendly reading polish

The reader is visually strong. Continue validating:

- verse-number contrast after the new 72% opacity treatment;
- large text and Georgia at all supported sizes;
- hanging indents for wrapped verses;
- poetry/heading semantics; and
- chapter positioning at the top overlay, especially when moving to a non-first chapter.

## Release engineering

- The projects pin Windows SDK `10.0.26100.0`, but this machine has `10.0.28000.0`. Builds succeeded with `/p:TargetPlatformVersion=10.0.28000.0`. Either install 26100 in the build environment or deliberately update and test the target; avoid relying on an undocumented local override.
- Add CI for restore, the portable tests, coverage, secret scanning, dependency auditing, and x86/x64/ARM64 package builds.
- Run the Windows App Certification Kit against the exact final upload package. The kit was not installed on this review machine.
- Run clean-install, upgrade, uninstall, offline, no-network, narrow-window, high-DPI, multi-monitor, sleep/resume, and architecture smoke tests.
- Exercise the UWP test project or replace its important coverage with maintainable integration/UI tests. The portable suite does not cover UWP lifecycle, XAML, packaging, or COM/widget behavior.
- Update `CHANGELOG.md`, which currently stops at 0.8.0, and add a 1.0 release checklist, privacy policy URL, support URL, and third-party notices.
- Update the test packages in a separate change. The scan found minor updates for the test SDK/MSTest/coverlet stack and a major SkiaSharp upgrade that should be evaluated deliberately, not slipped into the release candidate.

## Recommended 1.0 scope

Ship a smaller, dependable 1.0:

- offline NET/KJV/ASV reading only after translation rights are documented;
- full-Bible search;
- history;
- light/dark/system themes;
- font, size, and verse-number controls;
- local streaks.

Defer Verse of the Day, live tiles, and widgets unless their crash, licensing, privacy, packaging, attribution, and architecture-matrix work is completed.

## Final submission gate

- [ ] Verse of the Day fixed and fully compliant, or removed from the package/navigation.
- [ ] Unsplash key revoked, rotated, and removed from repository history.
- [ ] Translation redistribution permissions and third-party notices documented.
- [ ] Production manifest name/version/OS declarations finalized in one packaging path.
- [ ] Final icon family verified inside the upload package.
- [ ] Accessibility issues fixed and Narrator/keyboard/high-contrast tests passed.
- [ ] x86, x64, and ARM64 Release packages smoke-tested.
- [ ] Final package passes Windows App Certification Kit.
- [ ] Privacy and support URLs are live.
- [ ] Store listing claims only features included in the submitted package.
- [ ] Clean-install and upgrade-from-0.10 tests pass without data loss.
