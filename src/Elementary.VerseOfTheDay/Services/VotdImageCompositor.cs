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

        private static readonly SKColor[][] Palettes =
        {
            new[] { new SKColor(20, 16, 48), new SKColor(67, 56, 202), new SKColor(236, 72, 153), new SKColor(245, 158, 11) },
            new[] { new SKColor(5, 35, 51), new SKColor(14, 116, 144), new SKColor(56, 189, 248), new SKColor(167, 243, 208) },
            new[] { new SKColor(45, 15, 28), new SKColor(154, 52, 18), new SKColor(249, 115, 22), new SKColor(253, 230, 138) },
            new[] { new SKColor(5, 46, 43), new SKColor(15, 118, 110), new SKColor(52, 211, 153), new SKColor(250, 204, 21) },
            new[] { new SKColor(46, 16, 101), new SKColor(126, 34, 206), new SKColor(232, 121, 249), new SKColor(103, 232, 249) },
            new[] { new SKColor(39, 39, 105), new SKColor(192, 38, 211), new SKColor(251, 113, 133), new SKColor(253, 230, 138) }
        };

        public byte[] Compose(BibleVerseData verse, VotdImageSize size, string seed)
        {
            var sw = Stopwatch.StartNew();

            var (width, height) = SizeDimensions[(int)size];

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            DrawAbstractBackground(canvas, seed, width, height);
            DrawGradientOverlay(canvas, width, height);
            DrawVerseText(canvas, verse, width, height);
            DrawReference(canvas, verse, width, height);

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);

            sw.Stop();
            Debug.WriteLine($"[VotdImageCompositor] {size} composed in {sw.ElapsedMilliseconds}ms");

            return encoded.ToArray();
        }

        private static void DrawAbstractBackground(SKCanvas canvas, string seed, int width, int height)
        {
            var random = new Random(GetStableSeed(seed));
            var palette = Palettes[random.Next(Palettes.Length)];
            var angle = NextFloat(random, 0f, (float)(Math.PI * 2));
            var center = new SKPoint(width / 2f, height / 2f);
            var reach = Math.Max(width, height) * 0.75f;
            var direction = new SKPoint((float)Math.Cos(angle) * reach, (float)Math.Sin(angle) * reach);

            using (var basePaint = new SKPaint())
            {
                basePaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(center.X - direction.X, center.Y - direction.Y),
                    new SKPoint(center.X + direction.X, center.Y + direction.Y),
                    new[] { palette[0], palette[1], palette[0] },
                    new[] { 0f, 0.58f, 1f },
                    SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, width, height, basePaint);
            }

            var minimumDimension = Math.Min(width, height);
            for (var i = 0; i < 5; i++)
            {
                var accent = palette[2 + (i % 2)];
                var glowCenter = new SKPoint(
                    NextFloat(random, -0.1f, 1.1f) * width,
                    NextFloat(random, -0.1f, 1.1f) * height);
                var radius = minimumDimension * NextFloat(random, 0.32f, 0.72f);

                using var glowPaint = new SKPaint { IsAntialias = true };
                glowPaint.Shader = SKShader.CreateRadialGradient(
                    glowCenter,
                    radius,
                    new[] { WithAlpha(accent, (byte)random.Next(80, 151)), WithAlpha(accent, 0) },
                    new[] { 0f, 1f },
                    SKShaderTileMode.Clamp);
                canvas.DrawCircle(glowCenter, radius, glowPaint);
            }

            for (var i = 0; i < 3; i++)
            {
                var startY = NextFloat(random, -0.1f, 1.1f) * height;
#if NET10_0_OR_GREATER
                using var pathBuilder = new SKPathBuilder();
                pathBuilder.MoveTo(-width * 0.15f, startY);
                pathBuilder.CubicTo(
                    width * 0.25f, NextFloat(random, -0.2f, 1.2f) * height,
                    width * 0.65f, NextFloat(random, -0.2f, 1.2f) * height,
                    width * 1.15f, NextFloat(random, -0.1f, 1.1f) * height);
                using var path = pathBuilder.Detach();
#else
                using var path = new SKPath();
                path.MoveTo(-width * 0.15f, startY);
                path.CubicTo(
                    width * 0.25f, NextFloat(random, -0.2f, 1.2f) * height,
                    width * 0.65f, NextFloat(random, -0.2f, 1.2f) * height,
                    width * 1.15f, NextFloat(random, -0.1f, 1.1f) * height);
#endif

                using var ribbonPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeWidth = minimumDimension * NextFloat(random, 0.045f, 0.12f),
                    Color = WithAlpha(palette[2 + (i % 2)], (byte)random.Next(18, 43)),
                    IsAntialias = true
                };
                canvas.DrawPath(path, ribbonPaint);
            }

            using var detailPaint = new SKPaint { IsAntialias = true };
            var detailCount = Math.Max(20, Math.Min(80, (width * height) / 8000));
            for (var i = 0; i < detailCount; i++)
            {
                detailPaint.Color = WithAlpha(palette[3], (byte)random.Next(18, 55));
                var radius = minimumDimension * NextFloat(random, 0.0015f, 0.006f);
                canvas.DrawCircle(
                    NextFloat(random, 0f, width),
                    NextFloat(random, 0f, height),
                    Math.Max(0.6f, radius),
                    detailPaint);
            }
        }

        private static int GetStableSeed(string value)
        {
            unchecked
            {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                var hash = offset;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= prime;
                }

                return (int)hash;
            }
        }

        private static float NextFloat(Random random, float minimum, float maximum)
        {
            return minimum + ((float)random.NextDouble() * (maximum - minimum));
        }

        private static SKColor WithAlpha(SKColor color, byte alpha)
        {
            return new SKColor(color.Red, color.Green, color.Blue, alpha);
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
                Color = new SKColor(248, 244, 235),
                IsAntialias = true
            };

            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 115),
                IsAntialias = true
            };
            using var font = new SKFont(SKTypeface.Default, fontSize);

            var lines = WrapText(verse.VerseText, font, textPaint, textAreaWidth);
            float lineHeight = fontSize * 1.35f;
            float totalTextHeight = lines.Count * lineHeight;

            // Centre the text block vertically in the upper 75% of the image
            float textStartY = ((textAreaHeight - totalTextHeight) / 2f) + fontSize;
            DrawTextBackdrop(canvas, padding, textStartY, lines.Count, fontSize, lineHeight, textAreaWidth);

            foreach (var line in lines)
            {
                float shadowOffset = Math.Max(0.75f, fontSize * 0.025f);
                DrawText(canvas, line, padding + shadowOffset, textStartY + shadowOffset, font, shadowPaint);
                DrawText(canvas, line, padding, textStartY, font, textPaint);
                textStartY += lineHeight;
            }
        }

        private static void DrawTextBackdrop(
            SKCanvas canvas,
            float x,
            float firstBaseline,
            int lineCount,
            float fontSize,
            float lineHeight,
            float width)
        {
            if (lineCount <= 0) return;

            float horizontalInset = fontSize * 0.35f;
            float verticalInset = fontSize * 0.28f;
            float top = firstBaseline - fontSize - verticalInset;
            float bottom = firstBaseline + ((lineCount - 1) * lineHeight) + (fontSize * 0.35f) + verticalInset;
            var rect = new SKRect(
                x - horizontalInset,
                top,
                x + width + horizontalInset,
                bottom);
            float radius = Math.Max(4f, fontSize * 0.18f);

            using var glowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 55),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, Math.Max(3f, fontSize * 0.18f))
            };
            canvas.DrawRoundRect(rect, radius, radius, glowPaint);

            using var fillPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 62),
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, radius, radius, fillPaint);
        }

        private static void DrawReference(SKCanvas canvas, BibleVerseData verse, int width, int height)
        {
            float padding = width * 0.06f;
            float refFontSize = Math.Max(8f, width * 0.035f);

            using var refPaint = new SKPaint
            {
                Color = new SKColor(238, 232, 220, 210),
                IsAntialias = true
            };
            using var refFont = new SKFont(SKTypeface.Default, refFontSize);

            float y = height - padding;
            DrawText(canvas, verse.Reference, padding, y, refFont, refPaint);
        }

        private static void DrawText(
            SKCanvas canvas,
            string text,
            float x,
            float y,
            SKFont font,
            SKPaint paint)
        {
#if NET10_0_OR_GREATER
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
#else
            canvas.DrawText(text, x, y, font, paint);
#endif
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

            using var tempPaint = new SKPaint();
            using var tempFont = new SKFont(SKTypeface.Default, baseFontSize);
            var lines = WrapText(text, tempFont, tempPaint, textAreaWidth);
            float lineHeight = baseFontSize * 1.35f;
            float totalH = lines.Count * lineHeight;

            while ((totalH > textAreaHeight || HasLineExceedingWidth(lines, tempFont, tempPaint, textAreaWidth)) && baseFontSize > minFontSize)
            {
                baseFontSize -= 1f;
                tempFont.Size = baseFontSize;
                lines = WrapText(text, tempFont, tempPaint, textAreaWidth);
                lineHeight = baseFontSize * 1.35f;
                totalH = lines.Count * lineHeight;
            }

            return baseFontSize;
        }

        private static bool HasLineExceedingWidth(
            IReadOnlyList<string> lines,
            SKFont font,
            SKPaint paint,
            float maxWidth)
        {
            foreach (var line in lines)
            {
                if (MeasureText(line, font, paint) > maxWidth)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> WrapText(string text, SKFont font, SKPaint paint, float maxWidth)
        {
            var words = text.Split(' ');
            var lines = new List<string>();
            var current = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                var test = current.Length > 0 ? $"{current} {word}" : word;
                if (MeasureText(test, font, paint) > maxWidth && current.Length > 0)
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

        private static float MeasureText(string text, SKFont font, SKPaint paint)
        {
#if NET10_0_OR_GREATER
            return font.MeasureText(text, paint);
#else
            var glyphCount = font.CountGlyphs(text);
            var glyphs = new ushort[glyphCount];
            font.GetGlyphs(text, glyphs);
            return font.MeasureText(new ReadOnlySpan<ushort>(glyphs, 0, glyphCount), paint);
#endif
        }
    }
}
