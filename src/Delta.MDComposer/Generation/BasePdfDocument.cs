using System;
using System.Diagnostics;
using System.Reflection;
using Delta.MDComposer.Logging;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Delta.MDComposer.Generation;

internal abstract class BasePdfDocument(bool debugMode, string? title = null) : IDocument
{
    private static readonly ILog log = LogManager.GetLogger<BasePdfDocument>();

    protected bool DebugMode { get; } = debugMode;
    protected virtual string DocumentTitle { get; } = title ?? "Document";
    protected DateTimeOffset Now { get; } = DateTimeOffset.Now;

    public abstract void Compose(IDocumentContainer container);

    public virtual DocumentMetadata GetMetadata()
    {
        var version = FileVersionInfo
            .GetVersionInfo(Assembly.GetExecutingAssembly().Location)
            .ProductVersion;

        return new()
        {
            Title = DocumentTitle,
            Author = "Author",
            Creator = $"MDComposer {version}",
            CreationDate = Now.LocalDateTime,
            ModifiedDate = Now.LocalDateTime
        };
    }

    public void Generate(string filename)
    {
        try
        {
            this.GeneratePdf(filename);
        }
        catch (Exception exception)
        {
            try
            {
                var exceptionDocument = new ExceptionDocument(exception);
                exceptionDocument.GeneratePdf(filename);
            }
            catch (Exception ex)
            {
                log.Warn($"Could not generate Error PDF Document '{filename}': {ex.Message}", ex);
            }

            // Let's generate the 'Error' document, then rethrow. The caller will decide what to do with the exception
            throw;
        }
    }

    public byte[] GenerateBytes()
    {
        try
        {
            return this.GeneratePdf();
        }
        catch (Exception exception)
        {
            try
            {
                var exceptionDocument = new ExceptionDocument(exception);
                return exceptionDocument.GeneratePdf();
            }
            catch (Exception ex)
            {
                log.Warn($"Could not generate Error PDF Document: {ex.Message}", ex);
            }

            // Let's generate the 'Error' document, then rethrow. The caller will decide what to do with the exception
            throw;
        }
    }
}
