namespace VeridocX.Server.Domain.SaId;

public enum Gender { Female, Male }

public enum Citizenship { Citizen, PermanentResident, Unknown }

public record ValidationCheck(string Name, bool Passed, string Detail);

public record SaIdValidationResult
{
    public required string Input { get; init; }

    public bool IsValid { get; init; }

    public IReadOnlyList<ValidationCheck> Checks { get; init; } = [];

    public DateOnly? DateOfBirth { get; init; }
    public Gender? Gender { get; init; }
    public Citizenship? Citizenship { get; init; }
}
