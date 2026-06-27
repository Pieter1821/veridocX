namespace VeridocX.Server.Domain.Affordability;

public class AffordabilityAssessment
{
    public Guid Id { get; set; }

    public string? SubjectFingerprint { get; set; }

    public decimal GrossMonthlyIncome { get; set; }
    public decimal DiscretionaryIncome { get; set; }
    public decimal ProposedInstalment { get; set; }

    public bool IsAffordable { get; set; }
    public bool DeclaredBelowNorm { get; set; }

    public string? ResultJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
