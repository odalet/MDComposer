using System.IO;
using Delta.MDComposer.ProjectModel;

namespace Delta.MDComposer.Commands;

internal abstract class BaseCommand
{
    protected BaseCommand(bool verbose, bool debug)
    {
        Verbose = verbose;
        DebugMode = debug;
        NLogInitializer.UpdateVerbosity(verbose);
    }

    protected bool Verbose { get; }
    protected bool DebugMode { get; }
}
