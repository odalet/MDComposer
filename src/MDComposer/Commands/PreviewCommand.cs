using System.IO;
using NLog;

namespace Delta.MDComposer.Commands;

internal sealed class PreviewCommand(bool verbose, bool debug, FileInfo? input) : BaseCommand(verbose, debug)
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static string Name { get; } = "preview";

    public static int Execute(bool verbose, bool debug, FileInfo? input)
    {
        var me = new PreviewCommand(verbose, debug, input);
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

        log.Info($"Previewing '{input}'");

        PdfDocumentBuilder.Preview(input.FullName, DebugMode);

        return 0;
    }
}
