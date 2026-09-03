# Elementary Microsoft Store assets

## Ready-to-upload files

- `gallery/01-reader.png` — 1600×1200
- `gallery/02-search.png` — 1600×1200
- `gallery/03-reading-history.png` — 1600×1200
- `gallery/04-reading-streak.png` — 1600×1200
- `gallery/05-settings.png` — 1600×1200
- `icon/app-tile-300x300.png` — 300×300
- `hero/elementary-super-hero-1920x1080.png` — 1920×1080

The gallery images show direct captures of the running UWP app centered on a plain neutral wallpaper canvas. The app UI is unmodified; no marketing copy, extra logos, device frames, simulated UI, taskbar, or desktop clutter were added.

Run `Capture-StoreScreenshots.ps1` while Elementary is open to reproduce the five gallery captures. The script centers the live app window in the primary display's working area, navigates through the real app with Windows UI Automation, trims the transparent Windows shadow that can reveal the desktop, and writes upload-ready 1600×1200 PNG files.

The 300×300 tile is copied from the current UWP `Square150x150Logo.scale-200.png` asset and retains its transparent background.

The optional hero is an AI-generated abstract asset created for this review. It was center-cropped and resized deterministically to Microsoft’s required 1920×1080 dimensions. It contains no text, title, UI, device imagery, literal Bible, cross, or other religious symbol.

Listing copy, captions, and submission notes are in `STORE-LISTING.md`.
