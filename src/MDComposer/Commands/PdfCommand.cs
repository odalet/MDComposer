using System.IO;
using NLog;

namespace Delta.MDComposer.Commands;

internal sealed class PdfCommand(bool verbose, FileInfo? input, FileInfo? output) : BaseCommand(verbose)
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static string Name { get; } = "pdf";

    public static int Execute(bool verbose, FileInfo? input, FileInfo? output)
    {
        var me = new PdfCommand(verbose, input, output);
        return me.Run();
    }

    private int Run()
    {
        if (!ValidateArgs()) return -1;

        log.Info($"Generating '{output}' from '{input}'");
        return 0;
    }

    private bool ValidateArgs()
    {
        if (input == null)
        {
            log.Error($"Input file is mandatory. Try {Program.AppName} {Name} -h");
            return false;
        }

        if (!File.Exists(input.FullName))
        {
            log.Error($"Input file '{input}' does not exist");
            return false;
        }

        if (output == null)
        {
            var outputFileName = Path.ChangeExtension(input.FullName, ".pdf");
            output = new FileInfo(outputFileName);
            log.Info($"Output file will be '{output}'");
        }

        return true;
    }
}
