namespace Delta.MDComposer.Commands;

internal abstract class BaseCommand
{
    protected BaseCommand(bool verbose) =>
        NLogInitializer.UpdateVerbosity(verbose);
}
