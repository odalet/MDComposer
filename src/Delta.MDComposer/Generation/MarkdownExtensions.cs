using System;
using System.Collections.Generic;
using Markdig;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Delta.MDComposer.Generation;

internal static class MarkdownExtensions
{
    public static void Markdown(this IContainer container, string markdown, RenderOptions? options = null) =>
        Markdown(container, MarkdownContent.FromString(markdown), options);

    public static void Markdown(this IContainer container, MarkdownContent markdown, RenderOptions? options = null) =>
        container.Component(new MarkdownRenderer(markdown, options));

    internal static IContainer PaddedDebugArea(this IContainer container, string label, string color) =>
        container.DebugArea(label, color).PaddingTop(20);

    internal static TextSpanDescriptor ApplyStyles(this TextSpanDescriptor span, IList<Func<TextSpanDescriptor, TextSpanDescriptor>> applyStyles)
    {
        foreach (var applyStyle in applyStyles)
            span = applyStyle(span);

        return span;
    }
}
