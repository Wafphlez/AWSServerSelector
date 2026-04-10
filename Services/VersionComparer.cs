using System;
using System.Linq;

namespace AWSServerSelector.Services;

public static class VersionComparer
{
    public static int Compare(string? version1, string? version2)
    {
        if (string.IsNullOrWhiteSpace(version1) || string.IsNullOrWhiteSpace(version2))
        {
            return 0;
        }

        var v1Parts = version1.Split('.');
        var v2Parts = version2.Split('.');
        var maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var left = i < v1Parts.Length ? ParsePart(v1Parts[i]) : 0;
            var right = i < v2Parts.Length ? ParsePart(v2Parts[i]) : 0;

            if (left > right) return 1;
            if (left < right) return -1;
        }

        return 0;
    }

    private static int ParsePart(string part)
    {
        if (string.IsNullOrWhiteSpace(part))
        {
            return 0;
        }

        var digits = new string(part.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var result) ? result : 0;
    }
}
