using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Delta.MDComposer.Logging;

namespace Delta.MDComposer.ProjectModel;

internal sealed class ProjectParser
{
    private static readonly ILog log = LogManager.GetLogger(typeof(ProjectParser));
    private readonly FileInfo file;
    private readonly string rootDirectory;

    public ProjectParser(FileInfo fileInfo)
    {
        file = fileInfo;
        rootDirectory = file.DirectoryName ?? "";
        log.Info($"Loading Project '{file.Name}' from '{rootDirectory}'");
    }

    public IProject? Parse()
    {
        try
        {
            var xdoc = XDocument.Load(file.FullName, LoadOptions.SetLineInfo);
            var root = xdoc.Root;
            if (root == null)
            {
                log.Error("Invalid project: no root tag");
                return null;
            }

            var document = ParseInput(root);
            if (document.Length == 0)
            {
                log.Error($"Invalid project: No input documents are defined ({FormatLineInfo(root)})");
                return null;
            }

            return new Project(document);
        }
        catch (Exception ex)
        {
            log.Error($"Invalid project: {ex.Message}", ex);
            return null;
        }
    }

    private Document[] ParseInput(XElement projectRoot)
    {
        var inputElement = projectRoot.Element("input");
        if (inputElement == null)
            return [];

        var documents = new List<Document>();
        var mdDocumentCount = 0;
        foreach (var child in inputElement.Elements())
        {
            if (child.Name == "toc")
            {
                var document = ParseToc(child);
                if (document == null)
                    log.Warn($"Invalid Toc Element; ignoring ({FormatLineInfo(child)})");
                else documents.Add(document);
            }
            else if (child.Name == "markdown")
            {
                var document = ParseMarkdown(child);
                if (document == null)
                    log.Warn($"Invalid Markdown Element; ignoring ({FormatLineInfo(child)})");
                else
                {
                    mdDocumentCount++;
                    documents.Add(document);
                }
            }
        }

        if (mdDocumentCount == 0)
        {
            log.Error($"Invalid project: At least one markdown entry is required in the input section ({FormatLineInfo(inputElement)})");
            return [];
        }
        else return [.. documents];
    }

    private static TocDocument? ParseToc(XElement _) => new();

    private MarkdownDocument? ParseMarkdown(XElement xe)
    {
        var fileName = xe.Attribute("file")?.Value ?? "";
        if (string.IsNullOrEmpty(fileName))
        {
            log.Error($"Invalid Markdown Element: the file attribute is mandatory ({FormatLineInfo(xe)})");
            return null;
        }

        var fullName = Path.Combine(rootDirectory, fileName);
        var file = new FileInfo(fullName);  
        log.Trace($"Adding Markdown file '{file.FullName}'");
        return new MarkdownDocument(file);
    }

    private static string FormatLineInfo(XObject xo)
    {
        var xli = (IXmlLineInfo)xo;
        return $"{xli.LineNumber}, {xli.LinePosition}";
    }
}