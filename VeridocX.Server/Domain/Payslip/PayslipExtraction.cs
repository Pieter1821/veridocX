namespace VeridocX.Server.Domain.Payslip;

public record PayslipExtraction
{
    public decimal? GrossEarnings { get; init; }
    public decimal? NetPay { get; init; }
    public decimal? Paye { get; init; }
    public decimal? Uif { get; init; }

    public string? IdNumber { get; init; }

    public IReadOnlyList<string> Evidence { get; init; } = [];
}
