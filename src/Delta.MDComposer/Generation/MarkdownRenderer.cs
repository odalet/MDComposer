using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Delta.MDComposer.Generation;

internal sealed class MarkdownRenderer(MarkdownContent document, RenderOptions? options = null) : IComponent
{
    // This allows us to return "nothing" as if it were a value and hence use switch expressions :)
    private readonly struct Void { public static readonly Void Value = new(); }

    private sealed class TextProperties
    {
        public Stack<Func<TextSpanDescriptor, TextSpanDescriptor>> TextStyles { get; } = new();
        public string? LinkUrl { get; set; }
        public bool IsImage { get; set; }
    }

    private readonly RenderOptions options = options ?? new RenderOptions();
    private readonly TextProperties textProperties = new();

    public void Compose(IContainer container) => ProcessContainerBlock(document.Document, container);

    // Processes a ContainerBlock. Containers blocks contain other containers blocks or regular blocks (LeafBlock).
    // Container blocks are represented by a QuestPDF column with a row for each child item
    private IContainer ProcessContainerBlock(ContainerBlock block, IContainer pdf)
    {
        if (block.Count == 0) return pdf;

        if (options.Debug && block is not MarkdownDocument)
            pdf = pdf.PaddedDebugArea(block.GetType().Name, Colors.Blue.Medium);

        // Push any styles that should be applied to the entire container on the stack
        switch (block)
        {
            case QuoteBlock:
                pdf = pdf.BorderLeft(options.BlockQuoteBorderThickness)
                    .BorderColor(options.BlockQuoteBorderColor)
                    .PaddingLeft(10);
                textProperties.TextStyles.Push(t => t.FontColor(options.BlockQuoteTextColor));
                break;
        }

        if (block is Table table)
            pdf = ProcessTableBlock(table, pdf);
        else
        {
            pdf.Column(col =>
            {
                foreach (var item in block)
                {
                    var container = col.Item();
                    if (block is ListBlock list && item is ListItemBlock listItem)
                    {
                        col.Spacing(options.ListItemSpacing);
                        container.Row(li =>
                        {
                            li.Spacing(5);
                            _ = li.AutoItem().PaddingLeft(10).Text(list.IsOrdered ? $"{listItem.Order}{list.OrderedDelimiter}" : options.UnorderedListGlyph);
                            _ = ProcessBlock(item, li.RelativeItem());
                        });
                    }
                    else
                    {
                        // Paragraphs inside a list get the same spacing as the list items themselves
                        col.Spacing(item.Parent is ListItemBlock ? options.ListItemSpacing : options.ParagraphSpacing);
                        _ = ProcessBlock(item, container);
                    }
                }
            });
        }

        // Pop any styles that were applied to the entire container off the stack
        switch (block)
        {
            case QuoteBlock:
                _ = textProperties.TextStyles.Pop();
                break;
        }

        return pdf;
    }

    private IContainer ProcessTableBlock(Table table, IContainer pdf)
    {
        pdf.Table(td =>
        {
            td.ColumnsDefinition(cd =>
            {
                foreach (var col in table.ColumnDefinitions) // Width is set to 0 for relative columns
                    if (col.Width > 0)
                        cd.ConstantColumn(col.Width);
                    else
                        cd.RelativeColumn();
            });

            var rowIndex = 1;
            var rows = table.OfType<TableRow>().ToList();
            foreach (var row in rows)
            {
                if (row.IsHeader) textProperties.TextStyles.Push(t => t.Bold());

                var columnIndex = 0;
                var cells = row.OfType<TableCell>().ToList();
                foreach (var cell in cells)
                {
                    var colDef = table.ColumnDefinitions[columnIndex];
                    var container = td.Cell()
                        .RowSpan((uint)cell.RowSpan)
                        .Row((uint)rowIndex + 1)
                        .Column((uint)(cell.ColumnIndex >= 0 ? cell.ColumnIndex : columnIndex) + 1)
                        .ColumnSpan((uint)cell.RowSpan)
                        .BorderBottom(rowIndex < rows.Count ? (row.IsHeader ? options.TableHeaderBorderThickness : options.TableBorderThickness) : 0)
                        .BorderColor(options.TableBorderColor)
                        .Background(rowIndex % 2 == 0 ? options.TableEvenRowBackgroundColor : options.TableOddRowBackgroundColor)
                        .Padding(5);

                    switch (colDef.Alignment)
                    {
                        case TableColumnAlign.Left:
                            container = container.AlignLeft();
                            break;
                        case TableColumnAlign.Center:
                            container = container.AlignCenter();
                            break;
                        case TableColumnAlign.Right:
                            container = container.AlignRight();
                            break;
                    }

                    _= ProcessBlock(cell, container);
                    columnIndex++;
                }

                if (row.IsHeader)
                    _ = textProperties.TextStyles.Pop();

                rowIndex++;
            }
        });

        return pdf;
    }

    private Void ProcessContainerBlockNoReturn(ContainerBlock block, IContainer pdf)
    {
        _ = ProcessContainerBlock(block, pdf);
        return Void.Value;
    }

    // Processes a Block, which can be a ContainerBlock or a LeafBlock.
    private Void ProcessBlock(Block block, IContainer pdf) => block switch
    {
        ContainerBlock container => ProcessContainerBlockNoReturn(container, pdf),
        LeafBlock leaf => ProcessLeafBlock(leaf, pdf),
        _ => Void.Value
    };

    private Void ProcessLeafBlock(LeafBlock block, IContainer pdf) 
    {
        if (options.Debug) 
            pdf = pdf.PaddedDebugArea(block.GetType().Name, Colors.Red.Medium);

        // Push any styles that should be applied to the entire block on the stack
        switch (block)
        {
            case HeadingBlock heading:
                textProperties.TextStyles.Push(t => t.FontSize(Math.Max(0, options.CalculateHeadingSize(heading.Level))).Bold());
                break;
        }

        if (block.Inline != null && block.Inline.Any())  pdf.Text(text =>
        {
            // Process the block's inline elements
            foreach (var item in block.Inline)
            {
                switch (item)
                {
                    case ContainerInline container:
                        ProcessContainerInline(container, text);
                        break;
                    case LeafInline leaf:
                        ProcessLeafInline(leaf, text);
                        break;
                }
            }
        });
        else if (block is ThematicBreakBlock) pdf
                .LineHorizontal(options.HorizontalRuleThickness)
                .LineColor(options.HorizontalRuleColor);
        else if (block is CodeBlock code) _ = pdf
                .Background(options.CodeBlockBackground)
                .Padding(5)
                .Text(code.Lines.ToString())
                .FontFamily(options.CodeFont);

        // Pop any styles that were applied to the entire block off the stack
        switch (block)
        {
            case HeadingBlock:
                _ = textProperties.TextStyles.Pop();
                break;
        }

        return Void.Value; 
    }

    private void ProcessContainerInline(ContainerInline inline, TextDescriptor text)
    {
        foreach (var item in inline)
        {
            // Push any styles that should be applied to the entire span on the stack
            switch (inline)
            {
                case LinkInline link:
                    textProperties.TextStyles.Push(t => t
                        .FontColor(options.LinkTextColor)
                        .DecorationColor(options.LinkTextColor)
                        .Underline());
                    textProperties.LinkUrl = link.Url;
                    textProperties.IsImage = link.IsImage;
                    break;
                case EmphasisInline emphasis:
                    textProperties.TextStyles.Push(t => (emphasis.DelimiterChar, emphasis.DelimiterCount) switch
                    {
                        ('^', 1) => t.Superscript(),
                        ('~', 1) => t.Subscript(),
                        ('~', 2) => t.Strikethrough(),
                        ('+', 2) => t.Underline(),
                        ('=', 2) => t.BackgroundColor(options.MarkedTextBackgroundColor),
                        _ => emphasis.DelimiterCount == 2 ? t.Bold() : t.Italic(),
                    });
                    break;
            }

            switch (item)
            {
                case ContainerInline container:
                    ProcessContainerInline(container, text);
                    break;
                case LeafInline leaf:
                    ProcessLeafInline(leaf, text);
                    break;
            }

            // Pop any styles that were applied to the entire span off the stack
            if (inline is EmphasisInline or LinkInline)
                _ = textProperties.TextStyles.Pop();

            // Reset the link URL
            textProperties.LinkUrl = null;
            textProperties.IsImage = false;
        }
    }

    private void ProcessLeafInline(LeafInline inline, TextDescriptor text)
    {
        switch (inline)
        {
            case AutolinkInline autoLink:
                var linkSpan = text.Hyperlink(autoLink.Url, autoLink.Url);
                linkSpan.ApplyStyles(textProperties.TextStyles.ToList());
                break;
            case LineBreakInline:
                // Ignore markdown line breaks, they are used for formatting the source code.
                //span = text.Span("\n");
                break;
            case TaskList task:
                _ = text.Span(task.Checked ? options.TaskListCheckedGlyph : options.TaskListUncheckedGlyph)
                    .FontFamily(options.UnicodeGlyphFont);
                break;
            case LiteralInline literal:
                ProcessLiteralInline(literal, text)
                    .ApplyStyles(textProperties.TextStyles.ToList());
                break;
            case CodeInline code:
                _ = text.Span(code.Content)
                    .BackgroundColor(options.CodeInlineBackground)
                    .FontFamily(options.CodeFont);
                break;
            case HtmlEntityInline htmlEntity:
                _ = text.Span(htmlEntity.Transcoded.ToString());
                break;
            default:
                _ = text.Span($"Unknown LeafInline: {inline.GetType()}").BackgroundColor(Colors.Orange.Medium);
                break;
        }
    }

    private TextSpanDescriptor ProcessLiteralInline(LiteralInline literal, TextDescriptor text)
    {
        // Plain text
        if (string.IsNullOrEmpty(textProperties.LinkUrl)) return text.Span(literal.ToString());

        // Regular links, or images that could not be downloaded
        if (!textProperties.IsImage || !document.TryGetImageFromCache(textProperties.LinkUrl, out var image))
            return text.Hyperlink(literal.ToString(), textProperties.LinkUrl);

        // Images
        text.Element(e => e
            .Width(image.Width * options.ImageScalingFactor)
            .Height(image.Height * options.ImageScalingFactor)
            .Image(image.Image)
            .FitArea());

        return text.Span(string.Empty);
    }
}
