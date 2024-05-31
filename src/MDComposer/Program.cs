using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Delta.MDComposer.Commands;

namespace Delta.MDComposer;

internal static class Program
{
    public static string AppName { get; } = Path.GetFileNameWithoutExtension(
        Assembly.GetExecutingAssembly().Location);
    public static string AppVersion { get; } = FileVersionInfo.GetVersionInfo(
        typeof(Program).Assembly.Location).ProductVersion ?? "";

    private static int Main(string[] args)
    {
        NLogInitializer.Execute();
        var command = CreateCommands();
        return command.Invoke(args);
    }

    private static RootCommand CreateCommands()
    {
        var previewCommand = new Command(PreviewCommand.Name, "Preview the document");
        previewCommand.AddArgument(new Argument<FileInfo>("input", "Mandatory: Path to the file to preview"));
        previewCommand.Handler = CommandHandler.Create(PreviewCommand.Execute);

        var pdfCommand = new Command(PdfCommand.Name, "Generate the final document as a PDF file");
        pdfCommand.AddArgument(new Argument<FileInfo>("input", "Mandatory: Path to the file to preview"));
        pdfCommand.AddArgument(new Argument<FileInfo?>("output", () => null, "Path to the output PDF file (if not specified, the input file name with a .pdf extension will be created)"));
        pdfCommand.Handler = CommandHandler.Create(PdfCommand.Execute);

        var rootCommand = new RootCommand(description: "Combines several Markdown documents into a unique output PDF");
        rootCommand.AddGlobalOption(new Option<bool>(["-v", "--verbose"], "Enable verbose output"));
        rootCommand.AddCommand(previewCommand);
        rootCommand.AddCommand(pdfCommand);

        return rootCommand;
    }
}
