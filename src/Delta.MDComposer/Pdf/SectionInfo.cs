using System.Web;

namespace Delta.MDComposer.Pdf;

internal sealed class SectionInfo(SectionInfo? parent, string text)
{
    public SectionInfo(string text) : this(null, text) { }

    public SectionInfo? Parent { get; } = parent;
    public string Name { get; } = MakeName(parent, text);
    public string Text { get; } = text;
    public int Level { get; } = 1 + (parent?.Level ?? 0);
    public bool ShowInToc { get; init; } = true;

    // We separate sections with /. If the name already contains the character /, it is replaced with //.
    // And finally the result is url-encoded so as not to loose special characters (like °) when post-processing
    private static string MakeName(SectionInfo? parent, string name)
    {
        var result = name.Replace("/", "//");
        if (parent != null)
            result = parent.Name + "/" + result;
        return HttpUtility.UrlEncode(result);
    }
}