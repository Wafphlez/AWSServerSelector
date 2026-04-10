using System;

namespace AWSServerSelector.Services;

public static class HostsSectionBuilder
{
    public static string Build(string originalHosts, string sectionMarker, string innerContent)
    {
        static string NormalizeToLf(string value) => value.Replace("\r\n", "\n").Replace("\r", "\n");

        var original = NormalizeToLf(originalHosts ?? string.Empty);
        var first = original.IndexOf(sectionMarker, StringComparison.Ordinal);
        var last = first >= 0
            ? original.IndexOf(sectionMarker, first + sectionMarker.Length, StringComparison.Ordinal)
            : -1;

        var inner = NormalizeToLf(innerContent ?? string.Empty);
        if (inner.Length > 0 && !inner.EndsWith("\n", StringComparison.Ordinal))
        {
            inner += "\n";
        }

        if (inner.EndsWith("\n\n", StringComparison.Ordinal))
        {
            inner = inner.TrimEnd('\n') + "\n";
        }

        var wrapped = sectionMarker + "\n" + inner + sectionMarker;
        string result;

        if (first >= 0 && last >= 0)
        {
            var afterLast = last + sectionMarker.Length;
            result = original[..first] + wrapped + original[afterLast..];
        }
        else if (first >= 0)
        {
            result = original[..first] + wrapped;
        }
        else
        {
            var suffix = (original.EndsWith("\n", StringComparison.Ordinal) ? string.Empty : "\n") + "\n" + wrapped;
            result = original + suffix;
        }

        return result.Replace("\n", "\r\n");
    }
}
