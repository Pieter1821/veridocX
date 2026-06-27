using System.Globalization;
using System.Text.RegularExpressions;
using VeridocX.Server.Domain.Ocr;
using VeridocX.Server.Domain.SaId;

namespace VeridocX.Server.Domain.Payslip;

public static partial class PayslipParser
{
    [GeneratedRegex(@"\d[\d\s.,]*\d|\d")]
    private static partial Regex AmountToken();

    [GeneratedRegex(@"^\s*R?\s*\d[\d.,\s]*$")]
    private static partial Regex AmountCell();

    private static readonly string[] GrossKeywords =
        ["total earnings", "gross pay", "gross salary", "gross", "basic salary"];
    private static readonly string[] NetKeywords =
        ["net pay", "nett pay", "net salary", "nett salary", "take home", "take-home"];
    private static readonly string[] PayeKeywords =
        ["paye (income tax)", "paye", "income tax"];
    private static readonly string[] UifKeywords = ["uif"];

    public static PayslipExtraction Parse(string? text) =>
        Parse(new OcrDocument(text ?? string.Empty, []));

    public static PayslipExtraction Parse(OcrDocument doc)
    {
        var amounts = BuildAmountMap(doc.Tables);
        var evidence = new List<string>();

        var gross = LookupTable(amounts, GrossKeywords, "gross", evidence);
        var net = LookupTable(amounts, NetKeywords, "net", evidence);
        var paye = LookupTable(amounts, PayeKeywords, "paye", evidence);
        var uif = LookupUif(amounts, evidence);

        var lines = doc.Text.Split('\n');
        gross ??= FromLines(lines, GrossKeywords, "gross", evidence);
        net ??= FromLines(lines, NetKeywords, "net", evidence);
        paye ??= FromLines(lines, PayeKeywords, "paye", evidence);
        uif ??= FromLines(lines, UifKeywords, "uif", evidence);

        return new PayslipExtraction
        {
            GrossEarnings = gross,
            NetPay = net,
            Paye = paye,
            Uif = uif,
            IdNumber = SaIdTextScanner.FindBest(doc.Text),
            Evidence = evidence,
        };
    }

    private static List<(string Label, decimal Amount)> BuildAmountMap(IReadOnlyList<OcrTable> tables)
    {
        var map = new List<(string Label, decimal Amount)>();

        foreach (var table in tables)
        {
            var ytdStartColumn = FindYtdStartColumn(table);

            foreach (var row in table.Rows)
                ScanRow(row, map, ytdStartColumn);
        }

        return map;
    }

    private static int FindYtdStartColumn(OcrTable table)
    {
        var start = int.MaxValue;

        foreach (var row in table.Rows)
        {
            for (var c = 0; c < row.Count; c++)
            {
                var cell = row[c].ToLowerInvariant();
                if (cell.Contains("year to date") || cell.Contains("ytd") || cell.Contains("employer contribution"))
                    start = Math.Min(start, c);
            }
        }

        return start;
    }

    private static void ScanRow(IReadOnlyList<string> row, List<(string Label, decimal Amount)> map, int maxColumn)
    {
        string? pendingLabel = null;
        decimal? lastAmount = null;

        for (var i = 0; i < row.Count && i < maxColumn; i++)
        {
            var t = row[i].Trim();
            if (t.Length == 0)
                continue;

            if (AmountCell().IsMatch(t) && ParseAmount(t) is decimal amount)
            {
                lastAmount = amount;
            }
            else
            {
                if (pendingLabel is not null && lastAmount is not null)
                    map.Add((pendingLabel.ToLowerInvariant(), lastAmount.Value));

                pendingLabel = t;
                lastAmount = null;
            }
        }

        if (pendingLabel is not null && lastAmount is not null)
            map.Add((pendingLabel.ToLowerInvariant(), lastAmount.Value));
    }

    private static decimal? LookupTable(
        List<(string Label, decimal Amount)> map, string[] keywords, string field, List<string> evidence)
    {
        foreach (var keyword in keywords)
        {
            foreach (var (label, amount) in map)
            {
                if (label.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    evidence.Add($"{field}: table row '{label}' = {amount}");
                    return amount;
                }
            }
        }

        return null;
    }

    private static decimal? LookupUif(List<(string Label, decimal Amount)> map, List<string> evidence)
    {
        foreach (var (label, amount) in map)
        {
            if (label.Contains("uif", StringComparison.OrdinalIgnoreCase) &&
                !label.Contains("employer", StringComparison.OrdinalIgnoreCase))
            {
                evidence.Add($"uif: table row '{label}' = {amount}");
                return amount;
            }
        }

        return null;
    }

    private static decimal? FromLines(string[] lines, string[] keywords, string field, List<string> evidence)
    {
        var hit = FindByKeywords(lines, keywords);
        if (hit is null)
            return null;

        evidence.Add($"{field}: line '{hit.Value.Line}' = {hit.Value.Amount}");
        return hit.Value.Amount;
    }

    private static (decimal Amount, string Line)? FindByKeywords(string[] lines, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            foreach (var line in lines)
            {
                var idx = line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    continue;

                var amount = FirstAmountFrom(line, idx + keyword.Length);
                if (amount is not null)
                    return (amount.Value, line.Trim());
            }
        }

        return null;
    }

    private static decimal? FirstAmountFrom(string line, int start)
    {
        var tail = start < line.Length ? line[start..] : string.Empty;
        foreach (Match m in AmountToken().Matches(tail))
        {
            var value = ParseAmount(m.Value);
            if (value is not null)
                return value;
        }

        return null;
    }

    public static decimal? ParseAmount(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var s = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        if (s.Length == 0)
            return null;

        var hasDot = s.Contains('.');
        var hasComma = s.Contains(',');

        char? decimalSep = null;

        if (hasDot && hasComma)
        {
            decimalSep = s.LastIndexOf('.') > s.LastIndexOf(',') ? '.' : ',';
        }
        else if (hasDot || hasComma)
        {
            var sep = hasDot ? '.' : ',';
            var count = s.Count(c => c == sep);
            var after = s.Length - s.LastIndexOf(sep) - 1;
            if (count == 1 && (after == 1 || after == 2))
                decimalSep = sep;
        }

        string normalized;
        if (decimalSep is char d)
        {
            var thousands = d == '.' ? ',' : '.';
            normalized = s.Replace(thousands.ToString(), string.Empty).Replace(d, '.');
        }
        else
        {
            normalized = s.Replace(".", string.Empty).Replace(",", string.Empty);
        }

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}
