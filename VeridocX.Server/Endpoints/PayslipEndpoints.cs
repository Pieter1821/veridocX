using System.Text.Json;
using System.Text.Json.Serialization;
using VeridocX.Server.Data;
using VeridocX.Server.Domain;
using VeridocX.Server.Domain.Payslip;
using VeridocX.Server.Security;
using VeridocX.Server.Services;

namespace VeridocX.Server.Endpoints;

public static class PayslipEndpoints
{
    private static readonly JsonSerializerOptions StorageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static RouteGroupBuilder MapPayslipEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("documents/payslip/extract", async (
            IFormFile file,
            VeridocXDbContext db,
            IIdFingerprinter fingerprinter,
            IPayslipExtractor extractor,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.Problem(
                    title: "file is required",
                    statusCode: StatusCodes.Status400BadRequest);

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            return await ExtractAndStoreAsync(
                BinaryData.FromBytes(ms.ToArray()), db, fingerprinter, extractor, ct);
        })
        .WithName("ExtractPayslipFromUpload")
        .DisableAntiforgery();

        group.MapPost("documents/payslip/extract-base64", async (
            PayslipBase64Request request,
            VeridocXDbContext db,
            IIdFingerprinter fingerprinter,
            IPayslipExtractor extractor,
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

            return await ExtractAndStoreAsync(
                BinaryData.FromBytes(bytes), db, fingerprinter, extractor, ct);
        })
        .WithName("ExtractPayslipFromBase64");

        return group;
    }

    private static async Task<IResult> ExtractAndStoreAsync(
        BinaryData image,
        VeridocXDbContext db,
        IIdFingerprinter fingerprinter,
        IPayslipExtractor extractor,
        CancellationToken ct)
    {
        var extraction = await extractor.ExtractAsync(image, ct);

        string? subject = null;
        string? fingerprint = null;
        if (!string.IsNullOrEmpty(extraction.IdNumber))
        {
            subject = Mask(extraction.IdNumber);
            fingerprint = fingerprinter.Compute(extraction.IdNumber);
        }

        var stored = extraction with { IdNumber = subject };

        var now = DateTimeOffset.UtcNow;
        var job = new AnalysisJob
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Payslip,
            Status = JobStatus.Completed,
            PipelineVersion = "payslip-ocr-v1",
            Subject = subject,
            Fingerprint = fingerprint,
            ResultJson = JsonSerializer.Serialize(stored, StorageJsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/documents/{job.Id}",
            new ExtractPayslipResponse(job.Id, extraction));
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

public record PayslipBase64Request(string ImageBase64);

public record ExtractPayslipResponse(Guid JobId, PayslipExtraction Extraction);
