namespace Caliber.Api.Storage;

public interface IEvidenceStorage
{
    Task<StoredEvidenceFile> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);
}

public sealed record StoredEvidenceFile(string StoredFileName, long SizeBytes);
