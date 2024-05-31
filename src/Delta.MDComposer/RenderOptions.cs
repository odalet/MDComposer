using System;
using System.Runtime.InteropServices;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Delta.MDComposer;

public sealed class RenderOptions
{
    public bool Debug { get; set; }

    public Color LinkTextColor { get; set; } = Colors.Blue.Medium;
    public Color MarkedTextBackgroundColor { get; set; } = Colors.Yellow.Lighten2;

    public Color BlockQuoteBorderColor { get; set; } = Colors.Grey.Lighten2;
    public Color BlockQuoteTextColor { get; set; } = Colors.Grey.Darken1;
    public int BlockQuoteBorderThickness { get; set; } = 2;

    public string CodeFont { get; set; } = Fonts.CourierNew;
    public Color CodeBlockBackground { get; set; } = Colors.Grey.Lighten4;
    public Color CodeInlineBackground { get; set; } = Colors.Grey.Lighten3;

    public string TaskListCheckedGlyph { get; set; } = "☑";
    public string TaskListUncheckedGlyph { get; set; } = "☐";
    public string UnicodeGlyphFont { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Arial Unicode MS" : Fonts.Arial;

    public Color TableBorderColor { get; set; } = Colors.Grey.Lighten2;
    public Color TableEvenRowBackgroundColor { get; set; } = Colors.Grey.Lighten4;
    public Color TableOddRowBackgroundColor { get; set; } = Colors.White;
    public int TableHeaderBorderThickness { get; set; } = 3;
    public int TableBorderThickness { get; set; } = 1;

    public Color HorizontalRuleColor { get; set; } = Colors.Grey.Lighten2;
    public int HorizontalRuleThickness { get; set; } = 2;

    public float ImageScalingFactor { get; set; } = 0.5f; // The conversion factor used to scale images from pixel size to point size

    public float ParagraphSpacing { get; set; } = 10;
    public float ListItemSpacing { get; set; } = 5;
    public string UnorderedListGlyph { get; set; } = "•";

    // The formula used to calculate heading sizes based on their level.
    // Level is non zero-indexed and starts at 1 for the largest heading
    public Func<int, int> CalculateHeadingSize { get; set; } = level => 28 - 2 * (level - 1);
}
