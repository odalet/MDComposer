using System;
using Delta.MDComposer.Logging;
using QuestPDF.Infrastructure;
using SkiaSharp;
using Svg;
using Svg.Model;
using Svg.Skia;

namespace Delta.MDComposer.Pdf;

internal static class SvgHelper
{
    private static readonly ILog log = LogManager.GetLogger(typeof(SvgHelper));

    public static void Draw(this SKCanvas canvas, SvgData svg, Size availableSize,
        HorizontalAlignment halign = HorizontalAlignment.Center,
        VerticalAlignment valign = VerticalAlignment.Middle,
        ImageScaling scaling = ImageScaling.FitArea)
    {
        if (svg.Picture == null)
        {
            log.Warn("No image to render");
            return;
        }

        var pictureSize = new Size(svg.Bounds.Width, svg.Bounds.Height);
        canvas.Draw(svg.Picture, pictureSize, availableSize, halign, valign, scaling);
    }

    private static void Draw(this SKCanvas canvas, SKPicture picture, Size pictureSize, Size availableSize, HorizontalAlignment halign, VerticalAlignment valign, ImageScaling scaling)
    {
        _ = canvas.Save();

        var sx = 1f;
        var sy = 1f;

        switch (scaling)
        {
            case ImageScaling.FitWidth:
                sx = availableSize.Width / pictureSize.Width;
                sy = sx;
                break;
            case ImageScaling.FitHeight:
                sy = availableSize.Height / pictureSize.Height;
                sx = sy;
                break;
            case ImageScaling.FitArea:
                var ratio = availableSize.Width / pictureSize.Width;
                if (ratio > 1f)
                {
                    sx = availableSize.Width / pictureSize.Width;
                    sy = sx;
                }
                else
                {
                    sy = availableSize.Height / pictureSize.Height;
                    sx = sy;
                }

                break;
            case ImageScaling.Resize:
                sx = availableSize.Width / pictureSize.Width;
                sy = availableSize.Height / pictureSize.Height;
                break;
            default: break;
        }

        var (w, h) = (sx * pictureSize.Width, sy * pictureSize.Height);
        var tx = halign switch
        {
            HorizontalAlignment.Left => 0f,
            HorizontalAlignment.Center => 0.5f * (availableSize.Width - w),
            HorizontalAlignment.Right => availableSize.Width - w,
            _ => 0f
        };

        var ty = valign switch
        {
            VerticalAlignment.Top => 0f,
            VerticalAlignment.Middle => 0.5f * (availableSize.Height - h),
            VerticalAlignment.Bottom => availableSize.Height - h,
            _ => 0f
        };

        canvas.Translate(tx, ty);
        canvas.Scale(sx, sy);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }
}

// Adapted from Svg.Skia.SKSvg
internal sealed class SvgData : IDisposable
{
    private SvgData(SvgDocument document)
    {
        Bounds = SKRect.Empty;

        var Settings = new SKSvgSettings();
        var skiaModel = new SkiaModel(Settings);
        var AssetLoader = new SkiaAssetLoader(skiaModel);

        var Model = SvgExtensions.ToModel(document, AssetLoader, out _, out var rect);
        if (!rect.HasValue) throw new InvalidOperationException("Could not determine the bounds of the SVG picture");

        Bounds = new SKRect(rect.Value.Left, rect.Value.Top, rect.Value.Right, rect.Value.Bottom);
        Picture = skiaModel.ToSKPicture(Model) ?? throw new InvalidOperationException("Could not load an SVG picture");
    }

    public SKPicture Picture { get; }
    public SKRect Bounds { get; }

    public void Dispose() => Picture.Dispose();

    public static SvgData LoadFrom(string svg) => new(SvgDocument.FromSvg<SvgDocument>(svg));
}