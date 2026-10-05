# Uno / UWP functionality comparison

Updated 2026-10-02 against the current UWP page implementations.

## Implemented

- Continuous reading in both directions across book boundaries, with a bounded window of rendered chapters.
- Book/chapter selectors and saved position follow the visible chapter without filling navigation history.
- Choosing a book opens its chapter picker before navigation; dismissing the picker restores the current location.
- Explicit icon dimensions and initial selection after layout address startup navigation-icon clipping.
- Browser title and 16/32/48-pixel favicons use Elementary's original UWP assets.
- ASV, KJV, and NET translation selection, with all 66 books and 1,189 chapters per translation.
- Scripture and display lines prepared by the shared parser during the build, eliminating full-Bible USFM parsing in the browser. Generated resources remain under obj.
- Search-result navigation briefly highlights the selected verse.
- Delayed passage-loading feedback and an error message with retry.
- Streak cards reflow when the available width changes.
- Debug verse-of-the-day refresh resets the cached verse before fetching again.
- Search open/close and typography changes preserve a visible reading-line anchor while layout settles.
- Accessible names for book/chapter selectors and search controls; selectable verse text.

## Latest parity pass

- Escape closes Search and returns keyboard focus to its navigation item.
- Search glyphs now use the bundled original Fluent icon font.
- Verse-of-the-day artwork uses maximum dimensions instead of a fixed 490-pixel box, and exposes its reference and verse text to accessibility tools.
- Added a Cloudflare Workers configuration that publishes all translations with AOT explicitly disabled.

## Deliberately excluded

- Keep-screen-awake, per user request.
- Windows Live Tile updates, which have no direct browser equivalent.

## Validation and follow-up

- The scripture compiler successfully prepared 198 books / 3,567 chapters. Compilation validates chapter counts against the shared canonical mapping.
- Browser startup with all translations bundled was reviewed after correcting resource culture detection for book codes such as DAN and HEB.
- Detailed screen-reader/keyboard parity, narrow-window visuals, and release-build performance still need review.
- Browser review confirmed Search preserves the chapter position, full-Bible search finds John 3:16, and selecting it scrolls to a temporary highlighted verse.
- Translation changes defer chapter positioning until the reader is visible again.

This is a focused source audit, not a claim that every UWP feature has been checked. No new automated tests were run for this pass.


