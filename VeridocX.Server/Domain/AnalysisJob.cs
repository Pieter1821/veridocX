namespace VeridocX.Server.Domain;

public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum DocumentType
{
    Unknown,
    SaId,
    Payslip,
    BankStatement,
    ProofOfAddress
}

public class AnalysisJob
{
    public Guid Id { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Pending;

    public DocumentType DocumentType { get; set; } = DocumentType.Unknown;

    public string? BlobRef { get; set; }

    public string? CallbackUrl { get; set; }

    public string PipelineVersion { get; set; } = "v0";

    public bool? IsValid { get; set; }

    public string? Subject { get; set; }

    public string? Fingerprint { get; set; }

    public string? ResultJson { get; set; }

    public string? Error { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
