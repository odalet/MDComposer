using System;
using System.IO;
using Delta.MDComposer.Generation;
using Delta.MDComposer.Logging;
using Delta.MDComposer.Pdf;
using QuestPDF.Previewer;

namespace Delta.MDComposer;

public static class PdfDocumentBuilder
{
    private static readonly ILog log = LogManager.GetLogger(typeof(PdfDocumentBuilder));

    public static void Preview(string inputFileName, bool debugMode = false)
    {
        using var document = new PdfDocument(inputFileName, debugMode);
        document.ShowInPreviewer();
    }

    public static void Export(string inputFileName, string outputFileName, bool debugMode = false)
    {
        if (string.IsNullOrEmpty(outputFileName)) throw new ArgumentException(
            $"{nameof(outputFileName)} is mandatory", nameof(outputFileName));

        var outputDirectoryPath = Path.GetDirectoryName(outputFileName) ?? "";
        if (!Directory.Exists(outputDirectoryPath))
            _ = Directory.CreateDirectory(outputDirectoryPath);

        if (File.Exists(outputFileName)) log.Warn(
            $"File {outputFileName} already exists: It will be overwritten");
        log.Info($"Exporting file {outputFileName}");

        // See https://github.com/QuestPDF/QuestPDF/issues/195
        using var document = new PdfDocument(inputFileName, debugMode);
        var bytes = document.GenerateBytes();

        try
        {
            // Let's try to generate bookmarks
            var processor = new PdfPostProcessor(bytes);
            processor.Execute();
            processor.Save(outputFileName);
        }
        catch (Exception ex)
        {
            // If this fails, let's save the original pdf
            log.Warn($"Could not generate bookmarks: {ex.Message}", ex);
            File.WriteAllBytes(outputFileName, bytes);
        }

        log.Info($"Exporting file {outputFileName}: DONE");
    }
}
