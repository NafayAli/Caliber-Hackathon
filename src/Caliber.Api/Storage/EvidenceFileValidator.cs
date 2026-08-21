using Caliber.Api.Common;

namespace Caliber.Api.Storage;

internal static class EvidenceFileValidator
{
    private static readonly Dictionary<string, string[]> ExtensionContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".png"] = ["image/png"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".webp"] = ["image/webp"],
    };

    public static string NormalizeExtension(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new UnsupportedMediaTypeException("The file must have a supported extension.");
        }

        extension = extension.ToLowerInvariant();
        if (!ExtensionContentTypes.ContainsKey(extension))
        {
            throw new UnsupportedMediaTypeException(
                "Only PDF, PNG, JPEG, and WebP files are supported.");
        }

        return extension;
    }

    public static void ValidateContentType(string extension, string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)
            || !ExtensionContentTypes.TryGetValue(extension, out var allowed)
            || !allowed.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnsupportedMediaTypeException(
                "The declared content type does not match the file extension.");
        }
    }

    public static void ValidateMagicBytes(ReadOnlySpan<byte> header, string extension)
    {
        var valid = extension switch
        {
            ".pdf" => header.Length >= 4
                      && header[0] == 0x25
                      && header[1] == 0x50
                      && header[2] == 0x44
                      && header[3] == 0x46,
            ".png" => header.Length >= 8
                      && header[0] == 0x89
                      && header[1] == 0x50
                      && header[2] == 0x4E
                      && header[3] == 0x47
                      && header[4] == 0x0D
                      && header[5] == 0x0A
                      && header[6] == 0x1A
                      && header[7] == 0x0A,
            ".jpg" or ".jpeg" => header.Length >= 3
                                 && header[0] == 0xFF
                                 && header[1] == 0xD8
                                 && header[2] == 0xFF,
            ".webp" => header.Length >= 12
                       && header[0] == (byte)'R'
                       && header[1] == (byte)'I'
                       && header[2] == (byte)'F'
                       && header[3] == (byte)'F'
                       && header[8] == (byte)'W'
                       && header[9] == (byte)'E'
                       && header[10] == (byte)'B'
                       && header[11] == (byte)'P',
            _ => false,
        };

        if (!valid)
        {
            throw new UnsupportedMediaTypeException(
                "The file contents do not match a supported evidence format.");
        }
    }
}
