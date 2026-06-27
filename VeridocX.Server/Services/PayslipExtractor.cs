using VeridocX.Server.Domain.Payslip;

namespace VeridocX.Server.Services;

public interface IPayslipExtractor
{
    Task<PayslipExtraction> ExtractAsync(BinaryData image, CancellationToken ct);
}

public sealed class PayslipExtractor(IOcrService ocr) : IPayslipExtractor
{
    public async Task<PayslipExtraction> ExtractAsync(BinaryData image, CancellationToken ct)
    {
        var doc = await ocr.ReadAsync(image, ct);
        return PayslipParser.Parse(doc);
    }
}
