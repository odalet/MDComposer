using System.IO;
using NLog;

namespace Delta.MDComposer.Commands;

internal sealed class PdfCommand(bool verbose, bool debug, FileInfo? input, FileInfo? output) : BaseCommand(verbose, debug)
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static string Name { get; } = "pdf";

    public static int Execute(bool verbose, bool debug, FileInfo? input, FileInfo? output)
    {
        var me = new PdfCommand(verbose, debug, input, output);
        return me.Run();
    }

    private int Run()
    {
        if (input == null)
        {
            log.Error($"Input file is mandatory. Try {Program.AppName} {Name} -h");
            return -1;
        }

        if (!File.Exists(input.FullName))
        {
            log.Error($"Input file '{input}' does not exist");
            return -1;
        }

        if (output == null)
        {
            var outputFileName = Path.ChangeExtension(input.FullName, ".pdf");
            output = new FileInfo(outputFileName);
            log.Info($"Output file will be '{output}'");
        }

        log.Info($"Generating '{output}' from '{input}'");

        PdfDocumentBuilder.Export(input.FullName, output.FullName, DebugMode);

        return 0;
    }
}
