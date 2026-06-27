using VeridocX.Server.Domain.SaId;
using Xunit;

namespace VeridocX.Tests;

public class SaIdTextScannerTests
{
    [Fact]
    public void Finds_id_printed_as_a_single_run()
    {
        var best = SaIdTextScanner.FindBest("Identity Number\n8001015009087\nRSA");
        Assert.Equal("8001015009087", best);
    }

    [Fact]
    public void Finds_id_printed_in_spaced_groups()
    {
        var best = SaIdTextScanner.FindBest("I.D. No.: 800101 5009 08 7");
        Assert.Equal("8001015009087", best);
    }

    [Fact]
    public void Prefers_a_checksum_valid_candidate()
    {
        var text = "Ref 1234567890123 ID 800101 5009 08 7";
        var best = SaIdTextScanner.FindBest(text);
        Assert.Equal("8001015009087", best);
    }

    [Fact]
    public void Returns_null_when_no_thirteen_digit_run_exists()
    {
        Assert.Null(SaIdTextScanner.FindBest("Name: Jane Doe, Acc 12345"));
        Assert.Null(SaIdTextScanner.FindBest(null));
    }
}
