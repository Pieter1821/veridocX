using System.Text.RegularExpressions;

namespace VeridocX.Server.Domain.SaId;

public static partial class SaIdTextScanner
{
    [GeneratedRegex("[0-9][0-9 ]{11,}[0-9]")]
    private static partial Regex DigitRun();

    public static IReadOnlyList<string> FindCandidates(string? text)
    {
        var candidates = new List<string>();

        foreach (Match run in DigitRun().Matches(text ?? string.Empty))
        {
            var digits = new string(run.Value.Where(char.IsDigit).ToArray());
            for (var i = 0; i + 13 <= digits.Length; i++)
            {
                var window = digits.Substring(i, 13);
                if (!candidates.Contains(window))
                    candidates.Add(window);
            }
        }

        return candidates;
    }

    public static string? FindBest(string? text)
    {
        var candidates = FindCandidates(text);
        return candidates.FirstOrDefault(c => SaIdValidator.Validate(c).IsValid)
               ?? candidates.FirstOrDefault();
    }
}
