using Caliber.Api.Common;
using Microsoft.Extensions.Options;

namespace Caliber.Api.Storage;

public sealed class AvatarStorageOptions
{
    public const string SectionName = "Avatars";

    public string StoragePath { get; set; } = "App_Data/avatars";

    public long MaxFileSizeBytes { get; set; } = 2_097_152;

    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}

public sealed class LocalFileAvatarStorage(
    IOptions<AvatarStorageOptions> options,
    IHostEnvironment environment)
{
    private readonly AvatarStorageOptions _options = options.Value;

    public async Task<(string StoredFileName, string ContentType, long SizeBytes)> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var extension = NormalizeAvatarExtension(originalFileName);

        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnsupportedMediaTypeException("Only PNG, JPEG, and WebP avatar images are supported.");
        }

        ValidateAvatarContentType(extension, contentType);

        Directory.CreateDirectory(GetRootPath());

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = GetSafePath(storedFileName);

        try
        {
            var header = new byte[12];
            var headerLength = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (headerLength == 0)
            {
                throw new UnsupportedMediaTypeException("The uploaded file is empty.");
            }

            if (headerLength > _options.MaxFileSizeBytes)
            {
                throw new PayloadTooLargeException(_options.MaxFileSizeBytes);
            }

            EvidenceFileValidator.ValidateMagicBytes(header.AsSpan(0, headerLength), extension);

            await using var output = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await output.WriteAsync(header.AsMemory(0, headerLength), cancellationToken);

            var remainingBytes = await CopyWithLimitAsync(
                content,
                output,
                _options.MaxFileSizeBytes - headerLength,
                _options.MaxFileSizeBytes,
                cancellationToken);
            var totalBytes = headerLength + remainingBytes;

            return (storedFileName, ResolveContentTypeForExtension(extension), totalBytes);
        }
        catch
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            throw;
        }
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(storedFileName);
        if (!File.Exists(fullPath))
        {
            throw new NotFoundException("Avatar", storedFileName);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string? storedFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return Task.CompletedTask;
        }

        var fullPath = GetSafePath(storedFileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public string ResolveContentType(string storedFileName)
    {
        var extension = Path.GetExtension(storedFileName).ToLowerInvariant();
        return ResolveContentTypeForExtension(extension);
    }

    private static string ResolveContentTypeForExtension(string extension) =>
        extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };

    private static string NormalizeAvatarExtension(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new UnsupportedMediaTypeException("The file must have a supported extension.");
        }

        extension = extension.ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        {
            throw new UnsupportedMediaTypeException("Only PNG, JPEG, and WebP avatar images are supported.");
        }

        return extension;
    }

    private static void ValidateAvatarContentType(string extension, string contentType)
    {
        var allowed = extension switch
        {
            ".png" => new[] { "image/png" },
            ".webp" => new[] { "image/webp" },
            _ => new[] { "image/jpeg" },
        };

        if (string.IsNullOrWhiteSpace(contentType)
            || !allowed.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnsupportedMediaTypeException(
                "The declared content type does not match the file extension.");
        }
    }

    private string GetRootPath() =>
        Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.StoragePath));

    private string GetSafePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)
            || storedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || storedFileName.Contains("..", StringComparison.Ordinal))
        {
            throw new BadRequestException("The stored file name is invalid.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(GetRootPath(), storedFileName));
        if (!fullPath.StartsWith(GetRootPath(), StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("The stored file name is invalid.");
        }

        return fullPath;
    }

    private static async Task<long> CopyWithLimitAsync(
        Stream input,
        Stream output,
        long maxBytes,
        long maxTotalBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;

        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > maxBytes)
            {
                throw new PayloadTooLargeException(maxTotalBytes);
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return total;
    }
}
