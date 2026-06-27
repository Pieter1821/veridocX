using System.Security.Cryptography;
using System.Text;

namespace VeridocX.Server.Security;

public interface IIdFingerprinter
{
    string Compute(string idNumber);
}

public sealed class HmacIdFingerprinter : IIdFingerprinter
{
    private readonly byte[] _pepper;

    public HmacIdFingerprinter(IConfiguration config)
    {
        var pepper = config["Security:IdPepper"];

        if (string.IsNullOrWhiteSpace(pepper))
            throw new InvalidOperationException(
                "Security:IdPepper is not configured. Set it in user-secrets (dev) or Key Vault (prod).");

        _pepper = Convert.FromBase64String(pepper);
    }

    public string Compute(string idNumber)
    {
        var normalized = Encoding.UTF8.GetBytes(idNumber.Trim());
        using var hmac = new HMACSHA256(_pepper);
        var hash = hmac.ComputeHash(normalized);
        return Convert.ToHexString(hash);
    }
}
