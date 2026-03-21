using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace Elementary.Core.Parsers
{
    public class UsfmVerse
    {
        public int Number { get; set; }
        public string Text { get; set; }
    }

    public class UsfmChapter
    {
        public int Index { get; set; }
        public List<UsfmVerse> Verses { get; set; } = new List<UsfmVerse>();
        public List<string> Footnotes { get; set; } = new List<string>();

        public List<ChapterDisplayLine> ToDisplayLines()
        {
            var lines = new List<ChapterDisplayLine>();
            foreach (var v in Verses)
            {
                if (v.Number > 0)
                {
                    lines.Add(new ChapterDisplayLine
                    {
                        Type = ChapterDisplayLineType.Verse,
                        VerseNumber = v.Number,
                        Text = ToPlainText(v.Text)
                    });
                    continue;
                }

                var text = v.Text ?? string.Empty;
                if (text.IndexOf("<h1>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lines.Add(new ChapterDisplayLine { Type = ChapterDisplayLineType.Heading, Text = ToPlainText(text) });
                }
                else if (text.IndexOf("class=\"poetry\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lines.Add(new ChapterDisplayLine { Type = ChapterDisplayLineType.Poetry, Text = ToPlainText(text) });
                }
                else if (text.IndexOf("<p", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lines.Add(new ChapterDisplayLine { Type = ChapterDisplayLineType.ParagraphBreak, Text = string.Empty });
                }
                else
                {
                    var plain = ToPlainText(text);
                    if (!string.IsNullOrWhiteSpace(plain))
                    {
                        lines.Add(new ChapterDisplayLine { Type = ChapterDisplayLineType.Text, Text = plain });
                    }
                }
            }

            if (Footnotes.Count > 0)
            {
                for (int i = 0; i < Footnotes.Count; i++)
                {
                    lines.Add(new ChapterDisplayLine
                    {
                        Type = ChapterDisplayLineType.Footnote,
                        Text = $"{i + 1}. {ToPlainText(Footnotes[i])}"
                    });
                }
            }

            return lines;
        }

        public string ToHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"chapter\">");
            sb.Append($"<h2>{Index}</h2>");
            foreach (var v in Verses)
            {
                if (v.Number == 0)
                {
                    // special element (heading, paragraph marker, quote)
                    sb.Append($"{v.Text}");
                }
                else
                {
                    sb.Append($"<p><sup>{v.Number}</sup> {v.Text}</p>");
                }
            }
            if (Footnotes.Count > 0)
            {
                sb.Append("<div class=\"footnotes\">");
                int i = 1;
                foreach (var f in Footnotes)
                {
                    sb.Append($"<p><sup>{i}</sup> {System.Net.WebUtility.HtmlEncode(f)}</p>");
                    i++;
                }
                sb.Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string ToPlainText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var decoded = System.Net.WebUtility.HtmlDecode(text);
            decoded = Regex.Replace(decoded, "<sup>(\\d+)</sup>", string.Empty, RegexOptions.IgnoreCase);
            decoded = Regex.Replace(decoded, "<[^>]+>", string.Empty, RegexOptions.Singleline);
            decoded = Regex.Replace(decoded, "\\s+", " ").Trim();
            return decoded;
        }
    }

    public class UsfmBook
    {
        public string Title { get; set; }
        public List<UsfmChapter> Chapters { get; set; } = new List<UsfmChapter>();
    }

    public static class UsfmParser
    {
        // Lightweight USFM parser that supports: \id, \h, \mt, \c, \p, \s, \v, inline \em/\em* (italics), \it/\it*, \f ... \f* (footnotes), \x ... \x* (cross-refs), \q (poetic)
        public static UsfmBook ParseBook(string content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            var book = new UsfmBook();

            // Normalize line endings and convert escaped \n sequences used in tests to actual newlines.
            // Only convert when \n is used as an escaped line break (e.g. "\n\h"), not USFM markers like "\nd".
            content = Regex.Replace(content, @"\\n(?=\\|$)", "\n");
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");

            // Extract title from \h or \mt or \id
            var titleMatch = Regex.Match(content, @"\\h\s+([^\\\n\r]+)", RegexOptions.IgnoreCase);
            if (!titleMatch.Success)
            {
                titleMatch = Regex.Match(content, @"\\mt\s+([^\\\n\r]+)", RegexOptions.IgnoreCase);
            }
            if (!titleMatch.Success)
            {
                titleMatch = Regex.Match(content, @"\\id\s+([^\\\n\r]+)", RegexOptions.IgnoreCase);
            }
            if (titleMatch.Success)
            {
                book.Title = titleMatch.Groups[1].Value.Trim();
            }

            // Process sequentially: find chapters by \c markers
            var chapterSplits = Regex.Split(content, @"(?=\\c\s+\d+)", RegexOptions.Multiline);

            foreach (var chunk in chapterSplits)
            {
                if (string.IsNullOrWhiteSpace(chunk)) continue;

                var cMatch = Regex.Match(chunk, @"\\c\s+(\d+)", RegexOptions.IgnoreCase);
                if (!cMatch.Success)
                {
                    // if no chapter marker, skip
                    continue;
                }

                var chapter = new UsfmChapter();
                chapter.Index = int.Parse(cMatch.Groups[1].Value);

                // Extract footnotes first (\f ... \f*) non-greedy
                var footnotes = new List<string>();
                var footnotePattern = new Regex(@"\\f\s+(.*?)\\f\*", RegexOptions.Singleline);
                chapter.Footnotes = new List<string>();
                var footnoteIndex = 0;
                foreach (Match f in footnotePattern.Matches(chunk))
                {
                    var fn = f.Groups[1].Value.Trim();
                    // Remove nested markers in footnote
                    fn = ProcessInline(fn);
                    chapter.Footnotes.Add(fn);
                }

                // Remove footnote bodies from chunk (replace with a placeholder marker referencing index)
                var cleaned = footnotePattern.Replace(chunk, m =>
                {
                    footnoteIndex++;
                    return $" <fn id=\"{footnoteIndex}\"/> ";
                });

                // Now split lines and process verse markers
                var lines = cleaned.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    // Verse line
                    var vMatch = Regex.Match(trimmed, @"\\v\s+(\d+)\s*(.*)", RegexOptions.Singleline);
                    if (vMatch.Success)
                    {
                        var vnum = int.Parse(vMatch.Groups[1].Value);
                        var vtext = vMatch.Groups[2].Value.Trim();

                        vtext = ProcessInline(vtext);

                        // Replace footnote placeholders like <fn id="n"/> with reference numbers anchored to chapter footnotes
                        vtext = Regex.Replace(vtext, "<fn id=\\\"(\\d+)\\\"/>", m =>
                        {
                            var idx = int.Parse(m.Groups[1].Value) - 1;
                            if (idx >= 0 && idx < chapter.Footnotes.Count)
                            {
                                // Return a parenthetical footnote marker; actual footnote appended at end
                                return $"<sup>{idx + 1}</sup>";
                            }
                            return string.Empty;
                        });

                        chapter.Verses.Add(new UsfmVerse { Number = vnum, Text = vtext });
                        continue;
                    }

                    // Section heading \s, \s1, \s2, etc.
                    var sMatch = Regex.Match(trimmed, @"\\s\d*\s*(.*)", RegexOptions.Singleline);
                    if (sMatch.Success)
                    {
                        var stext = ProcessInline(sMatch.Groups[1].Value.Trim());
                        // represent as a paragraph with bold
                        chapter.Verses.Add(new UsfmVerse { Number = 0, Text = $"<h1>{stext}</h1>" });
                        continue;
                    }

                    // Paragraph marker \p - continue as paragraph separator (no specific verse)
                    var pMatch = Regex.Match(trimmed, @"^\\p$", RegexOptions.IgnoreCase);
                    if (pMatch.Success)
                    {
                        chapter.Verses.Add(new UsfmVerse { Number = 0, Text = "<p></p>" });
                        continue;
                    }

                    // Poetic lines \q, \q1, \q2, etc - preserve indent level
                    var qMatch = Regex.Match(trimmed, @"\\q(\d*)\s*(.*)", RegexOptions.Singleline);
                    if (qMatch.Success)
                    {
                        var qtext = ProcessInline(qMatch.Groups[2].Value.Trim());
                        var level = string.IsNullOrEmpty(qMatch.Groups[1].Value) ? 0 : int.Parse(qMatch.Groups[1].Value);
                        var indent = level * 20; // 20px per indent level
                        chapter.Verses.Add(new UsfmVerse { Number = 0, Text = $"<div class=\"poetry\" style=\"margin-left:{indent}px\">{qtext}</div>" });
                        continue;
                    }

                    // If none matched but line contains text, try to treat as continuation of previous verse
                    var last = chapter.Verses.Count > 0 ? chapter.Verses[chapter.Verses.Count - 1] : null;
                    if (last != null)
                    {
                        // append to last verse text
                        last.Text = last.Text + " " + ProcessInline(trimmed);
                    }
                }

                book.Chapters.Add(chapter);
            }

            // If no title found, try to infer from first non-empty book-level marker
            if (string.IsNullOrEmpty(book.Title))
            {
                var idMatch = Regex.Match(content, @"\\id\s+([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
                if (idMatch.Success) book.Title = idMatch.Groups[1].Value.Trim();
            }

            return book;
        }

        private static string ProcessInline(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Remove word markers (\w ... \w* and \+w ... \+w*) keeping only display text.
            text = Regex.Replace(text, @"\\\+?w\s+([^|\\]+?)(?:\|.*?)?\\\+?w\*", m => m.Groups[1].Value.Trim(), RegexOptions.Singleline);

            // Replace quote markers \qt ... \qt*
            text = Regex.Replace(text, @"\\qt\s+", "<qt>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\\qt\*", "</qt>", RegexOptions.IgnoreCase);

            // Replace cross-references \x ... \x*
            text = Regex.Replace(text, @"\\x\s+(.*?)\\x\*", m => $"<xr>{System.Net.WebUtility.HtmlEncode(m.Groups[1].Value.Trim())}</xr>", RegexOptions.Singleline);

            // Replace emphasis \em ... \em* and \it ... \it*
            text = Regex.Replace(text, @"\\em\s+", "<em>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\\em\*", "</em>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\\it\s+", "<em>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\\it\*", "</em>", RegexOptions.IgnoreCase);

            // Bold markers (some USFM use \bd ... \bd*)
            text = Regex.Replace(text, @"\\bd\s+", "<b>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\\bd\*", "</b>", RegexOptions.IgnoreCase);

            // Remove any other remaining USFM markers to prevent leakage (e.g. \nd, \nd*, \q1, \+w),
            // even when followed by punctuation like \nd*!
            text = Regex.Replace(text, @"\\\+?[a-z]+\d*\*?(?=[^A-Za-z0-9]|$)", "", RegexOptions.IgnoreCase);

            // HTML-encode the remainder then un-encode known tags
            var encoded = System.Net.WebUtility.HtmlEncode(text);
            // restore our simple tags
            encoded = encoded.Replace("&lt;em&gt;", "<em>").Replace("&lt;/em&gt;", "</em>");
            encoded = encoded.Replace("&lt;b&gt;", "<b>").Replace("&lt;/b&gt;", "</b>");
            encoded = encoded.Replace("&lt;qt&gt;", "<qt>").Replace("&lt;/qt&gt;", "</qt>");
            encoded = encoded.Replace("&lt;xr&gt;", "<xr>").Replace("&lt;/xr&gt;", "</xr>");
            encoded = encoded.Replace("&lt;fn id=\"", "<fn id=\"").Replace("/&gt;", "/>");
            encoded = encoded.Replace("&lt;h1&gt;", "<h1>").Replace("&lt;/h1&gt;", "</h1>");
            encoded = encoded.Replace("&lt;quote&gt;", "<quote>").Replace("&lt;/quote&gt;", "</quote>");
            // Restore poetry div with inline style (Regex pattern to handle variable pixel values)
            encoded = Regex.Replace(encoded, @"&lt;div class=&quot;poetry&quot; style=&quot;margin-left:(\d+)px&quot;&gt;", "<div class=\"poetry\" style=\"margin-left:$1px\">");
            encoded = encoded.Replace("&lt;/div&gt;", "</div>");

            return encoded;
        }
    }
}
