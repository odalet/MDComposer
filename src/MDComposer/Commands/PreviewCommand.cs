using System.IO;
using NLog;

namespace Delta.MDComposer.Commands;

internal sealed class PreviewCommand(bool verbose, FileInfo? input) : BaseCommand(verbose)
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static string Name { get; } = "preview";

    public static int Execute(bool verbose, FileInfo? input)
    {
        var me = new PreviewCommand(verbose, input);
        return me.Run();
    }

    private int Run()
    {
        if (!ValidateArgs()) return -1;

        log.Info($"Previewing '{input}'");
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

        return true;
    }
}
