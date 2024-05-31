using System;
using System.Collections.Generic;
using Delta.MDComposer.Pdf;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Delta.MDComposer.Generation;

internal sealed class PdfDocument : BasePdfDocument, IDisposable
{
    private abstract class BaseChildElement(PdfDocument owner)
    {
        protected PdfDocument Parent { get; } = owner;
        protected bool DebugMode => Parent.DebugMode;
    }

    private sealed class TestSection(PdfDocument owner) : BaseChildElement(owner)
    {
        public void Compose(IContainer container) => container.Text($"Test Section; Debug Mode: {DebugMode}");
    }

    public PdfDocument(string markdownFile, bool debugMode) : base(debugMode, "Markdown Document")
    {
        Settings.CheckIfAllTextGlyphsAreAvailable = false;
        SvgImages = new();
        Markdown = MarkdownContent.FromFile(markdownFile);
    }

    private static bool EnableCapitalization => true; // try with false to see the difference
    private List<SectionInfo> Sections { get; } = [];
    private SvgImageCache SvgImages { get; }
    private MarkdownContent Markdown { get; }

    private static TextStyle Header(TextStyle style) => style.FontSize(10f).FontColor("#AAA");
    private static TextStyle Footer(TextStyle style) => style.FontSize(10f).SemiBold();
    private static TextStyle Title1(TextStyle style) => style.FontSize(40f).FontColor("#AAA").SemiBold();
    private static TextStyle Title2(TextStyle style) => style.FontSize(14f).SemiBold();

    public void Dispose() => SvgImages.Dispose();

    public override void Compose(IDocumentContainer container)
    {
        Sections.Clear(); // Needed by hot reload...
        container.Page(page =>
        {
            ApplyPageSettings(page);
            // 1st page has no header
            page.Footer().Element(ComposeFooter);
            page.Content().Element(ComposeFirstPage);
        }).Page(page =>
        {
            ApplyPageSettings(page);
            page.Header().Element(ComposeHeader);
            page.Footer().Element(ComposeFooter);
            page.Content().Element(ComposeContent);
        });
    }

    private static void ApplyPageSettings(PageDescriptor page)
    {
        page.Size(PageSizes.A4.Portrait());

        page.MarginHorizontal(1.5f, Unit.Centimetre);
        page.MarginTop(0.75f, Unit.Centimetre);
        page.MarginBottom(0.75f, Unit.Centimetre);

        page.PageColor(Colors.White);

        var baseStyle = TextStyle.Default.FontSize(12);
        page.DefaultTextStyle(baseStyle.FontFamily(Fonts.Arial));
    }

    private void ComposeFirstPage(IContainer container) => container.PaddingTop(5).PaddingBottom(5).Column(c =>
    {
        var summarySection = new SectionInfo("Summary");
        c.Item().AddSection(Sections, summarySection).Element(e => new TestSection(this).Compose(e));
    });

    private void ComposeContent(IContainer container) => container.PaddingTop(5).PaddingBottom(5).Column(c =>
    {
        var content1Section = new SectionInfo("Content #1");
        c.Item().AddSection(Sections, content1Section).Element(e => new TestSection(this).Compose(e));

        var content2Section = new SectionInfo("Content #2");
        c.Item().AddSection(Sections, content2Section).Element(e => new TestSection(this).Compose(e));

        var mdSection = new SectionInfo("MD");
        c.Item().Markdown(Markdown);
    });

    private void ComposeHeader(IContainer container) => container.DefaultTextStyle(Header).Row(r =>
    {
        r.RelativeItem().AlignLeft().AlignBottom().Text(t => t.Span("Test Header"));
        r.AutoItem().AlignRight().AlignBottom().Text(t => t.Span("Test Document"));
    });

    private void ComposeFooter(IContainer container) => container.DefaultTextStyle(Footer).Height(20f).Row(r =>
    {
        // AddUp Horizontal Logo
        r.AutoItem().AlignLeft().AlignBottom().Row(rr => rr
            .ConstantItem(3f, Unit.Centimetre)
            .SkiaSharpCanvas((canvas, size) =>
            {
                if (SvgImages.InfoIcon != null)
                    canvas.Draw(SvgImages.InfoIcon, size, HorizontalAlignment.Left);
            }));

        // Generation date (centered and fills what room's left)
        r.RelativeItem().AlignCenter().AlignBottom().Text(
            t => t.Span(Capitalize($"Generated on {Now:yyyy/MM/dd} at {Now:HH:mm:ss} ({FormatTZ(Now.Offset)})")));

        // Page
        r.AutoItem().AlignRight().AlignBottom().Row(rr =>
        {
            rr.AutoItem().Text(t =>
            {
                _ = t.Span(Capitalize("Page "));
                _ = t.CurrentPageNumber().SemiBold();
                _ = t.Span(Capitalize(" of "));
            });

            // We want the "Number of pages" to be a link to the ToC
            rr.AutoItem().SectionLink(Capitalize("Contents")).Text(
                t => t.TotalPages().SemiBold().FontColor(Colors.DeepPurple.Medium));
        });
    });

    private static string Capitalize(string text) => EnableCapitalization ? text.ToUpper() : text;
    private static string FormatTZ(TimeSpan offset) => $"UTC{(offset > TimeSpan.Zero ? "+" : "-")}{offset:hh\\:mm}";
}