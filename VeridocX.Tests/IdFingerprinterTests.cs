using Microsoft.Extensions.Configuration;
using VeridocX.Server.Security;
using Xunit;

namespace VeridocX.Tests;

public class IdFingerprinterTests
{
    private const string PepperA = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string PepperB = "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkI=";

    private static IIdFingerprinter WithPepper(string pepperBase64)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:IdPepper"] = pepperBase64
            })
            .Build();
        return new HmacIdFingerprinter(config);
    }

    [Fact]
    public void Same_id_same_pepper_is_deterministic()
    {
        var fp = WithPepper(PepperA);
        Assert.Equal(fp.Compute("8001015009087"), fp.Compute("8001015009087"));
    }

    [Fact]
    public void Different_pepper_changes_the_fingerprint()
    {
        var a = WithPepper(PepperA).Compute("8001015009087");
        var b = WithPepper(PepperB).Compute("8001015009087");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Output_is_sha256_sized_hex()
    {
        var fp = WithPepper(PepperA).Compute("8001015009087");
        Assert.Equal(64, fp.Length);
    }

    [Fact]
    public void Missing_pepper_fails_closed()
    {
        var config = new ConfigurationBuilder().Build();
        Assert.Throws<InvalidOperationException>(() => new HmacIdFingerprinter(config));
    }
}
