using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VeridocX.Server.Data;
using VeridocX.Server.Domain;
using VeridocX.Server.Domain.SaId;
using VeridocX.Server.Security;
using VeridocX.Server.Services;

namespace VeridocX.Server.Endpoints;

public static class SaIdEndpoints
{
    private static readonly JsonSerializerOptions StorageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static RouteGroupBuilder MapSaIdEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("documents/sa-id", async (
            SaIdRequest request,
            VeridocXDbContext db,
            IIdFingerprinter fingerprinter,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdNumber))
                return Results.Problem(
                    title: "idNumber is required",
                    statusCode: StatusCodes.Status400BadRequest);

            var (jobId, result, previouslySeen) =
                await ValidateAndStoreAsync(request.IdNumber, "sa-id-v1", db, fingerprinter, ct);

            return Results.Created(
                $"/api/documents/{jobId}",
                new SaIdResponse(jobId, result, previouslySeen));
        })
        .WithName("ValidateSaId");

        group.MapPost("documents/sa-id/extract", async (
            IFormFile file,
            VeridocXDbContext db,
            IIdFingerprinter fingerprinter,
            IIdDocumentExtractor extractor,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.Problem(
                    title: "file is required",
                    statusCode: StatusCodes.Status400BadRequest);

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            return await ExtractValidateStoreAsync(
                BinaryData.FromBytes(ms.ToArray()), db, fingerprinter, extractor, ct);
        })
        .WithName("ExtractSaIdFromUpload")
        .DisableAntiforgery();

        group.MapPost("documents/sa-id/extract-base64", async (
            ExtractBase64Request request,
            VeridocXDbContext db,
            IIdFingerprinter fingerprinter,
            IIdDocumentExtractor extractor,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.ImageBase64))
                return Results.Problem(
                    title: "imageBase64 is required",
                    statusCode: StatusCodes.Status400BadRequest);

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(StripDataUrl(request.ImageBase64));
            }
            catch (FormatException)
            {
                return Results.Problem(
                    title: "imageBase64 is not valid Base64",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return await ExtractValidateStoreAsync(
                BinaryData.FromBytes(bytes), db, fingerprinter, extractor, ct);
        })
        .WithName("ExtractSaIdFromBase64");

        group.MapGet("documents/{id:guid}", async (
            Guid id,
            VeridocXDbContext db,
            CancellationToken ct) =>
        {
            var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
            return job is null
                ? Results.NotFound()
                : Results.Ok(new JobResponse(
                    job.Id,
                    job.DocumentType.ToString(),
                    job.Status.ToString(),
                    job.IsValid,
                    job.Subject,
                    job.ResultJson,
                    job.CreatedAt));
        })
        .WithName("GetDocument");

        return group;
    }

    private static async Task<IResult> ExtractValidateStoreAsync(
        BinaryData image,
        VeridocXDbContext db,
        IIdFingerprinter fingerprinter,
        IIdDocumentExtractor extractor,
        CancellationToken ct)
    {
        var extraction = await extractor.ExtractAsync(image, ct);

        if (extraction.IdNumber is null)
            return Results.Problem(
                title: "No 13-digit South African ID number was found in the document",
                statusCode: StatusCodes.Status422UnprocessableEntity);

        var (jobId, result, previouslySeen) =
            await ValidateAndStoreAsync(extraction.IdNumber, "sa-id-ocr-v1", db, fingerprinter, ct);

        return Results.Created(
            $"/api/documents/{jobId}",
            new ExtractSaIdResponse(jobId, result, previouslySeen, extraction.IdNumber, extraction.Candidates));
    }

    private static async Task<(Guid JobId, SaIdValidationResult Result, int PreviouslySeen)> ValidateAndStoreAsync(
        string idNumber,
        string pipelineVersion,
        VeridocXDbContext db,
        IIdFingerprinter fingerprinter,
        CancellationToken ct)
    {
        var result = SaIdValidator.Validate(idNumber);

        var fingerprint = fingerprinter.Compute(result.Input);
        var previouslySeen = await db.Jobs.CountAsync(j => j.Fingerprint == fingerprint, ct);

        var masked = Mask(result.Input);
        var storedResult = result with { Input = masked };

        var now = DateTimeOffset.UtcNow;
        var job = new AnalysisJob
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.SaId,
            Status = JobStatus.Completed,
            PipelineVersion = pipelineVersion,
            IsValid = result.IsValid,
            Subject = masked,
            Fingerprint = fingerprint,
            ResultJson = JsonSerializer.Serialize(storedResult, StorageJsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);

        return (job.Id, result, previouslySeen);
    }

    private static string StripDataUrl(string value)
    {
        if (!value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return value.Trim();

        var comma = value.IndexOf(',');
        return comma >= 0 ? value[(comma + 1)..].Trim() : value.Trim();
    }

    private static string Mask(string idNumber) =>
        idNumber.Length <= 4
            ? new string('*', idNumber.Length)
            : new string('*', idNumber.Length - 4) + idNumber[^4..];
}

public record SaIdRequest(string IdNumber);

public record ExtractBase64Request(string ImageBase64);

public record SaIdResponse(Guid JobId, SaIdValidationResult Result, int PreviouslySeen);

public record ExtractSaIdResponse(
    Guid JobId,
    SaIdValidationResult Result,
    int PreviouslySeen,
    string ExtractedIdNumber,
    IReadOnlyList<string> Candidates);

public record JobResponse(
    Guid Id,
    string DocumentType,
    string Status,
    bool? IsValid,
    string? Subject,
    string? ResultJson,
    DateTimeOffset CreatedAt);
