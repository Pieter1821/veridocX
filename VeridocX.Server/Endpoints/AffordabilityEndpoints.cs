using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VeridocX.Server.Data;
using VeridocX.Server.Domain.Affordability;
using VeridocX.Server.Security;

namespace VeridocX.Server.Endpoints;

public static class AffordabilityEndpoints
{
    private static readonly JsonSerializerOptions StorageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static RouteGroupBuilder MapAffordabilityEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("assessments/affordability", async (
            AffordabilityRequest request,
            VeridocXDbContext db,
            IIdFingerprinter fingerprinter,
            CancellationToken ct) =>
        {
            if (request.Assessment is null)
                return Results.Problem(
                    title: "assessment is required",
                    statusCode: StatusCodes.Status400BadRequest);

            var result = AffordabilityCalculator.Assess(request.Assessment);

            var subjectFingerprint = string.IsNullOrWhiteSpace(request.ApplicantIdNumber)
                ? null
                : fingerprinter.Compute(request.ApplicantIdNumber);

            var assessment = new AffordabilityAssessment
            {
                Id = Guid.NewGuid(),
                SubjectFingerprint = subjectFingerprint,
                GrossMonthlyIncome = result.GrossMonthlyIncome,
                DiscretionaryIncome = result.DiscretionaryIncome,
                ProposedInstalment = result.ProposedInstalment,
                IsAffordable = result.IsAffordable,
                DeclaredBelowNorm = result.DeclaredBelowNorm,
                ResultJson = JsonSerializer.Serialize(result, StorageJsonOptions),
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.AffordabilityAssessments.Add(assessment);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/assessments/affordability/{assessment.Id}",
                new AffordabilityResponse(assessment.Id, result));
        })
        .WithName("AssessAffordability");

        group.MapGet("assessments/affordability/{id:guid}", async (
            Guid id,
            VeridocXDbContext db,
            CancellationToken ct) =>
        {
            var a = await db.AffordabilityAssessments.FirstOrDefaultAsync(x => x.Id == id, ct);
            return a is null
                ? Results.NotFound()
                : Results.Ok(new AffordabilityRecordResponse(
                    a.Id,
                    a.SubjectFingerprint,
                    a.GrossMonthlyIncome,
                    a.DiscretionaryIncome,
                    a.ProposedInstalment,
                    a.IsAffordable,
                    a.DeclaredBelowNorm,
                    a.ResultJson,
                    a.CreatedAt));
        })
        .WithName("GetAffordabilityAssessment");

        return group;
    }
}

public record AffordabilityRequest(string? ApplicantIdNumber, AffordabilityInput Assessment);

public record AffordabilityResponse(Guid AssessmentId, AffordabilityResult Result);

public record AffordabilityRecordResponse(
    Guid Id,
    string? SubjectFingerprint,
    decimal GrossMonthlyIncome,
    decimal DiscretionaryIncome,
    decimal ProposedInstalment,
    bool IsAffordable,
    bool DeclaredBelowNorm,
    string? ResultJson,
    DateTimeOffset CreatedAt);
