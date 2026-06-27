using Azure;
using Azure.AI.DocumentIntelligence;
using VeridocX.Server.Domain.SaId;

namespace VeridocX.Server.Services;

public record IdExtractionResult(string? IdNumber, IReadOnlyList<string> Candidates);

public interface IIdDocumentExtractor
{
    Task<IdExtractionResult> ExtractAsync(BinaryData image, CancellationToken ct);
}

public sealed class IdDocumentExtractor(DocumentIntelligenceClient client) : IIdDocumentExtractor
{
    public async Task<IdExtractionResult> ExtractAsync(BinaryData image, CancellationToken ct)
    {
        Operation<AnalyzeResult> operation =
            await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", image, ct);

        var text = operation.Value.Content ?? string.Empty;

        var candidates = SaIdTextScanner.FindCandidates(text);
        var best = candidates.FirstOrDefault(c => SaIdValidator.Validate(c).IsValid)
                   ?? candidates.FirstOrDefault();

        return new IdExtractionResult(best, candidates);
    }
}
