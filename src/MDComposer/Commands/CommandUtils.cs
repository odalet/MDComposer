using System.IO;
using Delta.MDComposer.ProjectModel;

namespace Delta.MDComposer.Commands;

internal static class CommandUtils
{
    public const int OK = 0;
    public const int KO = 1;

    public static IProject? CreateProject(FileInfo inputFile) =>
        inputFile.Extension.Equals(".mdcproj", System.StringComparison.InvariantCultureIgnoreCase)
        ? Project.Parse(inputFile)
        : Project.FromMarkdownFile(inputFile);
}
