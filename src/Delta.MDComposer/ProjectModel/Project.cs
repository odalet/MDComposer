using System;
using System.IO;

namespace Delta.MDComposer.ProjectModel;

public interface IProject
{
    Document[] Documents { get; }
}

public sealed class Project : IProject
{
    internal Project(Document[] documents) => Documents = documents;

    public Document[] Documents { get; }

    public static IProject FromMarkdownFile(FileInfo file)
    {
        if (!file.Exists) throw new ArgumentException($"File {file} does not exist");
        var document = new MarkdownDocument(file);
        return new Project([document]);
    }

    public static IProject? Parse(FileInfo file) =>  file.Exists 
        ? new ProjectParser(file).Parse()
        : throw new ArgumentException($"File {file} does not exist");
}

public abstract class Document { }

public sealed class TocDocument : Document { }

public sealed class MarkdownDocument : Document
{
    public MarkdownDocument(FileInfo file)
    {
        if (!file.Exists) throw new ArgumentException($"File {file} does not exist");
        File = file;
    }

    public FileInfo File { get; }
}
