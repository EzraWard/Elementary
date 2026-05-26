using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elementary.VerseOfTheDay.Services
{
    public class VotdImageCompositor : IVotdImageCompositor
    {
        private static readonly (int Width, int Height)[] SizeDimensions =
        {
            (640, 360),  // Widget640x360
            (150, 150),  // TileMedium150
            (310, 150),  // TileWide310x150
            (310, 310),  // TileLarge310x310
            (800, 800),  // InApp
        };

        public byte[] Compose(UnsplashPhoto photo, BibleVerseData verse, VotdImageSize size)
        {
            var sw = Stopwatch.StartNew();

            var (width, height) = SizeDimensions[(int)size];

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            DrawBackground(canvas, photo, width, height);
            DrawGradientOverlay(canvas, width, height);
            DrawVerseText(canvas, verse, width, height);
            DrawReference(canvas, verse, width, height);

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);

            sw.Stop();
            Debug.WriteLine($"[VotdImageCompositor] {size} composed in {sw.ElapsedMilliseconds}ms");

            return encoded.ToArray();
        }

        private static void DrawBackground(SKCanvas canvas, UnsplashPhoto photo, int width, int height)
        {
            if (photo.ImageBytes == null || photo.ImageBytes.Length == 0) return;

            try
            {
                using var bitmap = SKBitmap.Decode(photo.ImageBytes);
                if (bitmap == null) return;

                var sourceRect = GetCoverSourceRect(bitmap.Width, bitmap.Height, width, height);
                canvas.DrawBitmap(bitmap, sourceRect, new SKRect(0, 0, width, height));
            }
            catch { /* Silently skip if image cannot be decoded */ }
        }

        private static SKRect GetCoverSourceRect(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || targetWidth <= 0 || targetHeight <= 0)
            {
                return new SKRect(0, 0, sourceWidth, sourceHeight);
            }

            var sourceAspect = (float)sourceWidth / sourceHeight;
            var targetAspect = (float)targetWidth / targetHeight;

            if (sourceAspect > targetAspect)
            {
                var croppedWidth = sourceHeight * targetAspect;
                var left = (sourceWidth - croppedWidth) / 2f;
                return new SKRect(left, 0, left + croppedWidth, sourceHeight);
            }

            var croppedHeight = sourceWidth / targetAspect;
            var top = (sourceHeight - croppedHeight) / 2f;
            return new SKRect(0, top, sourceWidth, top + croppedHeight);
        }

        private static void DrawGradientOverlay(SKCanvas canvas, int width, int height)
        {
            using var paint = new SKPaint();
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, height),
                new[]
                {
                    new SKColor(0, 0, 0, 30),
                    new SKColor(0, 0, 0, 160)
                },
                null,
                SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, width, height, paint);
        }

        private static void DrawVerseText(SKCanvas canvas, BibleVerseData verse, int width, int height)
        {
            float padding = width * 0.06f;
            float textAreaWidth = width - padding * 2;
            // Reserve bottom 20% for the reference
            float textAreaHeight = height * 0.75f;

            float fontSize = CalculateFontSize(verse.VerseText, textAreaWidth, textAreaHeight, width);

            using var textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = fontSize,
                IsAntialias = true,
                Typeface = SKTypeface.Default,
                FilterQuality = SKFilterQuality.High
            };

            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 200),
                TextSize = fontSize,
                IsAntialias = true,
                Typeface = SKTypeface.Default
            };

            var lines = WrapText(verse.VerseText, textPaint, textAreaWidth);
            float lineHeight = fontSize * 1.35f;
            float totalTextHeight = lines.Count * lineHeight;

            // Centre the text block vertically in the upper 75% of the image
            float textStartY = ((textAreaHeight - totalTextHeight) / 2f) + fontSize;

            foreach (var line in lines)
            {
                float shadowOffset = Math.Max(1f, fontSize * 0.04f);
                canvas.DrawText(line, padding + shadowOffset, textStartY + shadowOffset, shadowPaint);
                canvas.DrawText(line, padding, textStartY, textPaint);
                textStartY += lineHeight;
            }
        }

        private static void DrawReference(SKCanvas canvas, BibleVerseData verse, int width, int height)
        {
            float padding = width * 0.06f;
            float refFontSize = Math.Max(8f, width * 0.035f);

            using var refPaint = new SKPaint
            {
                Color = new SKColor(230, 230, 230, 220),
                TextSize = refFontSize,
                IsAntialias = true,
                Typeface = SKTypeface.Default
            };

            float y = height - padding;
            canvas.DrawText(verse.Reference, padding, y, refPaint);
        }

        internal static float CalculateFontSize(string text, float textAreaWidth, float textAreaHeight, int imageWidth)
        {
            float baseFontSize = imageWidth * 0.11f;
            float minFontSize = Math.Max(7f, imageWidth * 0.025f);
            float maxFontSize = imageWidth * 0.11f;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var characterCount = text.Trim().Length;
                if (characterCount > 180)
                {
                    baseFontSize = imageWidth * 0.07f;
                }
                else if (characterCount > 120)
                {
                    baseFontSize = imageWidth * 0.085f;
                }
            }

            baseFontSize = Math.Max(minFontSize, Math.Min(baseFontSize, maxFontSize));

            using var tempPaint = new SKPaint { TextSize = baseFontSize };
            var lines = WrapText(text, tempPaint, textAreaWidth);
            float lineHeight = baseFontSize * 1.35f;
            float totalH = lines.Count * lineHeight;

            while ((totalH > textAreaHeight || HasLineExceedingWidth(lines, tempPaint, textAreaWidth)) && baseFontSize > minFontSize)
            {
                baseFontSize -= 1f;
                tempPaint.TextSize = baseFontSize;
                lines = WrapText(text, tempPaint, textAreaWidth);
                lineHeight = baseFontSize * 1.35f;
                totalH = lines.Count * lineHeight;
            }

            return baseFontSize;
        }

        private static bool HasLineExceedingWidth(IReadOnlyList<string> lines, SKPaint paint, float maxWidth)
        {
            foreach (var line in lines)
            {
                if (paint.MeasureText(line) > maxWidth)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> WrapText(string text, SKPaint paint, float maxWidth)
        {
            var words = text.Split(' ');
            var lines = new List<string>();
            var current = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                var test = current.Length > 0 ? $"{current} {word}" : word;
                if (paint.MeasureText(test) > maxWidth && current.Length > 0)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
                else
                {
                    current.Clear();
                    current.Append(test);
                }
            }

            if (current.Length > 0)
                lines.Add(current.ToString());

            return lines;
        }
    }
}
