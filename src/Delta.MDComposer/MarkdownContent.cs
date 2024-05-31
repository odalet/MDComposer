using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Delta.MDComposer;

internal sealed record ImageWithDimensions(int Width, int Height, Image Image);

// Very much inspired by https://github.com/christiaanderidder/QuestPDF.Markdown
internal sealed class MarkdownContent
{
    private static readonly HttpClient defaultHttpClient = new();
    private readonly ConcurrentDictionary<string, ImageWithDimensions> images = new();

    private MarkdownContent(string payload)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseEmphasisExtras()
            .UseGridTables()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
            .Build();

        Document = Markdown.Parse(payload, pipeline);
    }

    public MarkdownDocument Document { get; }

    public static MarkdownContent FromFile(string filename) => new(File.ReadAllText(filename));
    public static MarkdownContent FromString(string payload) => new(payload);

    public bool TryGetImageFromCache(string url, [NotNullWhen(true)] out ImageWithDimensions? image) => 
        images.TryGetValue(url, out image);

    public async Task DownloadImages(int imageDownloaderMaxParallelism = 4, HttpClient? httpClient = null)
    {
        var parallelism = Math.Max(1, imageDownloaderMaxParallelism);
        var semaphore = new SemaphoreSlim(parallelism);

        var urls = Document.Descendants<LinkInline>()
            .Where(l => l.IsImage && l.Url != null && Uri.IsWellFormedUriString(l.Url, UriKind.Absolute))
            .Select(l => l.Url)
            .ToHashSet();

        // The semaphore is disposed after all tasks have completed, we can safely disable AccessToDisposedClosure
        var tasks = urls.Select(async (url) =>
        {
            if (url == null) return;

            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var client = httpClient ?? defaultHttpClient;
                var stream = await client.GetStreamAsync(url).ConfigureAwait(false);

                // QuestPDF does not allow accessing image dimensions on loaded images
                // To work around this we will parse the image ourselves first and keep track of the dimensions
                using var skImage = SKImage.FromEncodedData(stream);
                var pdfImage = Image.FromBinaryData(skImage.EncodedData.ToArray());

                var image = new ImageWithDimensions(skImage.Width, skImage.Height, pdfImage);
                _ = images.TryAdd(url, image);
            }
            finally
            {
                _ = semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Dispose semaphore after completing all tasks
        semaphore.Dispose();
    }
}
