using System.IO;
using System.Reflection;
using System.Text;

namespace AWSServerSelector.Services;

public static class EmbeddedResourceReader
{
    public static string ReadRequiredText(string relativePath)
    {
        using var stream = OpenRequired(relativePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public static Stream OpenRequired(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Resource path must not be empty.", nameof(relativePath));
        }

        var assembly = Assembly.GetExecutingAssembly();
        var suffix = "." + relativePath.Replace('\\', '.').Replace('/', '.');
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new FileNotFoundException(
                $"Embedded resource '{relativePath}' was not found in assembly '{assembly.GetName().Name}'.");
        }

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Embedded resource stream '{resourceName}' could not be opened.");
    }
}
