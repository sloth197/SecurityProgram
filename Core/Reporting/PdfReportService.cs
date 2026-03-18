using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace SecurityProgram.App.Core.Reporting;

public class PdfReportService
{
    private const double Margin = 40;
    private const double BodyLineHeight = 15;
    private const double SectionLineHeight = 18;
    private const double TitleLineHeight = 24;

    public string ExportReport(
        string outputPath,
        string title,
        string analystName,
        string targetSystem,
        DateTime generatedAt,
        string reportText)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));
        }

        if (string.IsNullOrWhiteSpace(reportText))
        {
            throw new ArgumentException("Report text is empty. Generate a preview first.", nameof(reportText));
        }

        var fullPath = Path.GetFullPath(outputPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        using var document = new PdfDocument();
        document.Info.Title = title;
        document.Info.Author = analystName;
        document.Info.Subject = $"Security report for {targetSystem}";
        document.Info.CreationDate = generatedAt;

        var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.TryComputeSubset);

        var titleFont = CreateFont("Malgun Gothic", 16, XFontStyleEx.Bold, fontOptions);
        var sectionFont = CreateFont("Malgun Gothic", 11, XFontStyleEx.Bold, fontOptions);
        var bodyFont = CreateFont("Malgun Gothic", 10, XFontStyleEx.Regular, fontOptions);
        var metaFont = CreateFont("Malgun Gothic", 9, XFontStyleEx.Regular, fontOptions);

        var page = document.AddPage();
        page.Size = PageSize.A4;

        var gfx = XGraphics.FromPdfPage(page);
        var contentWidth = page.Width.Point - (Margin * 2);
        var y = Margin;

        void StartNewPage()
        {
            gfx.Dispose();
            page = document.AddPage();
            page.Size = PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            contentWidth = page.Width.Point - (Margin * 2);
            y = Margin;
        }

        void EnsureSpace(double requiredHeight)
        {
            if (y + requiredHeight > page.Height.Point - Margin)
            {
                StartNewPage();
            }
        }

        void DrawTextLine(string line, XFont font, XBrush brush, double lineHeight)
        {
            EnsureSpace(lineHeight);
            gfx.DrawString(
                line,
                font,
                brush,
                new XRect(Margin, y, contentWidth, lineHeight),
                XStringFormats.TopLeft);
            y += lineHeight;
        }

        DrawTextLine(string.IsNullOrWhiteSpace(title) ? "Security Report" : title, titleFont, XBrushes.Black, TitleLineHeight);
        DrawTextLine($"Generated: {generatedAt:yyyy-MM-dd HH:mm:ss}", metaFont, XBrushes.DimGray, BodyLineHeight);
        DrawTextLine($"Analyst: {analystName}", metaFont, XBrushes.DimGray, BodyLineHeight);
        DrawTextLine($"Target: {targetSystem}", metaFont, XBrushes.DimGray, BodyLineHeight);
        y += 8;

        foreach (var raw in NormalizeLines(reportText))
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                y += BodyLineHeight * 0.6;
                continue;
            }

            var normalized = raw.TrimEnd();

            if (normalized.StartsWith("## ", StringComparison.Ordinal))
            {
                y += 4;
                var sectionText = normalized[3..].Trim();
                foreach (var line in WrapText(gfx, sectionFont, sectionText, contentWidth))
                {
                    DrawTextLine(line, sectionFont, XBrushes.DarkSlateBlue, SectionLineHeight);
                }

                continue;
            }

            if (normalized.StartsWith("# ", StringComparison.Ordinal))
            {
                var headingText = normalized[2..].Trim();
                foreach (var line in WrapText(gfx, sectionFont, headingText, contentWidth))
                {
                    DrawTextLine(line, sectionFont, XBrushes.Black, SectionLineHeight);
                }

                continue;
            }

            if (normalized.StartsWith("- ", StringComparison.Ordinal))
            {
                normalized = "• " + normalized[2..].Trim();
            }

            foreach (var line in WrapText(gfx, bodyFont, normalized, contentWidth))
            {
                DrawTextLine(line, bodyFont, XBrushes.Black, BodyLineHeight);
            }
        }

        gfx.Dispose();
        document.Save(fullPath);

        return fullPath;
    }

    private static XFont CreateFont(
        string preferredFamily,
        double size,
        XFontStyleEx style,
        XPdfFontOptions options)
    {
        try
        {
            return new XFont(preferredFamily, size, style, options);
        }
        catch
        {
            return new XFont("Arial", size, style, options);
        }
    }

    private static IEnumerable<string> NormalizeLines(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.Replace('\t', ' '));
    }

    private static IEnumerable<string> WrapText(XGraphics gfx, XFont font, string line, double maxWidth)
    {
        if (string.IsNullOrEmpty(line))
        {
            yield return string.Empty;
            yield break;
        }

        var words = line.Split(' ', StringSplitOptions.None);
        var current = new StringBuilder();

        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (gfx.MeasureString(candidate, font).Width <= maxWidth)
            {
                current.Clear();
                current.Append(candidate);
                continue;
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (gfx.MeasureString(word, font).Width <= maxWidth)
            {
                current.Append(word);
                continue;
            }

            foreach (var split in SplitLongToken(gfx, font, word, maxWidth))
            {
                yield return split;
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private static IEnumerable<string> SplitLongToken(XGraphics gfx, XFont font, string token, double maxWidth)
    {
        var buffer = new StringBuilder();

        foreach (var ch in token)
        {
            var candidate = buffer.ToString() + ch;
            if (gfx.MeasureString(candidate, font).Width <= maxWidth)
            {
                buffer.Append(ch);
                continue;
            }

            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }

            buffer.Append(ch);
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }
}
