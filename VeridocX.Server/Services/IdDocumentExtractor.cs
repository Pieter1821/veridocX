using VeridocX.Server.Domain.SaId;

namespace VeridocX.Server.Services;

public record IdExtractionResult(string? IdNumber, IReadOnlyList<string> Candidates);

public interface IIdDocumentExtractor
{
    Task<IdExtractionResult> ExtractAsync(BinaryData image, CancellationToken ct);
}

public sealed class IdDocumentExtractor(IOcrService ocr) : IIdDocumentExtractor
{
    public async Task<IdExtractionResult> ExtractAsync(BinaryData image, CancellationToken ct)
    {
        var doc = await ocr.ReadAsync(image, ct);

        var candidates = SaIdTextScanner.FindCandidates(doc.Text);
        var best = candidates.FirstOrDefault(c => SaIdValidator.Validate(c).IsValid)
                   ?? candidates.FirstOrDefault();

        return new IdExtractionResult(best, candidates);
    }
}
