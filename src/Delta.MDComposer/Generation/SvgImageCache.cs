using System;
using System.Text;
using Delta.MDComposer.Logging;
using Delta.MDComposer.Pdf;

namespace Delta.MDComposer.Generation;

internal sealed class SvgImageCache : IDisposable
{
    private static readonly ILog log = LogManager.GetLogger<SvgImageCache>();

    public SvgImageCache()
    {
        ErrorIcon = Load(Resources.error);
        WarningIcon = Load(Resources.warning);
        InfoIcon = Load(Resources.info);
    }

    public SvgData? ErrorIcon { get; }
    public SvgData? WarningIcon { get; }
    public SvgData? InfoIcon { get; }

    public void Dispose()
    {
        ErrorIcon?.Dispose();
        WarningIcon?.Dispose();
        InfoIcon?.Dispose();
    }

    private static SvgData? Load(byte[] svg) => Load(Encoding.UTF8.GetString(svg)); 
    private static SvgData? Load(string svg)
    {
        try { return SvgData.LoadFrom(svg); }
        catch (Exception ex)
        {
            log.Error($"Could not load SVG: {ex.Message}", ex);
            return null;
        }
    }
}
