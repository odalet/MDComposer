using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Delta.MDComposer.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Delta.MDComposer.Generation;

internal static class Extensions
{
    public static void SkiaSharpCanvas(this IContainer container, Action<SKCanvas, Size> drawOnCanvas) => container.Svg(size =>
    {
        using var stream = new MemoryStream();

        using (var canvas = SKSvgCanvas.Create(new SKRect(0, 0, size.Width, size.Height), stream))
            drawOnCanvas(canvas, size);

        var svgData = stream.ToArray();
        return Encoding.UTF8.GetString(svgData);
    });

    public static void SkiaSharpRasterized(this IContainer container, Action<SKCanvas, Size> drawOnCanvas) => container.Image(payload =>
    {
        using var bitmap = new SKBitmap(payload.ImageSize.Width, payload.ImageSize.Height);
        using (var canvas = new SKCanvas(bitmap))
        {
            var scalingFactor = payload.Dpi / (float)DocumentSettings.DefaultRasterDpi;
            canvas.Scale(scalingFactor);
            drawOnCanvas(canvas, payload.AvailableSpace);
        }

        return bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    });

    public static IContainer AddSection(this IContainer parent, IList<SectionInfo> sections, SectionInfo section)
    {
        sections.Add(section);
        return parent.Section(section.Name);
    }
}