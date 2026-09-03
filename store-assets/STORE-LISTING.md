# Microsoft Store listing draft

This draft intentionally does **not** advertise Verse of the Day, live tiles, or widgets. Add those features only after the release blockers in `RELEASE-READINESS-1.0.md` are resolved.

## Identity

Product name: **Elementary**

Reserve and use the exact name **Elementary** consistently in Partner Center and the production manifest. If it is unavailable, choose a distinctive replacement before finalizing any Store copy rather than appending descriptive marketing text to the title.

Primary category: **Books & reference**

Pricing recommendation: **Free**

Market/language for 1.0: **English (United States)**  
Do not declare other UI languages until the app strings and listing are localized.

## Short description

Read the Bible without clutter. Elementary pairs offline NET, KJV, and ASV text with full-Bible search, reading history, customizable typography and themes, and private on-device streaks.

## Description

Elementary is a calm, focused Bible reader built for Windows.

Open directly to the text and read without accounts, ads, feeds, or unnecessary distractions. Choose the NET Bible, King James Version, or American Standard Version, then make the reading view your own with light, dark, or system themes, two typefaces, three text sizes, and optional verse numbers.

All included Bible text is available offline. Search across the entire Bible or narrow the scope to the Old or New Testament. Reading history makes it easy to return to recently opened chapters, while a private on-device streak and badge gallery help you build a consistent reading habit.

Elementary stores your reading position, history, preferences, and streak progress on your Windows device. No account or sign-in is required.

## Product features

Enter these as separate Partner Center feature fields; Partner Center supplies the bullets.

1. Focused, distraction-free Bible reading
2. Offline NET, KJV, and ASV translations
3. Search the entire Bible or either testament
4. Continuous reading across books and chapters
5. Reading history for recently opened chapters
6. Private on-device reading streaks and badges
7. Light, dark, and system themes
8. Segoe UI or Georgia reading fonts
9. Three text sizes and optional verse numbers
10. Keep-screen-awake option for longer reading sessions
11. No account or sign-in required

## Keywords

Use these seven keyword phrases:

1. Bible reader
2. offline Bible
3. scripture
4. Bible study
5. KJV
6. ASV
7. NET Bible

## What's new

Leave this field blank for the first Store submission. For the first update after 1.0, use release-specific notes rather than repeating the description.

## Screenshot order and captions

All screenshots use unmodified captures of the running app, centered on a plain neutral wallpaper background at 1600×1200. The taskbar and desktop clutter are excluded, and the app UI contains no added logos or marketing overlays.

1. `gallery/01-reader.png`
   Read offline in a focused, distraction-free view designed for comfortable time in Scripture.
2. `gallery/02-search.png`
   Find a word or passage across the entire Bible, Old Testament, or New Testament.
3. `gallery/03-reading-history.png`
   Return to recently opened books and chapters with one click.
4. `gallery/04-reading-streak.png`
   Build a consistent reading habit with private on-device streaks and badges.
5. `gallery/05-settings.png`
   Choose your translation, font, text size, verse numbers, theme, and screen-awake preference.

Additional Store art:

- `icon/app-tile-300x300.png` — recommended 1:1 app tile icon.
- `hero/elementary-super-hero-1920x1080.png` — optional 16:9 super hero art.

Microsoft requires at least one screenshot and recommends at least four. Screenshots must be accurate, and Microsoft advises against adding marketing messages or extra logos over them:  
https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images

## Support and links

Support email: **ezra.ward@outlook.com**

Website/source: **https://github.com/EzraWard/Elementary**

Privacy policy: publish the reviewed policy on a stable HTTPS page you control, then enter that URL in Partner Center. A draft is included at `PRIVACY-POLICY-DRAFT.md`.

## Age rating

Complete the Partner Center/IARC questionnaire honestly. The app is a scripture reader, but the bundled text includes descriptions of violence, death, sexual conduct, alcohol, and other mature subjects. Do not choose an answer solely from the app’s visual presentation.

## Certification notes draft

Elementary does not require an account or sign-in.

To exercise the core app:

1. Launch the app; the Bible reader opens.
2. Use the book and chapter selectors at the top of the reader.
3. Open Search from the left navigation and search for “love”.
4. Open Reading History from the left navigation.
5. Open Settings to change translation, verse numbers, font, text size, and theme.
6. Open Reading Streak to view on-device progress and badges.

The included Bible translations are packaged for offline reading. Reading position, history, preferences, and streak progress are stored locally.

If Verse of the Day remains in the submitted build, document its network dependency and graceful offline behavior here. Do not submit the currently reviewed crash-prone implementation.

## Partner Center reminders

- Make the Store product name and installed manifest display name match.
- Keep the production description plain text.
- Enter features without manual bullets.
- Upload only screenshots from the submitted feature set.
- Use the final live privacy/support URLs.
- Upload the exact package that passed the architecture smoke tests and Windows App Certification Kit.

Microsoft’s MSIX listing field guidance:  
https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/add-and-edit-store-listing-info
