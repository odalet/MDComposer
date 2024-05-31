using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Delta.MDComposer.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace Delta.MDComposer.Pdf;

// Uses PDFSharp to build the bookmarks list
// See https://github.com/QuestPDF/QuestPDF/issues/195
internal class PdfPostProcessor
{
    private sealed record PdfPageReference(string Destination, int PageNumber, double Top);

    private static readonly ILog log = LogManager.GetLogger<PdfPostProcessor>();
    private readonly PdfDocument document;

    public PdfPostProcessor(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        document = PdfReader.Open(stream, PdfDocumentOpenMode.Modify);
    }

    public void Save(string filename) => document.Save(filename);

    public void Execute()
    {
        // examine all the defined destinations in the PDF
        if (document.Internals.Catalog.Elements["/Dests"] is not PdfReference destinationsReference)
        {
            log.Warn("Could not find any 'PdfReference' dictionary. Nothing to do.");
            return;
        }

        if (destinationsReference.Value is not PdfDictionary destinationsMap)
        {
            log.Warn("'Destinations Reference' is not a dictionary. Nothing to do.");
            return;
        }

        var pageReferences = BuildPageReferences(destinationsMap.Elements);
        GenerateOutlines(pageReferences);
    }

    private List<PdfPageReference> BuildPageReferences(PdfDictionary.DictionaryElements elements)
    {
        static double toDouble(PdfItem item) => item switch
        {
            PdfInteger integer => integer.Value,
            PdfReal real => real.Value,
            _ => 0.0
        };

        var pageReferences = new List<PdfPageReference>();
        foreach (var element in elements)
        {
            // element.Value is the link value which is always an array
            // see section 8.2.1 Destinations for all the possibilities
            // NOTE: we're only handling the situations that we generate via QuestPDF
            //   [page type ... ] for example: [page /XYZ left top zoom]
            if (element.Value is not PdfArray pdfArray)
                continue; // Skip

            // find the referenced page (it's always the first element in the array and should always exist)
            // and by the way, the array should always be at least 4-element long (we'll need up to items[3])
            if (pdfArray.Elements.Items.Length < 4) continue;
            if (pdfArray.Elements.Items[0] is not PdfReference pdfReference) continue;
            if (pdfArray.Elements.Items[1] is not PdfName destinationType) continue;

            // Let's find the taget page number
            var pageNumber = -1;
            for (var currentPageNumber = 0; currentPageNumber < document.PageCount; currentPageNumber++)
            {
                var page = document.Pages[currentPageNumber];
                if (page.Reference?.ObjectID == pdfReference.ObjectID)
                {
                    pageNumber = currentPageNumber;
                    break;
                }
            }

            if (pageNumber == -1) continue; // No target page found

            // QuestPDF generates XYZ destinations so we only handle those right now
            if (destinationType.Value != "/XYZ") continue;

            var top = toDouble(pdfArray.Elements.Items[3]);

            // element.Key is the link name
            pageReferences.Add(new(element.Key, pageNumber, top));
        }

        return pageReferences;
    }

    private void GenerateOutlines(List<PdfPageReference> pageReferences)
    {
        // First, let's extract the sections hierarchy
        var definitions = new List<(string key, string title, int page, double top, string parentKey)>();
        foreach (var pageReference in pageReferences)
        {
            var sections = Parse(pageReference.Destination);
            if (sections.Length == 0) continue;

            var key = string.Join('/', sections);
            var parentKey = sections.Length == 1 ? "" : string.Join('/', sections.Take(sections.Length - 1));
            definitions.Add((key, sections[^1], pageReference.PageNumber, pageReference.Top, parentKey));
        }

        var map = new Dictionary<string, PdfOutline>();

        foreach (var (key, title, page, top, parentKey) in definitions)
        {
            if (map.ContainsKey(key))
                continue;

            var outline = new PdfOutline
            {
                Title = title,
                PageDestinationType = PdfPageDestinationType.Xyz,
                DestinationPage = document.Pages[page],
                Top = top
            };

            map.Add(key, outline);
            if (string.IsNullOrEmpty(parentKey))
                document.Outlines.Add(outline);
            else if (map.TryGetValue(parentKey, out var parentOutline))
                parentOutline.Outlines.Add(outline);
            else // Should have a parent, but not found, let's add it to the document (Should never happen)
                document.Outlines.Add(outline);
        }
    }

    private static string[] Parse(string sectionName)
    {
        var name = sectionName;
        try
        {
            name = HttpUtility.UrlDecode(sectionName);
        }
        catch (Exception ex)
        {
            log.Warn($"Could not url-decode the string '{sectionName}'; using it as-is", ex);
        }

        var parts = new List<string>();
        var current = new StringBuilder();
        var index = name.StartsWith('/') ? 1 : 0; // Eat the leading / (it was added by the PDF format)
        while (index < name.Length)
        {
            var c = name[index];
            switch (c)
            {
                case '+':
                    _ = current.Append(' '); // sometimes, + is used to represent a space...
                    break;
                case '/':
                    if (index + 1 <= name.Length && name[index + 1] == '/')
                    {
                        // Escaped /
                        _ = current.Append('/');
                        index++; // consume the additional /
                    }
                    else
                    {
                        // section separator
                        parts.Add(current.ToString());
                        _ = current.Clear();
                    }
                    break;
                default:
                    _ = current.Append(c);
                    break;
            }

            index++;
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return [.. parts];
    }
}