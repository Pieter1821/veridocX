using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VeridocX.Server.Data;
using VeridocX.Server.Domain;
using VeridocX.Server.Domain.SaId;
using VeridocX.Server.Security;

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

            var result = SaIdValidator.Validate(request.IdNumber);

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
                PipelineVersion = "sa-id-v1",
                IsValid = result.IsValid,
                Subject = masked,
                Fingerprint = fingerprint,
                ResultJson = JsonSerializer.Serialize(storedResult, StorageJsonOptions),
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Jobs.Add(job);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/documents/{job.Id}",
                new SaIdResponse(job.Id, result, previouslySeen));
        })
        .WithName("ValidateSaId");

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

    private static string Mask(string idNumber) =>
        idNumber.Length <= 4
            ? new string('*', idNumber.Length)
            : new string('*', idNumber.Length - 4) + idNumber[^4..];
}

public record SaIdRequest(string IdNumber);

public record SaIdResponse(Guid JobId, SaIdValidationResult Result, int PreviouslySeen);

public record JobResponse(
    Guid Id,
    string DocumentType,
    string Status,
    bool? IsValid,
    string? Subject,
    string? ResultJson,
    DateTimeOffset CreatedAt);
