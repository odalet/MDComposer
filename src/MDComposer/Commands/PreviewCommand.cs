using System;
using System.IO;
using NLog;

namespace Delta.MDComposer.Commands;

using static CommandUtils;

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
            return KO;
        }

        if (!File.Exists(input.FullName))
        {
            log.Error($"Input file '{input}' does not exist");
            return KO;
        }

        log.Info($"Previewing '{input.FullName}'");

        // TODO
        try
        {
            var project = CreateProject(input);
            if (project == null)
            {
                log.Error("Could not load the project");
                return KO;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Could not load the project: {ex.Message}");
            return KO;
        }

        PdfDocumentBuilder.Preview(input.FullName, DebugMode);
        return OK;
    }
}
