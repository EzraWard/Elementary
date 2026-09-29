# Shared Assets

Elementary's title bar image and Fluent navigation font are linked directly from
`Elementary.UWP/Assets`. `Icons/appicon.png` and `Splash/elementary.png` are
unchanged copies of `Elementary.UWP/Assets/Square44x44Logo.targetsize-256.png`;
Uno Resizetizer requires separate inputs with lowercase file names.

The streak PathIcon preserves the outline of the UWP app's Segoe UI Symbol
U+1F525 glyph. This avoids relying on a Windows system font being installed in
the browser runtime.

See documentation about assets here: https://github.com/unoplatform/uno/blob/master/doc/articles/features/working-with-assets.md

## Here is a cheat sheet

1. Add the image file to the `Assets` directory of a shared project.
2. Set the build action to `Content`.
3. (Recommended) Provide an asset for various scales/dpi

### Examples

```text
\Assets\Images\logo.scale-100.png
\Assets\Images\logo.scale-200.png
\Assets\Images\logo.scale-400.png

\Assets\Images\scale-100\logo.png
\Assets\Images\scale-200\logo.png
\Assets\Images\scale-400\logo.png
```

### Table of scales

| Scale | WinUI       | iOS | Android |
|-------|:-----------:|:---------------:|:-------:|
| `100` | scale-100   | @1x             | mdpi    |
| `125` | scale-125   | N/A             | N/A     |
| `150` | scale-150   | N/A             | hdpi    |
| `200` | scale-200   | @2x             | xhdpi   |
| `300` | scale-300   | @3x             | xxhdpi  |
| `400` | scale-400   | N/A             | xxxhdpi |
